// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-15
// 
// Licensed under the MIT License.

using Shared;
using Shared.Data;
using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using TaskDetectionTracker.Views;

namespace TaskDetectionTracker
{
    public class Deamon : BaseTracker, ITracker
    {

        public Deamon()
        {
            Name = Settings.TrackerName;
        }

        public override void CreateDatabaseTablesIfNotExist()
        {
            //TODO
        }

        public override string GetVersion()
        {
            var v = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;
            return Shared.Helpers.VersionHelper.GetFormattedVersion(v);
        }

        public override bool IsEnabled()
        {
            //TODO
            return true;
        }

        public override void Start()
        {
            //TODO
        }

        public override void Stop()
        {
            //TODO
        }

        public override void UpdateDatabaseTables(int version)
        {
            //not needed
        }

        private void ShowTaskDetectionPopup()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(
                () =>
                {
                    var popup = new TaskDetectionPopup();
                    popup.ShowDialog();
                }));
            }
            catch (ThreadAbortException e) { Database.GetInstance().LogError(Name + ": " + e.Message); }
            catch (Exception e) { Database.GetInstance().LogError(Name + ": " + e.Message); }
        }
    }
}