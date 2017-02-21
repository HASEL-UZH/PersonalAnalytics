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
using Shared.Data;
using BluetoothLowEnergyConnector;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Concurrent;
using System.Timers;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;

namespace PolarTracker
{
    public sealed class Deamon : BaseTracker, ITracker, BluetoothDeviceListener
    {
        private static readonly ConcurrentQueue<HeartRateMeasurement> hrQueue = new ConcurrentQueue<HeartRateMeasurement>();
        private System.Timers.Timer saveToDatabaseTimer = new System.Timers.Timer();

        private NotifyIcon notification;
        private NotifyIcon btNotification;
        private bool showNotification = true;
        private bool showBluetoothNotification = true;
        private double previousRR = double.NaN;
        private bool isConnectedToBluetoothDevice = false;
        private ChooseBluetoothDevice chooser;

        public Deamon()
        {
            Name = Settings.TRACKER_NAME;
            if (Settings.IsDetailedCollectionEnabled)
            {
                Name += " (detailed)";
            }
            
            LoggerWrapper.Instance.NewConsoleMessage += OnNewConsoleMessage;
            LoggerWrapper.Instance.NewLogFileMessage += OnNewLogFileMessage;

            chooser = new ChooseBluetoothDevice();
            chooser.AddListener(this);
        }

        public override string GetVersion()
        {
            var v = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;
            return Shared.Helpers.VersionHelper.GetFormattedVersion(v);
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

        public override bool IsFirstStart { get { return !Database.GetInstance().HasSetting(Settings.TRACKER_ENEABLED_SETTING); } }

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
            
            string storedDeviceName = Database.GetInstance().GetSettingsString(Settings.HEARTRATE_TRACKER_ID_SETTING, String.Empty);
            if (storedDeviceName.Equals(String.Empty))
            {
                chooser.ShowDialog();
            }
            else
            {
                PortableBluetoothDeviceInformation deviceInformation = await Connector.Instance.FindDeviceByName(storedDeviceName);

                if (deviceInformation == null)
                {
                    chooser.ShowDialog();
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

        public void ChangeEnableState(bool? polarTrackerEnabled)
        {
            Console.WriteLine(Settings.TRACKER_NAME + " is now " + (polarTrackerEnabled.Value ? "enabled" : "disabled"));
            Database.GetInstance().SetSettings(Settings.TRACKER_ENEABLED_SETTING, polarTrackerEnabled.Value);
            Database.GetInstance().LogInfo("The participant updated the setting '" + Settings.TRACKER_ENEABLED_SETTING + "' to " + polarTrackerEnabled.Value);

            if (polarTrackerEnabled.Value)
            {
                CreateDatabaseTablesIfNotExist();
                Start();
            } else
            {
                Stop();
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

        void BluetoothDeviceListener.OnConnectionEstablished(string deviceID)
        {
            chooser.Close();
            Database.GetInstance().SetSettings(Settings.HEARTRATE_TRACKER_ID_SETTING, deviceID);
            Connector.Instance.ConnectionLost += OnConnectionToDeviceLost;
            Connector.Instance.ValueChangeCompleted += OnNewHeartrateMeasurement;
            Connector.Instance.ConnectionReestablished += OnConnectionReestablished;
            Connector.Instance.BluetoothNotEnabled += OnBluetoothNotEnabled;
            FindSensorLocation();
            StartDatabaseTimer();
            IsRunning = true;
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

                if (Settings.IsDetailedCollectionEnabled)
                {
                    DatabaseConnector.AddHeartMeasurementsToDatabase(measurements, false);
                }
               
                HeartRateMeasurement average = new HeartRateMeasurement()
                {
                        HeartRateValue = measurements.Where(x => !Double.IsNaN(x.HeartRateValue)).Average(x => x.HeartRateValue),
                        RRDifference = measurements.Where(x => !Double.IsNaN(x.RRDifference)).Average(x => x.RRDifference),
                        RRInterval = measurements.Where(x => !Double.IsNaN(x.RRInterval)).Average(x => x.RRInterval),
                        Timestamp = measurements[0].Timestamp
                };
                DatabaseConnector.AddHeartMeasurementsToDatabase(new List<HeartRateMeasurement>() { average }, true);
            }
            else
            {
                isConnectedToBluetoothDevice = false;
                Logger.WriteToConsole("Nothing to save...");
            }
        }

        private void FindSensorLocation()
        {
            string sensorLocation = Connector.Instance.GetBodySensorLocation().Result.ToString();
            if (sensorLocation != null)
            {
                isConnectedToBluetoothDevice = true;
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
            saveToDatabaseTimer.Stop();
            await Task.Run(() =>
                SaveToDatabase()
            );
            IsRunning = false;
            isConnectedToBluetoothDevice = false;
        }

        void BluetoothDeviceListener.OnTrackerDisabled() 
        {
            chooser.Close();
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

        public override List<FirstStartScreenContainer> GetStartScreens()
        {
            FirstStartWindow window = new FirstStartWindow();
            return new List<FirstStartScreenContainer>() { new FirstStartScreenContainer(window, Settings.TRACKER_NAME, window.NextClicked) };
        }
    }
}