// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2016-12-07
// 
// Licensed under the MIT License.

using BiometricsTracker.Data;
using Shared;
using System;
using System.Collections.Generic;

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
            throw new NotImplementedException();
        }

        public override void Stop()
        {
            throw new NotImplementedException();
        }

        public override void UpdateDatabaseTables(int version)
        {
            // no database updates necessary yet
        }
        
    }
}