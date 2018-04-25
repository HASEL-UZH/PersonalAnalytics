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
using System.Threading;

namespace Retrospection
{
    /// <summary>
    /// Class which manages the retrospection window & server
    /// </summary>
    public sealed class Handler
    {
        public bool IsRunning;
        private static Handler _handler;
        private PersonalAnalyticsHttp _http;
        private RetrospectionWindow _retrospection;
        private SettingsWindow _settingsWindow;
        private string _publishedAppVersion;
        private List<ITracker> _trackers;
        private bool _hideFeedback;
        private bool _hideAbout;

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
            StartHttpServer();
        }

        /// <summary>
        /// prepares the retrospection: starts HTTP Localhost
        /// </summary>
        /// <param name="trackers">visualizes these trackers' visualizations</param>
        /// <param name="appVersion">necessary to display in the pop-up</param>
        /// <param name="hideFeedback">hides the feedback option in the retrospection window, default: false</param>
        /// <param name="hideAbout">hides the about option in the retrospection window, default: false</param>
        public void Start(List<ITracker> trackers, string appVersion, bool hideFeedback = false, bool hideAbout = false)
        {
            // start http server (if not already started)
            StartHttpServer();

            // set needed variables
            SetTrackers(trackers);
            _publishedAppVersion = appVersion;
            _hideFeedback = hideFeedback;
            _hideAbout = hideAbout;
            if (hideFeedback) Shared.Settings.IsFeedbackEnabled = false;

            IsRunning = true;
        }

        private void StartHttpServer()
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
            if (_http == null) return;

            // method to wait for still running events before shutdown
            //new Thread(new ThreadStart(delegate ()
            //{
            //    // Wait a few seconds and if threads are not terminated, kill the process
            //    for (int i = 0; i < 20; i++)
            //    {
            //        Thread.Sleep(100);
            //        if (_http == null)
            //            return;
            //    }
            //    Process.GetCurrentProcess().Kill();
            //})).Start();


            _http.Stop();
            _http = null;

            IsRunning = false;
        }

        #endregion

        /// <summary>
        /// forward the trackers to the server, which actually
        /// needs them for the visualization
        /// </summary>
        /// <param name="trackers"></param>
        private void SetTrackers(List<ITracker> trackers)
        {
            _trackers = trackers;
            _http.SetTrackers(_trackers);
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

        private static string CreateNavigateUri(string parameters)
        {
            return "http://localhost:" + Shared.Settings.Port + "/" + parameters;
        }

        public bool OpenRetrospection()
        {
            try
            {
                // new window
                if (_retrospection == null)
                {
                    _retrospection = new RetrospectionWindow(_hideFeedback, _hideAbout);
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
            _retrospection?.Close();
        }

        public void SendFeedback(string subject = "Feedback", string body = "")
        {
            FeedbackHelper.SendFeedback(subject, body, _publishedAppVersion);
        }

        public void OpenPrivacyStatement()
        {
            Process.Start(Shared.Settings.PrivacyStatementUri);
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
                var window = new AboutWindow(_publishedAppVersion, Database.GetInstance().GetDbPragmaVersion());
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
