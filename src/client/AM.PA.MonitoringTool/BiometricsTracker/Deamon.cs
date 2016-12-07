// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.

using BluetoothLowEnergy;
using BiometricsTracker.Data;
using Shared;

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
            Connector c = new Connector();
            c.Start();
            c.ValueChangeCompleted += C_ValueChangeCompleted;
        }

        private void C_ValueChangeCompleted(HeartRateMeasurement heartRateMeasurementValue)
        {
            Logger.WriteToConsole("New value received: " + heartRateMeasurementValue.ToString());
            DatabaseConnector.AddHeartrateToDatabase(heartRateMeasurementValue.Timestamp, heartRateMeasurementValue.HeartRateValue);
        }

        public override void Stop()
        {
            //TODO
        }

        public override void UpdateDatabaseTables(int version)
        {
            // no database updates necessary yet
        }
        
    }
}