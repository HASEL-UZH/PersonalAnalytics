// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.

using BluetoothLowEnergy;
using PolarTracker.Data;
using Shared;
using System.Collections.Generic;
using PolarTracker.Visualizations;
using System;
using PolarTracker.Views;
using System.Windows;
using Shared.Data;
using BluetoothLowEnergyConnector;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Concurrent;
using System.Timers;
using System.Threading.Tasks;

namespace PolarTracker
{
    public sealed class Deamon : BaseTracker, ITracker
    {
        private static readonly ConcurrentQueue<HeartRateMeasurement> hrQueue = new ConcurrentQueue<HeartRateMeasurement>();
        private System.Timers.Timer saveToDatabaseTimer = new System.Timers.Timer();

        private NotifyIcon notification;
        private NotifyIcon btNotification;
        private Window window;
        private bool showNotification = true;
        private bool showBluetoothNotification = true;
        private double previousRR = Double.NaN;
        private bool isConnectedToBluetoothDevice = false;

        public Deamon()
        {
            Name = Settings.TRACKER_NAME;
            
            LoggerWrapper.Instance.NewConsoleMessage += OnNewConsoleMessage;
            LoggerWrapper.Instance.NewLogFileMessage += OnNewLogFileMessage;

            ChooseBluetoothDevice chooser = new ChooseBluetoothDevice();
            chooser.AddListener(this);
            window = new Window
            {
                Title = "Choose a Bluetooth Device to connect",
                Content = chooser,
                Height = chooser.Height,
                Width = chooser.Width
            };
        }

        private void OnNewLogFileMessage(Exception error)
        {
            Logger.WriteToLogFile(error);
        }

        private void OnNewConsoleMessage(string message)
        {
            Logger.WriteToConsole(message);
        }

        public override void CreateDatabaseTablesIfNotExist()
        {
            DatabaseConnector.CreatePolarTables();
        }

        public override string GetStatus()
        {
            return IsRunning ? (Name + " is running. A bluetooth device is " + (isConnectedToBluetoothDevice ? "connected." : "NOT connected.")) : (Name + " is NOT running.");
        }

        public override bool IsEnabled()
        {
            return Database.GetInstance().GetSettingsBool(Settings.TRACKER_ENEABLED_SETTING, true);
        }

        public override async void Start()
        {
            bool trackerEnabled = Database.GetInstance().GetSettingsBool(Settings.TRACKER_ENEABLED_SETTING, true);
            if (trackerEnabled)
            {
                string storedDeviceName = Database.GetInstance().GetSettingsString(Settings.HEARTRATE_TRACKER_ID_SETTING, String.Empty);
                if (storedDeviceName.Equals(String.Empty))
                {
                    window.ShowDialog();
                }
                else
                {
                    PortableBluetoothDeviceInformation deviceInformation = await Connector.Instance.FindDeviceByName(storedDeviceName);

                    if (deviceInformation == null)
                    {
                        window.ShowDialog();
                    }
                    else
                    {
                        bool connected = await Connector.Instance.Connect(deviceInformation);

                        if (connected)
                        {
                            Connector.Instance.ConnectionLost += OnConnectionToDeviceLost;
                            Connector.Instance.ValueChangeCompleted += OnNewHeartrateMeasurement;
                            Connector.Instance.ConnectionReestablished += OnConnectionReestablished;
                            Connector.Instance.BluetoothNotEnabled += OnBluetoothNotEnabled;
                            FindSensorLocation();
                            StartDatabaseTimer();
                            IsRunning = true;
                        }
                        else
                        {
                            Logger.WriteToConsole("Couldn't establish a connection! Tracker is not running.");
                            IsRunning = false;
                        }
                    }
                }
            }
        }

        private void OnBluetoothNotEnabled()
        {
            Logger.WriteToConsole("Bluetooth not enabled!");
            if (showBluetoothNotification)
            {
                btNotification = new NotifyIcon();
                btNotification.Visible = true;
                btNotification.BalloonTipTitle = "PersonalAnalytics: Bluetooth not enabled!";
                btNotification.BalloonTipText = "PersonalAnalytics: Bluetooth is not enabled. To use the biometrics tracker, please enable bluetooth.";
                btNotification.Icon = SystemIcons.Exclamation;
                btNotification.Text = "PersonalAnalytics: Bluetooth not enabled!";
                btNotification.ShowBalloonTip(60 * 1000);

            }
            showBluetoothNotification = false;
        }

        public void OnConnectionEstablished(string deviceID)
        {
            window.Close();
            Database.GetInstance().SetSettings(Settings.HEARTRATE_TRACKER_ID_SETTING, deviceID);
            Connector.Instance.ConnectionLost += OnConnectionToDeviceLost;
            Connector.Instance.ValueChangeCompleted += OnNewHeartrateMeasurement;
            Connector.Instance.ConnectionReestablished += OnConnectionReestablished;
            Connector.Instance.BluetoothNotEnabled += OnBluetoothNotEnabled;
            FindSensorLocation();
            StartDatabaseTimer();
            IsRunning = true;
            isConnectedToBluetoothDevice = true;
        }

        private void OnConnectionReestablished()
        {
            Logger.WriteToConsole("Connection restablished!");
            if (notification != null)
            {
                notification.Dispose();
            }
            if (btNotification != null)
            {
                btNotification.Dispose();
            }

            isConnectedToBluetoothDevice = true;
        }

        private void StartDatabaseTimer()
        {
            if (saveToDatabaseTimer != null)
            {
                saveToDatabaseTimer.Interval = Settings.SAVE_TO_DATABASE_INTERVAL;
                saveToDatabaseTimer.Elapsed += OnSaveToDatabase;
                saveToDatabaseTimer.Start();
            }
        }

        private async void OnSaveToDatabase(object sender, ElapsedEventArgs e)
        {
            await Task.Run(() =>
                SaveToDatabase()
            );
        }

        private void SaveToDatabase()
        {
            if (hrQueue.Count > 0)
            {
                isConnectedToBluetoothDevice = true;
                List<HeartRateMeasurement> measurements = new List<HeartRateMeasurement>();

                HeartRateMeasurement measurement = null;
                while (!hrQueue.IsEmpty)
                {
                    hrQueue.TryDequeue(out measurement);
                    if (measurement != null)
                    {
                        if (!Double.IsNaN(previousRR))
                        {
                            measurement.RRDifference = Math.Abs(measurement.RRInterval - previousRR);
                        }
                        previousRR = measurement.RRInterval;
                        measurements.Add(measurement);
                    }
                }
                DatabaseConnector.AddHeartMeasurementsToDatabase(measurements);
            }
            else
            {
                Logger.WriteToConsole("Nothing to save...");
            }
        }

        private void FindSensorLocation()
        {
            string sensorLocation = Connector.Instance.GetBodySensorLocation().Result.ToString();
            if (sensorLocation != null)
            {
                Database.GetInstance().SetSettings(Settings.HEARTRATE_TRACKER_LOCATION_SETTING, sensorLocation);
                Logger.WriteToConsole("Body sensor location: " + sensorLocation);
            }
            else
            {
                Database.GetInstance().SetSettings(Settings.HEARTRATE_TRACKER_LOCATION_SETTING, Settings.HEARTRATE_TRACKER_LOCATION_UNKNOWN);
                Logger.WriteToConsole("Body sensor location unknown");
            }
        }

        private void OnConnectionToDeviceLost(string deviceName)
        {
            if (showNotification)
            {
                notification = new NotifyIcon();
                notification.Visible = true;
                notification.BalloonTipTitle = "PersonalAnalytics: Connection lost!";
                notification.BalloonTipText = "PersonalAnalytics has lost the connection to: " + deviceName;
                notification.Icon = SystemIcons.Exclamation;
                notification.Text = "PersonalAnalytics: Connection to bluetooth device lost!";
                notification.ShowBalloonTip(60 * 1000);
                
            }
            showNotification = false;
            isConnectedToBluetoothDevice = false;
        }

        private async void OnNewHeartrateMeasurement(List<HeartRateMeasurement> heartRateMeasurementValue)
        {
            foreach (HeartRateMeasurement measurement in heartRateMeasurementValue)
            {
                await Task.Run(() => hrQueue.Enqueue(measurement));
            }
        }

        public override async void Stop()
        {
            Connector.Instance.ValueChangeCompleted -= OnNewHeartrateMeasurement;
            Connector.Instance.ConnectionLost -= OnConnectionToDeviceLost;
            Connector.Instance.ConnectionReestablished -= OnConnectionReestablished;
            Connector.Instance.Stop();
            saveToDatabaseTimer.Dispose();
            await Task.Run(() =>
                SaveToDatabase()
            );
            IsRunning = false;
            isConnectedToBluetoothDevice = false;
        }

        internal void OnTrackerDisabled()
        {
            window.Close();
            IsRunning = false;
            isConnectedToBluetoothDevice = false;
            Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, false);
        }

        public override void UpdateDatabaseTables(int version)
        {
            // no database updates necessary yet
        }

        public override List<IVisualization> GetVisualizationsDay(DateTimeOffset date)
        {
            return new List<IVisualization> { new PolarVisualizationForDay(date) };
        }
        
        public override List<IVisualization> GetVisualizationsWeek(DateTimeOffset date)
        {
            return new List<IVisualization> { new PolarVisualizationForWeek(date) };
        }
    }
}