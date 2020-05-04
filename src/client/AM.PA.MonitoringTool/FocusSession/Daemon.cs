// Created by Philip Hofmann (philip.hofmann@uzh.ch) from the University of Zurich
// Created: 2020-02-11
// 
// Licensed under the MIT License.

using System;
using MsOfficeTracker.Visualizations;
using Shared;

namespace FocusSession
{
    public sealed class Daemon : BaseTracker, ITracker
    {

        public Daemon()
        {
            Name = Settings.TrackerName;
        }

        public override void Start()
        {
            IsRunning = true;
        }

        public override void Stop()
        {
            IsRunning = false;
        }

        public override System.Collections.Generic.List<IVisualization> GetVisualizationsDay(DateTimeOffset date)
        {
            var vis = new Visualizations.TimerButton(date);
            return new System.Collections.Generic.List<IVisualization> { vis };
        }

        public override void CreateDatabaseTablesIfNotExist()
        {
            Data.Queries.CreateFocusTable();
        }

        public override void UpdateDatabaseTables(int version)
        {
            Data.Queries.UpdateDatabaseTables(version);
        }

        public override string GetVersion()
        {
            var v = new System.Reflection.AssemblyName(System.Reflection.Assembly.GetExecutingAssembly().FullName).Version;
            return Shared.Helpers.VersionHelper.GetFormattedVersion(v);
        }

        // Indicates in the hover menu if there is an active Session and for how long
        public override string GetStatus()
        {
            String currentSessionStatus;
            // no Session
            if (!(Controls.Timer.openSession || Controls.Timer.closedSession))
            {
                currentSessionStatus = "FocusSession is currently not being used.";
            }
            else
            {
                // open Session
                if (Controls.Timer.openSession)
                {
                    if (Controls.Timer.getSessionTime().TotalMinutes < 1) // if it has been running for 0 minutes, just shot that a session is running, not that is has been running for 0 minutes
                    {
                        currentSessionStatus = "There is an open FocusSession running.";
                    }
                    else
                    {
                        currentSessionStatus = "There is an open FocusSession running since " + Controls.Timer.getSessionTime().Minutes + " minutes.";
                    }
                }
                // closed Session
                else
                {
                    currentSessionStatus = "There is a closed FocusSession running for another " + Controls.Timer.getSessionTime().Minutes + " minutes.";
                }
            }
            return currentSessionStatus;
        }

        // TODO implement this, that the user can enable or disable, change when user updates settings, include in settings editor
        public override bool IsEnabled()
        {
            return true;
        }

        /* TODO CustomTimerDuration
        private TimeSpan _CustomTimerDurationInMins;
        public TimeSpan CustomTimerDurationInMins
        {
            get
            {
                var value = Database.GetInstance().GetSettingsInt("CustomTimerDuration", Settings.DefaultCustomTimerDuration);
                _popUpIntervalInMins = TimeSpan.FromMinutes(value);
                return _popUpIntervalInMins;
            }
            set
            {
                var updatedInterval = value;

                // only update if settings changed
                //if (updatedInterval == _popUpIntervalInMins) return;
                //_popUpIntervalInMins = updatedInterval;

                // update settings
                //Database.GetInstance().SetSettings("PopUpInterval", updatedInterval.TotalMinutes.ToString(CultureInfo.InvariantCulture));

                // update interval time
                //_timeRemainingUntilNextSurvey = _popUpIntervalInMins;

                // log
                //Database.GetInstance().LogInfo("The participant updated the setting 'PopUpInterval' to " + _popUpIntervalInMins);
            }
        }
        */

    }
}