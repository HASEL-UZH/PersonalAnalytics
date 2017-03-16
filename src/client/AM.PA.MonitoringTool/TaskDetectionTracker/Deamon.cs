// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-15
// 
// Licensed under the MIT License.

using Shared;
using Shared.Data;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using TaskDetectionTracker.Model;
using TaskDetectionTracker.Views;

namespace TaskDetectionTracker
{
    public class Deamon : BaseTracker, ITracker
    {

        #region ITracker Stuff

        public Deamon()
        {
            Name = Settings.TrackerName;
        }

        public override void CreateDatabaseTablesIfNotExist()
        {
            //TODO: create table
        }

        public override string GetVersion()
        {
            var v = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;
            return Shared.Helpers.VersionHelper.GetFormattedVersion(v);
        }

        public override bool IsEnabled()
        {
            return Settings.IsEnabledByDefault;
        }

        public override void Start()
        {
            //TODO: start timer
       
            IsRunning = true;
        }

        public override void Stop()
        {
            //TODO: stop timer

            IsRunning = false;
        }

        public override void UpdateDatabaseTables(int version)
        {
            //not needed
        }

        #endregion

        private void ShowTaskDetectionPopup()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(
                () =>
                {
                    var popup = new TaskDetectionPopup(new List<TaskDetectionInput>());
                    popup.ShowDialog();
                }));
            }
            catch (ThreadAbortException e) { Database.GetInstance().LogError(Name + ": " + e.Message); }
            catch (Exception e) { Database.GetInstance().LogError(Name + ": " + e.Message); }
        }
    }
}