// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;
using Shared;
using Shared.Data;
using TaskSwitchTracker.Models;
using System.Windows;

namespace TaskSwitchTracker
{
    public class Daemon : BaseTracker, ITracker
    {
        private readonly List<TaskSwitch> _taskSwitchesList = new List<TaskSwitch>(); 

        #region METHODS

        #region ITracker Stuff

        public Daemon()
        {
            Name = "User Task Switches";
        }

        public override void Start()
        {
            StartSingleButtonTaskBarWindow();

            IsRunning = true;
        }

        public override void Stop()
        {
            StopSingleButtonTaskBarWindow();

            IsRunning = false;
        }

        public override void CreateDatabaseTablesIfNotExist()
        {
            Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.DbTable + " (id INTEGER PRIMARY KEY, time TEXT, task_switch BOOL)");
        }

        public override bool IsEnabled()
        {
            return Settings.IsEnabled;
        }

        #endregion

        #region Daemon

        /// <summary>
        /// Displays the user a single button (to say when he/she switched a task)
        /// Nothing else...
        /// </summary>
        private void StartSingleButtonTaskBarWindow()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(
                            () =>
                            {
                                var notify = new UserTaskSwitchesWindow();
                                notify.NewTaskSwitch += NewUserTaskSwitch;
                                if (notify.ShowDialog() == true)
                                {
                                    // TODO: do something here?
                                }
                                else
                                {
                                    //TODO: what happens here?
                                    Database.GetInstance().LogErrorUnknown(Name);
                                }
                            }));

        }

        private void NewUserTaskSwitch(TaskSwitch datainstance)
        {
            SaveSuccessfulTaskSwitchEntry(datainstance);
        }

        private void StopSingleButtonTaskBarWindow()
        {
            //TODO: Handle...
        }

        private void SaveSuccessfulTaskSwitchEntry(TaskSwitch entry)
        {
            // add to local list (needed for next survey entry)
            _taskSwitchesList.Add(entry);

            // save to database
            Database.GetInstance().ExecuteDefaultQuery("INSERT INTO " + Settings.DbTable + " (time, task_switch) VALUES (" +
                Database.GetInstance().QTime(entry.TimeStamp) + ", " +
                Database.GetInstance().Q(true.ToString()) + ")" );
        }

        #endregion

        #endregion
    }
}
