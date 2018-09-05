// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-23
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;
using DryIoc;
using EyeCatcher;
using EyeCatcher.DataCollection;
using Shared;

namespace TobiiEyeTracker
{
    public sealed class Deamon : BaseTracker, ITracker
    {
        public Deamon()
        {
            Name = "TobiiEyeTracker";
            IocHelper.Register();
        }

        private DataCollector DataCollector { get; set; }

        public override string GetVersion()
        {
            var v = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;
            return Shared.Helpers.VersionHelper.GetFormattedVersion(v);
        }

        public override string GetStatus()
        {
            return IsRunning ? $"{Name} is running." : $"{Name} is NOT running.";
        }

        public override void CreateDatabaseTablesIfNotExist()
        {
            // Do nothing
        }

        public override bool IsEnabled()
        {
            return true;
        }

        /// <summary>
        /// Zero configuration :p
        /// </summary>
        public override bool IsFirstStart => false;

        public override void Start()
        {
            IsRunning = true;
            DataCollector = IocHelper.Container.Resolve<DataCollector>();
            DataCollector.Start();
        }

        public override void Stop()
        {
            IsRunning = false;
            var dataCollector = DataCollector;
            DataCollector = null;
            dataCollector.Dispose();
        }

        public override void UpdateDatabaseTables(int version)
        {
            //No updates needed so far!
        }

        public override List<IVisualization> GetVisualizationsDay(DateTimeOffset date)
        {
            return new List<IVisualization>();
        }

        public override List<IVisualization> GetVisualizationsWeek(DateTimeOffset date)
        {
            return new List<IVisualization>();
        }

        public override List<IFirstStartScreen> GetStartScreens()
        {
            return new List<IFirstStartScreen>();
        }

    }
}