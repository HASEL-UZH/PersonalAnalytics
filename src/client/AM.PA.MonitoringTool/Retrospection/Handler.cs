// Created by André Meyer (t-anmeye@microsoft.com) working as an Intern at Microsoft Research
// Created: 2015-11-24
// 
// Licensed under the MIT License.

using Shared;
using System.Collections.Generic;
using System;
using System.Windows;
using Shared.Data;
using System.Net.Mail;
using System.Diagnostics;
using System.Windows.Threading;
using Shared.Helpers;
using System.Globalization;

namespace Retrospection
{
    /// <summary>
    /// Class which manages the retrospection window & server
    /// </summary>
    public sealed class Handler
    {
        private static Handler _handler;
        private PersonalAnalyticsHttp _http;
        private RetrospectionWindow _retrospection;
        private SettingsWindow _settingsWindow;
        private string _publishedAppVersion;
        private List<ITracker> _trackers;

        #region Start/Stop & Initialization of Singleton

        /// <summary>
        /// Singleton
        /// </summary>
        /// <returns></returns>
        public static Handler GetInstance()
        {
            return _handler ?? (_handler = new Handler());
        }

        public Handler()
        {
            Start();
        }

        /// <summary>
        /// start HTTP Localhost
        /// </summary>
        public void Start()
        {
            if (_http != null) return;
            _http = new PersonalAnalyticsHttp();
            _http.Start();
        }

        /// <summary>
        /// stop HTTP Localhost
        /// </summary>
        public void Stop()
        {
            if (_http != null) return;
            _http.Stop();
            _http = null;
        }
        #endregion

        /// <summary>
        /// forward the trackers to the server, which actually
        /// needs them for the visualization
        /// </summary>
        /// <param name="trackers"></param>
        public void SetTrackers(List<ITracker> trackers)
        {
            _trackers = trackers;
            _http.SetTrackers(_trackers);
        }

        /// <summary>
        /// Sets the current published app version 
        /// (used for feedback) 
        /// </summary>
        /// <param name="v"></param>
        public void SetAppVersion(string v)
        {
            _publishedAppVersion = v;
        }

        #region Open/Close & Navigate Retrospection

        internal string GetDashboardHome()
        {
            return GetDashboardNavigateUriForType(DateTime.Now, VisType.Day); // default: daily retrospection
        }

        internal string GetDashboardNavigateUriForType(DateTime date, VisType type)
        {
            var uri = string.Format(CultureInfo.InvariantCulture, "stats?type={0}&date={1}", type, date.ToString("yyyy-MM-dd"));
            return CreateNavigateUri(uri);
        }

        private string CreateNavigateUri(string parameters)
        {
            return "http://localhost:" + Settings.Port + "/" + parameters;
        }

        public bool OpenRetrospection()
        {
            try
            {
                // new window
                if (_retrospection == null)
                {
                    _retrospection = new RetrospectionWindow();
                    _retrospection.WindowState = (OpenRetrospectionInFullScreen) ? WindowState.Maximized : WindowState.Normal;
                    _retrospection.Topmost = true;
                    _retrospection.Topmost = false;
                    _retrospection.Show();
                }
                // open again if it lost focus, is minimized or was in the background
                else
                {
                    _retrospection.ForceRefreshWindow();
                    _retrospection.WindowState = (OpenRetrospectionInFullScreen) ? WindowState.Maximized : WindowState.Normal;
                    _retrospection.Activate();
                    _retrospection.Topmost = true;
                    _retrospection.Topmost = false;
                    _retrospection.Focus();
                    _retrospection.Show();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// User manually wants to have the retrospection in the browser to be able to bookmark it
        /// </summary>
        public void OpenRetrospectionInBrowser()
        {
            Process.Start(GetDashboardHome());
        }

        public void CloseRetrospection()
        {
            if (_retrospection == null) return;
            _retrospection.Close();
        }

        public void SendFeedback(string subject = "Feedback", string body = "")
        {
            FeedbackHelper.SendFeedback(subject, body, _publishedAppVersion);
        }

        public SettingsDto OpenSettings(SettingsDto currentSettings)
        {
            var updatedSettings = new SettingsDto();

            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(
            () =>
            {
                _settingsWindow = new SettingsWindow(_trackers, currentSettings, _publishedAppVersion);
                //_settings.Show();
                Database.GetInstance().LogInfo("The participant opened the settings.");

                if (_settingsWindow.ShowDialog() == true)
                {
                    updatedSettings = _settingsWindow.UpdatedSettingsDto;
                }
            }));
            
            return updatedSettings;
        }

        public void OpenAbout()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(
            () =>
            {
                var window = new AboutWindow(_publishedAppVersion);
                window.Show();
            }));
        }

        private bool _openRetrospectionInFullScreen;
        public bool OpenRetrospectionInFullScreen
        {
            get
            {
                _openRetrospectionInFullScreen = Database.GetInstance().GetSettingsBool("OpenRetrospectionInFullScreen", false); // default: open in window, not full screen
                return _openRetrospectionInFullScreen;
            }
            set
            {
                // only update if settings changed
                if (value == _openRetrospectionInFullScreen) return;

                // update settings
                Database.GetInstance().SetSettings("OpenRetrospectionInFullScreen", value);

                // log
                Database.GetInstance().LogInfo("The participant updated the setting 'OpenRetrospectionInFullScreen' to " + value);
            }
        }

        #endregion
    }

    public enum NavigateType
    {
        RetrospectionDay,
        RetrospectionWeek
    }
}
