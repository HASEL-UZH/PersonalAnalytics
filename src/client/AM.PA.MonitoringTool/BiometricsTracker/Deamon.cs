// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.

using BluetoothLowEnergy;
using BiometricsTracker.Data;
using Shared;
using System.Collections.Generic;
using BiometricsTracker.Visualizations;
using System;
using BiometricsTracker.Views;
using System.Windows;
using Shared.Data;
using BluetoothLowEnergyConnector;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;

namespace BiometricsTracker
{
    public sealed class Deamon : BaseTracker, ITracker
    {
        private const string TrackerEnabledSetting = "BiometricsTrackerEnabled";
        private const string HeartrateTrackerIDSetting = "HeartrateTrackerID";

        private Window window;
        private bool showNotification = true;

        public Deamon()
        {
            Name = "Biometrics Tracker";

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
            DatabaseConnector.CreateBiometricTables();
        }

        public override bool IsEnabled()
        {
            return Database.GetInstance().GetSettingsBool(TrackerEnabledSetting, true);
        }

        public override async void Start()
        {
            bool trackerEnabled = Database.GetInstance().GetSettingsBool(TrackerEnabledSetting, true);
            if (trackerEnabled)
            {
                string storedDeviceName = Database.GetInstance().GetSettingsString(HeartrateTrackerIDSetting, String.Empty);
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
                            Logger.WriteToConsole("Connection established");
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

        public void OnConnectionEstablished(string deviceID)
        {
            window.Close();
            Database.GetInstance().SetSettings(HeartrateTrackerIDSetting, deviceID);
            Connector.Instance.ConnectionLost += OnConnectionToDeviceLost;
            Connector.Instance.ValueChangeCompleted += OnNewHeartrateMeasurement;
            IsRunning = true;
        }

        private void OnConnectionToDeviceLost(string deviceName)
        {
            if (showNotification)
            {
                NotifyIcon notification = new NotifyIcon();
                notification.Visible = true;
                notification.BalloonTipTitle = "PersonalAnalytics: Connection lost!";
                notification.BalloonTipText = "PersonalAnalytics has lost the connection to: " + deviceName;
                notification.Icon = SystemIcons.Exclamation;
                notification.ShowBalloonTip(60 * 1000);
            }
            showNotification = false;
        }

        private void OnNewHeartrateMeasurement(List<HeartRateMeasurement> heartRateMeasurementValue)
        {
            foreach (HeartRateMeasurement measurement in heartRateMeasurementValue)
            {
                Logger.WriteToConsole(measurement.ToString());
                DatabaseConnector.AddHeartrateToDatabase(measurement.Timestamp, measurement.HeartRateValue, measurement.RRInterval);
            }
        }

        public override void Stop()
        {
            Connector.Instance.ValueChangeCompleted -= OnNewHeartrateMeasurement;
            Connector.Instance.ConnectionLost -= OnConnectionToDeviceLost;
            Connector.Instance.Stop();
            IsRunning = false;
        }

        internal void OnTrackerDisabled()
        {
            window.Close();
            IsRunning = false;
            Database.GetInstance().SetSettings(TrackerEnabledSetting, false);
        }

        public override void UpdateDatabaseTables(int version)
        {
            // no database updates necessary yet
        }

        public override List<IVisualization> GetVisualizationsDay(DateTimeOffset date)
        {
            return new List<IVisualization> { new BiometricVisualizationForDay(date) };
        }
        
        public override List<IVisualization> GetVisualizationsWeek(DateTimeOffset date)
        {
            return new List<IVisualization> { new BiometricVisualizationForWeek(date) };
        }
    }
}