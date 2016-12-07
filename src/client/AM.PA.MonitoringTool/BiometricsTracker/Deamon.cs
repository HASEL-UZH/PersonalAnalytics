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

namespace BiometricsTracker
{
    public sealed class Deamon : BaseTracker, ITracker
    {
        public Deamon()
        {
            Name = "Biometrics Tracker";
        }

        public override void CreateDatabaseTablesIfNotExist()
        {
            DatabaseConnector.CreateBiometricTables();
        }

        public override bool IsEnabled()
        {
            return true;
        }

        public override void Start()
        {
            Connector.Instance.ValueChangeCompleted += OnNewHeartrateMeasurement;
            Connector.Instance.Start();
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