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

namespace BiometricsTracker
{
    public sealed class Deamon : BaseTracker, ITracker
    {
        private Window window;

        public Deamon()
        {
            Name = "Biometrics Tracker";

            BluetoothLowEnergy.LoggerWrapper.Instance.NewConsoleMessage += OnNewConsoleMessage;
            BluetoothLowEnergy.LoggerWrapper.Instance.NewLogFileMessage += OnNewLogFileMessage;

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
            Shared.Logger.WriteToLogFile(error);
        }

        private void OnNewConsoleMessage(string message)
        {
            Shared.Logger.WriteToConsole(message);
        }

        public override void CreateDatabaseTablesIfNotExist()
        {
            DatabaseConnector.CreateBiometricTables();
        }

        public override bool IsEnabled()
        {
            return true;
        }

        public override async void Start()
        {
            bool trackerEnabled = Database.GetInstance().GetSettingsBool("BiometricsTrackerEnabled", true);
            if (trackerEnabled)
            {
                string storedDeviceID = Database.GetInstance().GetSettingsString("HeartrateTrackerID", String.Empty);
                if (storedDeviceID.Equals(String.Empty))
                {
                    window.ShowDialog();
                }
                else
                {
                    PortableBluetoothDeviceInformation deviceInformation = await Connector.Instance.FindDeviceByID(storedDeviceID);
                    await Connector.Instance.Connect(deviceInformation);
                    Connector.Instance.ValueChangeCompleted += OnNewHeartrateMeasurement;
                    Shared.Logger.WriteToConsole("Connection established");
                    IsRunning = true;
                }
            }
        }

        public void OnConnectionEstablished(string deviceID)
        {
            window.Close();
            Connector.Instance.ValueChangeCompleted += OnNewHeartrateMeasurement;
            Database.GetInstance().SetSettings("HeartrateTrackerID", deviceID);
            IsRunning = true;
        }

        private void OnNewHeartrateMeasurement(HeartRateMeasurement heartRateMeasurementValue)
        {
            DatabaseConnector.AddHeartrateToDatabase(heartRateMeasurementValue.Timestamp, heartRateMeasurementValue.HeartRateValue);
        }

        public override void Stop()
        {
            Connector.Instance.ValueChangeCompleted -= OnNewHeartrateMeasurement;
            Connector.Instance.Stop();
            IsRunning = false;
        }

        internal void OnTrackerDisabled()
        {
            window.Close();
            IsRunning = false;
            Database.GetInstance().SetSettings("BiometricsTrackerEnabled", false);
        }

        public override void UpdateDatabaseTables(int version)
        {
            // no database updates necessary yet
        }

        public override List<IVisualization> GetVisualizationsDay(DateTimeOffset date)
        {
            return new List<IVisualization> { new BiometricVisualizationForDay(date) };
        }

    }
}