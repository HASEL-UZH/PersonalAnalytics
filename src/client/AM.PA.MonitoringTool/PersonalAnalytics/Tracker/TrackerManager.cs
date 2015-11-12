// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using PersonalAnalytics.Visualizations;
using Shared;
using Shared.Data;

namespace PersonalAnalytics.Tracker
{
    public class TrackerManager
    {
        private static TrackerManager _manager;
        private readonly List<ITracker> _trackers = new List<ITracker>();
        private PersonalAnalyticsHttp _http;
        public TaskbarIcon TaskbarIcon;
        private DispatcherTimer _timer;
        private string _publishedAppVersion;
        private bool _isPaused;

        /// <summary>
        /// Singleton
        /// </summary>
        /// <returns></returns>
        public static TrackerManager GetInstance()
        {
            return _manager ?? (_manager = new TrackerManager());
        }

        /// <summary>
        /// Initialize the TrackerManager (starts every tracker, creates a connection to the database, etc.)
        /// </summary>
        public void Start()
        {
            // Get (and set) app version
            _publishedAppVersion = GetPublishedAppVersion();

            // Start all registered trackers
            foreach (var tracker in _trackers)
            {
                tracker.CreateDatabaseTablesIfNotExist();
                if (! tracker.IsEnabled()) continue;
                tracker.Start();
            }

            // Start Visualization (HTTP Localhost)
            _http = new PersonalAnalyticsHttp();
            _http.Start();

            // Communication
            var trackersString = string.Join(", ", _trackers.Where(t => t.IsRunning).ToList().ConvertAll(t => t.Name).ToArray());
            Database.GetInstance().LogInfo(String.Format("TrackerManager (V{0}) started with {1} trackers ({2})", _publishedAppVersion, _trackers.Where(t => t.IsRunning).ToList().Count, trackersString));
            SetTaskbarIconTooltip("Tracker started");

            // Start Timer (to update taskbaricon tooltip & database connection)
            _timer = new DispatcherTimer();
            _timer.Interval = Settings.TrackerManagerUpdateInterval;
            _timer.Tick += TrackerManagerUpdates;
            _timer.Start();            
        }

        /// <summary>
        /// stops the TrackerManager (stops every tracker, saves entries, dismisses connection to the database, etc.)
        /// </summary>
        public void Stop()
        {
            Database.GetInstance().Disconnect();

            // stop trackers
            foreach (var tracker in _trackers)
            {
                tracker.Stop();
            }

            // stop visualization (HTTP localhost)
            _http.Stop();
            _http = null;

            _timer.Stop();

            Database.GetInstance().LogInfo("TrackerManager stopped.");
            SetTaskbarIconTooltip("Tracker stopped");
        }

        /// <summary>
        /// stops all trackers
        /// </summary>
        public void Pause()
        {
            foreach (var tracker in _trackers)
            {
                tracker.Stop();
            }
            _isPaused = true;
        }

        // continues all trackers (that are enabled)
        public void Continue()
        {
            foreach (var tracker in _trackers)
            {
                if (!tracker.IsEnabled()) continue;
                tracker.Start();
            }
            _isPaused = false;
        }

        /// <summary>
        /// Tracker registers its service to the TrackerManager
        /// </summary>
        /// <param name="t"></param>
        public void Register(ITracker t)
        {
            _trackers.Add(t);
        }

        /// <summary>
        /// Dreates a taskbar icon to modify its tooltip and create the context menu options
        /// </summary>
        public void InitializeTaskBarIcon()
        {
            TaskbarIcon = new TaskbarIcon();
            TaskbarIcon.Icon = new Icon("Assets/icon.ico");
            TaskbarIcon.ToolTipText = "PersonalAnalytics starting up...";
            TaskbarIcon.TrayMouseDoubleClick += (o, i) => OpenRetrospection();
            SetContextMenuOptions();
        }

        /// <summary>
        /// Manually add ContextMenuOptions
        /// </summary>
        private void SetContextMenuOptions()
        {
            var cm = new System.Windows.Controls.ContextMenu();
            TaskbarIcon.MenuActivation = PopupActivationMode.RightClick;
            TaskbarIcon.ContextMenu = cm;

            var m1 = new System.Windows.Controls.MenuItem { Header = "Show Retrospection" };
            m1.Click += (o, i) => OpenRetrospection();
            TaskbarIcon.ContextMenu.Items.Add(m1);

            var m2 = new System.Windows.Controls.MenuItem { Header = "Do Mini-Survey Now" };
            m2.Click += (o, i) => ManuallyStartUserSurvey();
            if (IsUserEfficiencyTrackerEnabled()) TaskbarIcon.ContextMenu.Items.Add(m2);

            var m6 = new System.Windows.Controls.MenuItem { Header = "Edit Settings" };
            m6.Click += (o, i) => OpenSettings();
            TaskbarIcon.ContextMenu.Items.Add(m6);

            var m4 = new System.Windows.Controls.MenuItem { Header = "Open tracked Data" };
            m4.Click += (o, i) => OpenDataExportDirectory();
            TaskbarIcon.ContextMenu.Items.Add(m4);

            //var m5 = new System.Windows.Controls.MenuItem { Header = "Open baseline" };
            //m5.Click += (o, i) => OpenBaseline();
            //TaskbarIcon.ContextMenu.Items.Add(m5);

            //var m3 = new System.Windows.Controls.MenuItem {Header = "Shutdown Tracker"};
            //m3.Click += (o, i) => Application.Current.Shutdown();
            //TaskbarIcon.ContextMenu.Items.Add(m3);

            var m3 = new System.Windows.Controls.MenuItem { Header = "Pause Tracker" };
            m3.Click += (o, i) => PauseContinueTracker(m3);
            TaskbarIcon.ContextMenu.Items.Add(m3);

            // Styling
            //var converter = new System.Windows.Media.BrushConverter();
            //var brush = (System.Windows.Media.Brush)converter.ConvertFromString("#FFFFFF90");
            //cm.Background = brush;
            //cm.Width = 200;
            //cm.Height = 100;

            //var style = App.Current.TryFindResource("SysTrayMenu");
            //_taskbarIcon.ContextMenu = (System.Windows.Controls.ContextMenu)style;
        }

        /// <summary>
        /// depending on the variable _isPaused, the tracker is
        /// paused or continued
        /// </summary>
        /// <param name="item"></param>
        private void PauseContinueTracker(System.Windows.Controls.MenuItem item)
        {
            // continue
            if (_isPaused)
            {
                Continue();
                item.Header = "Pause Tracker";
                Database.GetInstance().LogInfo("The participant continued the trackers.");
            }
            // pause 
            else
            {
                Pause();
                item.Header = "Continue Tracker";
                Database.GetInstance().LogInfo("The participant paused the trackers.");
            }
        }

        /// <summary>
        /// Opens the directory where all the data is saved
        /// </summary>
        private static void OpenDataExportDirectory()
        {
            var path = Settings.ExportFilePath;
            //var path = Database.GetInstance().GetDatabaseSavePath();
            System.Diagnostics.Process.Start(path);
        }

        /// <summary>
        /// Opens the browser to show the retrospection
        /// </summary>
        private static void OpenRetrospection()
        {
            System.Diagnostics.Process.Start("http://localhost:" + Settings.Port + "/stats");
            //Database.GetInstance().LogInfo("The participant opened the retrospection/visualization."); // do when visualizations are loaded
        }

        /// <summary>
        /// Opens the browser to modify the settings
        /// </summary>
        private static void OpenSettings()
        {
            System.Diagnostics.Process.Start("http://localhost:" + Settings.Port + "/settings");
            Database.GetInstance().LogInfo("The participant opened the settings.");
        }

        /// <summary>
        /// Opens a youtube clip to show the baseline
        /// </summary>
        //private static void OpenBaseline()
        //{
        //    System.Diagnostics.Process.Start("http://www.andre-meyer.ch/baseline");
        //    Database.GetInstance().LogInfo("The participant opened the baseline.");
        //}

        /// <summary>
        /// Enables and Starts the windows context tracker or
        /// disables and Stops it
        /// TODO: ugly hardcoded...
        /// </summary>
        /// <param name="shouldEnable"></param>
        public void EnableDisableWindowsContextTracker(bool shouldEnable)
        {
            try
            {
                var selectedTracker =
                    _trackers.Where(t => t.GetType() == typeof(WindowsContextTracker.Daemon))
                        .Cast<WindowsContextTracker.Daemon>()
                        .FirstOrDefault();

                if (selectedTracker == null) return;

                if (shouldEnable) // enable
                {
                    Database.GetInstanceSettings().WindowsContextTrackerEnabled = true;
                    if (!selectedTracker.IsRunning) selectedTracker.Start();
                }
                else // disable
                {
                    Database.GetInstanceSettings().WindowsContextTrackerEnabled = false;
                    if (selectedTracker.IsRunning) selectedTracker.Stop();
                }
            }
            catch { }
        }

        /// <summary>
        /// Enables and Starts the user efficiency tracker (mini surveys) or
        /// disables and Stops it
        /// TODO: hack (also not sure if makes it inconsistent with IsEnabled variable)
        /// </summary>
        /// <param name="shouldEnable"></param>
        public void EnableDisableUserEfficiencyTracker(bool shouldEnable)
        {
            try
            {
                var selectedTracker =
                    _trackers.Where(t => t.GetType() == typeof(UserEfficiencyTracker.Daemon))
                        .Cast<UserEfficiencyTracker.Daemon>()
                        .FirstOrDefault();

                if (selectedTracker == null) return;

                if (shouldEnable) // enable
                {
                    Database.GetInstanceSettings().MiniSurveysEnabled = true;
                    if (!selectedTracker.IsRunning) selectedTracker.Start();
                }
                else // disable
                {
                    Database.GetInstanceSettings().MiniSurveysEnabled = false;
                    if (selectedTracker.IsRunning) selectedTracker.Stop();
                }
            }
            catch { }
        }

        /// <summary>
        /// todo: ugly, hardcoded
        /// </summary>
        /// <returns>true if the user efficiency tracker is enabled, false otherwise</returns>
        private bool IsUserEfficiencyTrackerEnabled()
        {
            try
            {
                var userEfficiencyTracker =
                    _trackers.Where(t => t.GetType() == typeof(UserEfficiencyTracker.Daemon))
                        .Cast<UserEfficiencyTracker.Daemon>()
                        .FirstOrDefault();
                return userEfficiencyTracker != null && userEfficiencyTracker.IsEnabled();
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// gets the current instance of the userefficiency tracker and manually starts
        /// the survey there
        /// todo: ugly, hardcoded
        /// </summary>
        private void ManuallyStartUserSurvey()
        {
            try
            {
                var userEfficiencyTracker =
                    _trackers.Where(t => t.GetType() == typeof (UserEfficiencyTracker.Daemon))
                        .Cast<UserEfficiencyTracker.Daemon>()
                        .FirstOrDefault();
                if (userEfficiencyTracker == null) return;
                userEfficiencyTracker.ManualTakeSurveyNow();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Updates the taskbar icon tooltip text based on the timer and
        /// checks if a new database file should be created (depending on the current week)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrackerManagerUpdates(object sender, EventArgs e)
        {
            // Update Taskbar Icon Tooltip
            var text = _trackers.Aggregate(String.Empty, (current, tracker) => current + (tracker.GetStatus() + "\n"));
            text += "Version: " + _publishedAppVersion;
            SetTaskbarIconTooltip(text);

            // Update database file (if necessary)
            if (Database.GetInstance().CurrentDatabaseDumpFile != Database.GetLocalDatabaseSavePath())
            {
                Database.GetInstance().Disconnect(); // closes the current instance
                Database.GetInstance().Reconnect(Database.GetLocalDatabaseSavePath()); // connects to the database (& creates a new dump file)
                foreach (var t in _trackers)
                {
                    t.CreateDatabaseTablesIfNotExist();
                }
            }
        }

        /// <summary>
        /// Gets and Formats the currently published
        /// application version.
        /// </summary>
        /// <returns></returns>
        private static string GetPublishedAppVersion()
        {
            if (!ApplicationDeployment.IsNetworkDeployed) return "?.?.?.?";
            var cd = ApplicationDeployment.CurrentDeployment;
            return string.Format("{0}.{1}.{2}.{3}", cd.CurrentVersion.Major, cd.CurrentVersion.Minor, cd.CurrentVersion.Build, cd.CurrentVersion.Revision);
        }

        /// <summary>
        /// helper method to change the tooltip text
        /// </summary>
        /// <param name="message"></param>
        private void SetTaskbarIconTooltip(string message)
        {
            if (TaskbarIcon == null) return;
            TaskbarIcon.ToolTipText = message;
        }
    }
}
