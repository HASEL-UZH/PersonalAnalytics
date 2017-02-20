// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-10
// 
// Licensed under the MIT License.

using Shared;
using System.Reflection;
using System;
using System.Collections.Generic;
using Shared.Data;

namespace GarminTracker
{
    public class Daemon : BaseTracker, ITracker
    {

        public Daemon()
        {
            Name = Settings.TRACKER_NAME;
            if (Settings.IS_DETAILED_COLLECTION_AVAILABLE)
            {
                Name += " (detailed)";
            }
        }

        public override List<IVisualization> GetVisualizationsDay(DateTimeOffset date)
        {
            return new List<IVisualization>() { };
        }

        public override List<IVisualization> GetVisualizationsWeek(DateTimeOffset date)
        {
            return new List<IVisualization>() { };
        }

        public override void CreateDatabaseTablesIfNotExist()
        {

        }

        public override string GetVersion()
        {
            var v = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;
            return Shared.Helpers.VersionHelper.GetFormattedVersion(v);
        }

        public override bool IsEnabled()
        {
            return Database.GetInstance().GetSettingsBool(Settings.TRACKER_ENABLED, false);
        }

        public override void Start()
        {
            Logger.WriteToConsole("GarminTracker is now running!");
        }

        public override void Stop()
        {
            Logger.WriteToConsole("GarminTracker is not running anymore!");
        }

        public override void UpdateDatabaseTables(int version)
        {
            //not needed
        }
    }

}