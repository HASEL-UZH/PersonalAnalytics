// Created by Philip Hofmann (philip.hofmann@uzh.ch) from the University of Zurich
// Created: 2020-02-11
// 
// Licensed under the MIT License.

using System;
using System.Globalization;
using MsOfficeTracker.Visualizations;
using Shared;
using Shared.Data;

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
            return new System.Collections.Generic.List<IVisualization> {vis};
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
            var v = new System.Reflection.AssemblyName(System.Reflection.Assembly.GetExecutingAssembly().FullName)
                .Version;
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
                    if (Controls.Timer.getSessionTime() < 1
                    ) // if it has been running for 0 minutes, just shot that a session is running, not that is has been running for 0 minutes
                    {
                        currentSessionStatus = "There is an open FocusSession running.";
                    }
                    else
                    {
                        currentSessionStatus = "There is an open FocusSession running since " +
                                               Controls.Timer.getSessionTime() + " minutes.";
                    }
                }
                // closed Session
                else
                {
                    currentSessionStatus = "There is a closed FocusSession running for another " +
                                           Controls.Timer.getSessionTime() + " minutes.";
                }
            }

            return currentSessionStatus;
        }

        // TODO implement this, that the user can enable or disable, change when user updates settings, include in settings editor
        public override bool IsEnabled()
        {
            return true;
        }


        private int _closedSessionDuration;

        public int ClosedSessionDuration
        {
            get
            {
                _closedSessionDuration = Database.GetInstance()
                    .GetSettingsInt("ClosedSessionDuration", Settings.ClosedSessionDuration);
                return _closedSessionDuration;
            }
            set
            {
                var updatedClosedSessionDuration = value;

                // only update if settings changed
                if (updatedClosedSessionDuration == _closedSessionDuration)
                {
                    return;
                }

                _closedSessionDuration = updatedClosedSessionDuration;
                // update variable
                Settings.ClosedSessionDuration = _closedSessionDuration;

                // update settings
                Database.GetInstance().SetSettings("ClosedSessionDuration",
                    updatedClosedSessionDuration.ToString(CultureInfo.InvariantCulture));

                // log
                Database.GetInstance().LogInfo("The participant updated the setting 'ClosedSessionDuration' to " +
                                               _closedSessionDuration);
            }
        }

        public bool ReplyMessageIsEnabled()
        {
            return Database.GetInstance().GetSettingsBool(Settings.REPLYMESSAGE_ENEABLED_SETTING, Settings.IsEnabledByDefault);
        }

        public void ChangeReplyMessageEnabledState(bool? ReplyMessageEnabled)
        {
            Console.WriteLine(" ReplyMessage is now " + (ReplyMessageEnabled.Value ? "enabled" : "disabled"));
            Database.GetInstance().SetSettings(Settings.REPLYMESSAGE_ENEABLED_SETTING, ReplyMessageEnabled.Value);
            Database.GetInstance().LogInfo("The participant updated the setting '" + Settings.REPLYMESSAGE_ENEABLED_SETTING + "' to " + ReplyMessageEnabled.Value);

            if (ReplyMessageEnabled.Value)
            {
                CreateDatabaseTablesIfNotExist();
                Controls.Timer.ReplyMessageEnabled = true;
            }
            else if (!ReplyMessageEnabled.Value)
            {
                Controls.Timer.ReplyMessageEnabled = false;
            }
            else
            {
                Logger.WriteToConsole("ChangeReplyMessageEnabledState else statement");
            }
        }

        public bool WindowFlaggingIsEnabled()
        {
            return Database.GetInstance().GetSettingsBool(Settings.WINDOWFLAGGING_ENEABLED_SETTING, Settings.IsEnabledByDefault);
        }

        public void ChangeWindowFlaggingEnabledState(bool? WindowFlaggingEnabled)
        {
            Console.WriteLine(" WindowFlagging is now " + (WindowFlaggingEnabled.Value ? "enabled" : "disabled"));
            Database.GetInstance().SetSettings(Settings.WINDOWFLAGGING_ENEABLED_SETTING, WindowFlaggingEnabled.Value);
            Database.GetInstance().LogInfo("The participant updated the setting '" + Settings.WINDOWFLAGGING_ENEABLED_SETTING + "' to " + WindowFlaggingEnabled.Value);

            if (WindowFlaggingEnabled.Value)
            {
                CreateDatabaseTablesIfNotExist();
                Controls.Timer.WindowFlaggingEnabled = true;
            }
            else if (!WindowFlaggingEnabled.Value)
            {
                Controls.Timer.WindowFlaggingEnabled = false;
            }
            else
            {
                Logger.WriteToConsole("ChangeWindowFlaggingEnabledState else statement");
            }
        }

    }
}