// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2016-01-08
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Shared.Data
{
    /// <summary>
    /// Global settings option (to open settings window and handle settings)
    /// TODO: de-couple
    /// </summary>
     public class TrackerSettings
    {
        private List<ITracker> _trackers;

        public TrackerSettings(List<ITracker> trackers)
        {
            _trackers = trackers;
        }

        public void OpenSettings()
        {
            try
            {
                // Open Settings and get response
                var settings = GetCurrentSettings();

                // Update/Save Settings
                var updatedSettings = Retrospection.Handler.GetInstance().OpenSettings(settings);

                if (updatedSettings.PopUpEnabled.HasValue)
                {
                    if (GetUserEfficiencyTracker() != null) GetUserEfficiencyTracker().PopUpEnabled = updatedSettings.PopUpEnabled.Value;
                }
                if (updatedSettings.PopUpInterval.HasValue)
                {
                    if (GetUserEfficiencyTracker() != null) GetUserEfficiencyTracker().PopUpIntervalInMins = TimeSpan.FromMinutes(updatedSettings.PopUpInterval.Value);
                }
                if (updatedSettings.UserInputTrackerEnabled.HasValue)
                {
                    if (GetUserInputTracker() != null) GetUserInputTracker().UserInputTrackerEnabled = updatedSettings.UserInputTrackerEnabled.Value;
                }
                if (updatedSettings.TimeSpentShowEmailsEnabled.HasValue)
                {
                    if (GetTimeSpentVisualizerVisualizer() != null) GetTimeSpentVisualizerVisualizer().TimeSpentShowEmailsEnabled = updatedSettings.TimeSpentShowEmailsEnabled.Value;
                }
                if (updatedSettings.TimeSpentShowProgramsEnabled.HasValue)
                {
                    if (GetTimeSpentVisualizerVisualizer() != null) GetTimeSpentVisualizerVisualizer().TimeSpentShowProgramsEnabled = updatedSettings.TimeSpentShowProgramsEnabled.Value;
                }
                if (updatedSettings.OpenRetrospectionInFullScreen.HasValue)
                {
                    Retrospection.Handler.GetInstance().OpenRetrospectionInFullScreen = updatedSettings.OpenRetrospectionInFullScreen.Value;
                }
                if (updatedSettings.Office365ApiEnabled.HasValue)
                {
                    if (GetMSOfficeTracker() != null) GetMSOfficeTracker().MsOfficeTrackerEnabled = updatedSettings.Office365ApiEnabled.Value;
                    //if (GetPeopleVisualizer() != null) GetPeopleVisualizer().PeopleVisualizerEnabled = updatedSettings.Office365ApiEnabled.Value;
                }
                if (updatedSettings.FlowLightTrackerEnabled.HasValue)
                {
                    if (GetFlowLightTracker() != null) GetFlowLightTracker().FlowLightTrackerEnabled = updatedSettings.FlowLightTrackerEnabled.Value;
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        private SettingsDto GetCurrentSettings()
        {
            var dto = new SettingsDto();

            try
            {
                var userEfficiencyTracker = GetUserEfficiencyTracker();
                if (userEfficiencyTracker != null) dto.PopUpEnabled = userEfficiencyTracker.PopUpEnabled;
                if (userEfficiencyTracker != null) dto.PopUpInterval = (int)userEfficiencyTracker.PopUpIntervalInMins.TotalMinutes;

                var userInputTracker = GetUserInputTracker();
                if (userInputTracker != null) dto.UserInputTrackerEnabled = userInputTracker.UserInputTrackerEnabled;

                var timeSpentVisualizer = GetTimeSpentVisualizerVisualizer();
                if (timeSpentVisualizer != null)
                {
                    dto.TimeSpentShowEmailsEnabled = timeSpentVisualizer.TimeSpentShowEmailsEnabled;
                    dto.TimeSpentShowProgramsEnabled = timeSpentVisualizer.TimeSpentShowProgramsEnabled;
                }

                dto.OpenRetrospectionInFullScreen = Retrospection.Handler.GetInstance().OpenRetrospectionInFullScreen;

                //var peopleVisualizer = GetPeopleVisualizer();
                var msOfficeTracker = GetMSOfficeTracker();
                dto.Office365ApiEnabled = msOfficeTracker.MsOfficeTrackerEnabled;
                //if (peopleVisualizer != null && msOfficeTracker != null) dto.Office365ApiEnabled = (peopleVisualizer.PeopleVisualizerEnabled || msOfficeTracker.MsOfficeTrackerEnabled);
                //else if (peopleVisualizer == null && msOfficeTracker != null) dto.Office365ApiEnabled = msOfficeTracker.MsOfficeTrackerEnabled;
                //else if (peopleVisualizer != null && msOfficeTracker == null) dto.Office365ApiEnabled = peopleVisualizer.PeopleVisualizerEnabled;
                //else dto.Office365ApiEnabled = false;

                var flowLightTracker = GetFlowLightTracker();
                if (flowLightTracker != null) dto.FlowLightTrackerEnabled = flowLightTracker.FlowLightTrackerEnabled;
            } 
            catch { }

            return dto;
        }

        //private PeopleVisualizer.PeopleVisualizer GetPeopleVisualizer()
        //{
        //    try
        //    {
        //        var tracker =
        //            _trackers.Where(t => t.GetType() == typeof(PeopleVisualizer.PeopleVisualizer))
        //                .Cast<PeopleVisualizer.PeopleVisualizer>()
        //                .FirstOrDefault();

        //        return tracker;
        //    }
        //    catch { return null; }
        //}

        private TimeSpentVisualizer.Visualizers.TimeSpentVisualizer GetTimeSpentVisualizerVisualizer()
        {
            try
            {
                var tracker =
                    _trackers.Where(t => t.GetType() == typeof(TimeSpentVisualizer.Visualizers.TimeSpentVisualizer))
                        .Cast<TimeSpentVisualizer.Visualizers.TimeSpentVisualizer>()
                        .FirstOrDefault();

                return tracker;
            }
            catch { return null; }
        }

        private MsOfficeTracker.Daemon GetMSOfficeTracker()
        {
            try
            {
                var tracker =
                    _trackers.Where(t => t.GetType() == typeof(MsOfficeTracker.Daemon))
                        .Cast<MsOfficeTracker.Daemon>()
                        .FirstOrDefault();

                return tracker;
            }
            catch { return null; }
        }

        private UserEfficiencyTracker.Daemon GetUserEfficiencyTracker()
        {
            try
            {
                var tracker =
                    _trackers.Where(t => t.GetType() == typeof(UserEfficiencyTracker.Daemon))
                        .Cast<UserEfficiencyTracker.Daemon>()
                        .FirstOrDefault();

                return tracker;
            }
            catch { return null; }
        }

        private UserInputTracker.Daemon GetUserInputTracker()
        {
            try
            {
                var tracker =
                    _trackers.Where(t => t.GetType() == typeof(UserInputTracker.Daemon))
                        .Cast<UserInputTracker.Daemon>()
                        .FirstOrDefault();

                return tracker;
            }
            catch { return null; }
        }

        public bool IsUserEfficiencyTrackerEnabled()
        {
            var tracker = GetUserEfficiencyTracker();
            if (tracker != null) return tracker.PopUpEnabled;
            else return false;
        }

        private FlowLightTracker.Daemon GetFlowLightTracker()
        {
            try
            {
                var tracker =
                    _trackers.Where(t => t.GetType() == typeof(FlowLightTracker.Daemon))
                        .Cast<FlowLightTracker.Daemon>()
                        .FirstOrDefault();

                return tracker;
            }
            catch { return null; }
        }
    }
}
