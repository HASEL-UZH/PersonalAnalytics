// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2016-01-08
// 
// Licensed under the MIT License.

using FitbitTracker;
using FitbitTracker.Data;
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
                if (updatedSettings.TimeSpentHideMeetingsWithoutAttendeesEnabled.HasValue)
                {
                    if (GetTimeSpentVisualizerVisualizer() != null) GetTimeSpentVisualizerVisualizer().TimeSpentHideMeetingsWithoutAttendeesEnabled = updatedSettings.TimeSpentHideMeetingsWithoutAttendeesEnabled.Value;
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
                if (updatedSettings.FlowLightEnabled.HasValue)
                {
                    FlowLight.Handler.GetInstance().FlowLightEnabled = updatedSettings.FlowLightEnabled.Value;
                }
                if (updatedSettings.FlowLightSkypeForBusinessEnabled.HasValue)
                {
                    FlowLight.Handler.GetInstance().SkypeForBusinessEnabled = updatedSettings.FlowLightSkypeForBusinessEnabled.Value;
                }
                if (updatedSettings.FlowLightAutomaticEnabled.HasValue)
                {
                    FlowLight.Handler.GetInstance().AutomaticEnabled = updatedSettings.FlowLightAutomaticEnabled.Value;
                }
                if (updatedSettings.FlowLightDnDEnabled.HasValue)
                {
                    FlowLight.Handler.GetInstance().DnDEnabled = updatedSettings.FlowLightDnDEnabled.Value;
                }
                if (updatedSettings.FlowLightSensitivityLevel.HasValue)
                {
                    FlowLight.Handler.GetInstance().SensitivityLevel = updatedSettings.FlowLightSensitivityLevel.Value;
                }
                if (updatedSettings.FlowLightBlacklist != null)
                {
                    GetFlowTracker().SetSetting_Application_Blacklist(updatedSettings.FlowLightBlacklist);
                }
                if (updatedSettings.PolarTrackerEnabled.HasValue)
                {
                    if (GetPolarTracker() != null) GetPolarTracker().ChangeEnableState(updatedSettings.PolarTrackerEnabled);
                }

                if (updatedSettings.FitbitTrackerEnabled.HasValue)
                {
                    if (GetFitbitTracker() != null) GetFitbitTracker().ChangeEnabledState(updatedSettings.FitbitTrackerEnabled);
                }

                if (updatedSettings.FitbitTokenRevoked.HasValue)
                {
                    FitbitConnector.RevokeAccessToken(SecretStorage.GetAccessToken());
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
                    dto.TimeSpentHideMeetingsWithoutAttendeesEnabled = timeSpentVisualizer.TimeSpentHideMeetingsWithoutAttendeesEnabled;
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

                var polarTracker = GetPolarTracker();
                dto.PolarTrackerEnabled = polarTracker.IsEnabled();

                var fitbitTracker = GetFitbitTracker();
                dto.FitbitTrackerEnabled = fitbitTracker.IsEnabled();
                dto.FitbitTokenRevokEnabled = SecretStorage.GetAccessToken() != null && fitbitTracker.IsEnabled();
                dto.FitbitTokenRevoked = dto.FitbitTokenRevokEnabled;

                var flowLight = FlowLight.Handler.GetInstance();
                if (flowLight != null)
                {
                    dto.FlowLightAvailable = FlowLight.Settings.IsEnabledByDefault;
                    dto.FlowLightEnabled = flowLight.FlowLightEnabled;
                    dto.FlowLightSkypeForBusinessEnabled = flowLight.SkypeForBusinessEnabled;
                    dto.FlowLightAutomaticEnabled = flowLight.AutomaticEnabled;
                    dto.FlowLightDnDEnabled = flowLight.DnDEnabled;
                    dto.FlowLightSensitivityLevel = flowLight.SensitivityLevel;            
                }

                var flowLightTracker = GetFlowTracker();
                if (flowLightTracker != null) dto.FlowLightBlacklist = flowLightTracker.GetSetting_Application_Blacklist();
            } 
            catch { }

            return dto;
        }

        private FitbitTracker.Deamon GetFitbitTracker()
        {
            try
            {
                var tracker =
                    _trackers.Where(t => t.GetType() == typeof(FitbitTracker.Deamon))
                        .Cast<FitbitTracker.Deamon>()
                        .FirstOrDefault();

                return tracker;
            }
            catch { return null; }
        }

        private PolarTracker.Deamon GetPolarTracker()
        {
            try
            {
                var tracker =
                    _trackers.Where(t => t.GetType() == typeof(PolarTracker.Deamon))
                        .Cast<PolarTracker.Deamon>()
                        .FirstOrDefault();

                return tracker;
            }
            catch { return null; }
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

        private FlowTracker.Daemon GetFlowTracker()
        {
            try
            {
                var tracker =
                    _trackers.Where(t => t.GetType() == typeof(FlowTracker.Daemon))
                        .Cast<FlowTracker.Daemon>()
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
            return tracker != null && tracker.PopUpEnabled;
        }
    }
}
