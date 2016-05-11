// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Drawing;
using System.Linq;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using Shared;
using Shared.Data;
using System.Windows;
using System.Globalization;
using Microsoft.Win32;

namespace PersonalAnalytics
{
    public class TrackerManager
    {
        private static TrackerManager _manager; // singleton instance
        private static TrackerSettings _settings;
        private readonly List<ITracker> _trackers = new List<ITracker>();
        public TaskbarIcon TaskbarIcon;

        private DispatcherTimer _taskbarIconTimer;
        private DispatcherTimer _remindToContinueTrackerTimer;
        private DispatcherTimer _checkForUpdatesTimer;
        private DispatcherTimer _checkTimeZoneTimer;

        private TimeZoneInfo _previousTimeZone;
        private System.Windows.Controls.MenuItem _pauseContinueMenuItem;
        private string _publishedAppVersion;
        private bool _isPaused;

        #region Initialize & Handle TrackerManager

        /// <summary>
        /// Singleton
        /// </summary>
        /// <returns></returns>
        public static TrackerManager GetInstance()
        {
            return _manager ?? (_manager = new TrackerManager());
        }

        /// <summary>
        /// Initialize the TrackerManager
        /// (prepares the settings, starts every tracker, creates a connection to the database, etc.)
        /// </summary>
        public void Start()
        {
            // Get (and set) app version
            _publishedAppVersion = GetPublishedAppVersion();

            // User First Start: Welcome
            if (!Database.GetInstance().HasSetting("FirstStartWindowShown"))
            {
                var firstStart = new FirstStartWindow(_publishedAppVersion);
                firstStart.ShowDialog();

                Database.GetInstance().SetSettings("FirstStartWindowShown", true);
            }

            // Check if the user accepted the consent form, if not: shut down the application
            /*  if (!TrackerManager.GetInstance().UserConsentsToUseApplication())
                {
                    // todo: shut down
                } */

            // Start all registered trackers
            foreach (var tracker in _trackers)
            {
                tracker.CreateDatabaseTablesIfNotExist();
                if (! tracker.IsEnabled()) continue;
                tracker.Start();
            }

            // Start Visualization
            Retrospection.Handler.GetInstance().SetTrackers(_trackers);
            Retrospection.Handler.GetInstance().SetAppVersion(_publishedAppVersion);

            // Communication
            var trackersString = string.Join(", ", _trackers.Where(t => t.IsRunning).ToList().ConvertAll(t => t.Name).ToArray());
            Database.GetInstance().LogInfo(string.Format(CultureInfo.InvariantCulture, "TrackerManager (V{0}) started with {1} trackers ({2})", _publishedAppVersion, _trackers.Where(t => t.IsRunning).ToList().Count, trackersString));
            SetTaskbarIconTooltip("Tracker started");

            // Initialize & start the timer to update the taskbaricon toolitp
            _taskbarIconTimer = new DispatcherTimer();
            _taskbarIconTimer.Interval = Settings.TooltipIconUpdateInterval;
            _taskbarIconTimer.Tick += UpdateTooltipIcon;
            _taskbarIconTimer.Start();

            // Initialize & start the timer to check for updates
            _checkForUpdatesTimer = new DispatcherTimer();
            _checkForUpdatesTimer.Interval = Settings.CheckForToolUpdatesInterval;
            _checkForUpdatesTimer.Tick += UpdateApplicationIfNecessary;
            _checkForUpdatesTimer.Start();

            // initialize & start the (temporary) timer to check and store the timezone
            _checkTimeZoneTimer = new DispatcherTimer();
            _checkTimeZoneTimer.Interval = Settings.CheckTimeZoneInterval;
            _checkTimeZoneTimer.Tick += ((s, e) =>
            {
                CultureInfo.CurrentCulture.ClearCachedData(); // clear the currently cached data
                var currentTimeZone = TimeZoneInfo.Local;
                if (_previousTimeZone != null && _previousTimeZone == currentTimeZone) return;

                // save the current time zone info to the DB
                Database.GetInstance().LogInfo(string.Format(CultureInfo.InvariantCulture, "TimeZoneInfo: local=[{0}], utc=[{1}], zone=[{2}], offset=[{3}].", 
                    DateTime.Now.ToLocalTime(), DateTime.Now.ToUniversalTime(), currentTimeZone.Id, currentTimeZone.BaseUtcOffset));

                _previousTimeZone = currentTimeZone;
            });
            _checkTimeZoneTimer.Start();

            // track time zone changes
            // TODO: store now already!
            SystemEvents.TimeChanged += TimeZoneChanged;
        }

        /// <summary>
        /// Saves the current (updated) time zone to the database
        /// (temporary workaround)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimeZoneChanged(object sender, EventArgs e)
        {
            CultureInfo.CurrentCulture.ClearCachedData();
            var currentTimeZone = TimeZoneInfo.Local;

            // save the current time zone info to the DB
            Database.GetInstance().LogInfo(string.Format(CultureInfo.InvariantCulture, "TimeZoneInfo: local=[{0}], utc=[{1}], zone=[{2}], offset=[{3}].",
                DateTime.Now.ToLocalTime(), DateTime.Now.ToUniversalTime(), currentTimeZone.Id, currentTimeZone.BaseUtcOffset));
        }

        /// <summary>
        /// stops the TrackerManager (stops every tracker, saves entries, dismisses connection to the database, etc.)
        /// </summary>
        public void Stop(bool stoppedManually = false)
        {
            // stop trackers
            foreach (var tracker in _trackers)
            {
                tracker.Stop();
            }

            // close the retrospection window
            Retrospection.Handler.GetInstance().CloseRetrospection();

            // shutdown the visualization server
            Retrospection.Handler.GetInstance().Stop();

            // stop timers & unregister
            if (_taskbarIconTimer != null) _taskbarIconTimer.Stop();
            if (_remindToContinueTrackerTimer != null) _remindToContinueTrackerTimer.Stop();
            if (_checkForUpdatesTimer != null) _checkForUpdatesTimer.Stop();
            TaskbarIcon.TrayBalloonTipClicked -= TrayBallonTipClicked;
            TaskbarIcon.TrayMouseDoubleClick -= (o, i) => OpenRetrospection();

            // sometimes the icon doesn't go away unless we manually dispose it
            TaskbarIcon.Dispose();

            // log shutdown & disconnect from database
            Database.GetInstance().LogInfo("The tracker was shut down.");
            Database.GetInstance().Disconnect();

            // kill the process
            if (stoppedManually)
            {
                ShutdownApplication();
            }
        }

        /// <summary>
        /// stops all trackers
        /// starts a reminder to remind the user to resume it
        /// </summary>
        public void Pause()
        {
            foreach (var tracker in _trackers)
            {
                tracker.Stop();
            }
            _isPaused = true;

            if (_remindToContinueTrackerTimer == null)
            {
                _remindToContinueTrackerTimer = new DispatcherTimer();
                _remindToContinueTrackerTimer.Interval = Settings.RemindToResumeToolInterval;
                _remindToContinueTrackerTimer.Tick += ((s, e) =>
                {
                    // show the popup (already registered for the click event)
                    TaskbarIcon.ShowBalloonTip("Reminder", "The Personal Analytics Tool is still paused. Click here to resume it.", BalloonIcon.Warning);
                });
            }
            _remindToContinueTrackerTimer.IsEnabled = true;
            _remindToContinueTrackerTimer.Start();

            Database.GetInstance().LogInfo("The participant paused the trackers.");
        }

        // continues all trackers (that are enabled)
        public void Continue()
        {
            // disable the reminder
            if (_remindToContinueTrackerTimer != null)
            {
                _remindToContinueTrackerTimer.Stop();
                _remindToContinueTrackerTimer.IsEnabled = false;
            }

            // continue the trackers
            foreach (var tracker in _trackers)
            {
                if (!tracker.IsEnabled()) continue;
                tracker.Start();
            }
            _isPaused = false;
            Database.GetInstance().LogInfo("The participant resumed the trackers.");
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
        /// Initialize/Prepare Settings
        /// </summary>
        public void PrepareSettings()
        {
            _settings = new TrackerSettings(_trackers);
        }

        #endregion

        #region Taskbar Icon Options

        /// <summary>
        /// Dreates a taskbar icon to modify its tooltip and create the context menu options
        /// </summary>
        public void InitializeTaskBarIcon()
        {
            TaskbarIcon = new TaskbarIcon();
            TaskbarIcon.Icon = new Icon("Assets/icon.ico");
            TaskbarIcon.ToolTipText = "PersonalAnalytics starting up...";
            TaskbarIcon.TrayMouseDoubleClick += (o, i) => OpenRetrospection();
            TaskbarIcon.TrayBalloonTipClicked += TrayBallonTipClicked;
            SetContextMenuOptions();
        }

        /// <summary>
        /// Click on a TaskbarIcon Balloon to resume the tracker
        /// todo: as soon as there are other balloons shown, we need to be able to distinguish which one
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void TrayBallonTipClicked(object s, RoutedEventArgs e)
        {
            PauseContinueTracker(_pauseContinueMenuItem);
        }

        /// <summary>
        /// Manually add ContextMenuOptions
        /// </summary>
        private void SetContextMenuOptions()
        {
            var cm = new System.Windows.Controls.ContextMenu();
            TaskbarIcon.MenuActivation = PopupActivationMode.RightClick;
            TaskbarIcon.ContextMenu = cm;

            var m8 = new System.Windows.Controls.MenuItem { Header = "Upload collected data" };
            m8.Click += (o, i) => UploadTrackedData();
            if (Settings.IsUploadEnabled) TaskbarIcon.ContextMenu.Items.Add(m8);

            var m4 = new System.Windows.Controls.MenuItem { Header = "Open collected data" };
            m4.Click += (o, i) => OpenDataExportDirectory();
            TaskbarIcon.ContextMenu.Items.Add(m4);

            var m6 = new System.Windows.Controls.MenuItem { Header = "Settings" };
            m6.Click += (o, i) => OpenSettings();
            TaskbarIcon.ContextMenu.Items.Add(m6);

            var m2 = new System.Windows.Controls.MenuItem { Header = "Answer pop-up now" };
            m2.Click += (o, i) => ManuallyStartUserSurvey();
            if (_settings.IsUserEfficiencyTrackerEnabled()) TaskbarIcon.ContextMenu.Items.Add(m2);

            var m1 = new System.Windows.Controls.MenuItem { Header = "Show Retrospection" };
            m1.Click += (o, i) => Retrospection.Handler.GetInstance().OpenRetrospection();
            TaskbarIcon.ContextMenu.Items.Add(m1);

#if DEBUG
            var m5 = new System.Windows.Controls.MenuItem { Header = "Show Retrospection (in browser)" };
            m5.Click += (o, i) => Retrospection.Handler.GetInstance().OpenRetrospectionInBrowser();
            TaskbarIcon.ContextMenu.Items.Add(m5);
#endif

            var m3 = new System.Windows.Controls.MenuItem { Header = "Pause Tracker" };
            m3.Click += (o, i) => PauseContinueTracker(m3);
            TaskbarIcon.ContextMenu.Items.Add(m3);
            _pauseContinueMenuItem = m3;

#if DEBUG
            // hint: test really carefully before enabling it for everyone (occasionally causes some problems,
            // which don't correctly shutdown the httpserver with the result that the next time the tool is used/started, 
            // there is a corrupt server, and therefore the retrospection not showing any visualizations
            var m7 = new System.Windows.Controls.MenuItem { Header = "Shutdown Tracker" };
            m7.Click += (o, i) => Stop(true);
            TaskbarIcon.ContextMenu.Items.Add(m7);
#endif

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
            }
            // pause 
            else
            {
                Pause();
                item.Header = "Resume Tracker";
            }
        }

        /// <summary>
        /// Opens the UI specified for the current retrospection
        /// </summary>
        private void OpenRetrospection()
        {
            Retrospection.Handler.GetInstance().OpenRetrospection();
        }

        /// <summary>
        /// Opens the directory where all the data is saved
        /// </summary>
        private static void OpenDataExportDirectory()
        {
            var path = Settings.ExportFilePath;
            System.Diagnostics.Process.Start(path);
        }

        /// <summary>
        /// Starts the upload wizard
        /// </summary>
        private static void UploadTrackedData()
        {
            Retrospection.Handler.GetInstance().OpenUploadWizard();
        }

        /// <summary>
        /// Opens the browser to modify the settings
        /// </summary>
        private static void OpenSettings()
        {
            _settings.OpenSettings();
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
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateTooltipIcon(object sender, EventArgs e)
        {
            // Update Taskbar Icon Tooltip
            var text = _trackers.Aggregate(String.Empty, (current, tracker) => current + (tracker.GetStatus() + "\n"));
            text += "Version: " + _publishedAppVersion;
            SetTaskbarIconTooltip(text);

            // Update database file (if necessary)
            //if (Database.GetInstance().CurrentDatabaseDumpFile != Database.GetLocalDatabaseSavePath())
            //{
            //    Database.GetInstance().Disconnect(); // closes the current instance
            //    Database.GetInstance().Reconnect(Database.GetLocalDatabaseSavePath()); // connects to the database (& creates a new dump file)
            //    foreach (var t in _trackers)
            //    {
            //        t.CreateDatabaseTablesIfNotExist();
            //    }
            //}
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

        #endregion

        #region Helpers

        /// <summary>
        /// Gets and Formats the currently published
        /// application version.
        /// </summary>
        /// <returns></returns>
        private static string GetPublishedAppVersion()
        {
            if (!ApplicationDeployment.IsNetworkDeployed) return "?.?.?.?";
            var cd = ApplicationDeployment.CurrentDeployment;
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}.{3}", cd.CurrentVersion.Major, cd.CurrentVersion.Minor, cd.CurrentVersion.Build, cd.CurrentVersion.Revision);
        }

        /// <summary>
        /// Called in a regular interval to try and update the tool.
        /// Asks the user for consent prior to installing the update. In case the installation was successful, the tool will restart.
        /// 
        /// source: https://msdn.microsoft.com/en-us/library/ms404263.aspx
        /// </summary>
        private void UpdateApplicationIfNecessary(object sender, EventArgs e)
        {
            UpdateCheckInfo info = null;

            // can only update if it is network deployed
            if (!ApplicationDeployment.IsNetworkDeployed)
            {
                Database.GetInstance().LogError("Failed to check for updates for the application. IsNetworkDeployed is false.");
                return;
            }

            var ad = ApplicationDeployment.CurrentDeployment;

            try
            {
                info = ad.CheckForDetailedUpdate();

            }
            catch (DeploymentDownloadException dde)
            {
                // may rarely happen
                Logger.WriteToLogFile(dde);
                Database.GetInstance().LogError(string.Format(CultureInfo.InvariantCulture, "Failed to install the newest version of the application. This might be a network issue (error: DeploymentDownloadException, details: {0}).", dde.Message));
                return;
            }
            catch (InvalidDeploymentException ide)
            {
                // shouldn't happen
                Database.GetInstance().LogError(string.Format(CultureInfo.InvariantCulture, "Failed to install the newest version of the application. The ClickOnce Deployment is corrupt (error: InvalidDeploymentException, details: {0}).", ide.Message));
                MessageBox.Show("Cannot check for a new version of the application. The ClickOnce deployment is corrupt. Please redeploy the application and try again. Please contact us in case this problem persists or you have any questions.", 
                    Dict.ToolName + ": Error", 
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            catch (InvalidOperationException ioe)
            {
                // shouldn't happen
                Database.GetInstance().LogError(string.Format(CultureInfo.InvariantCulture, "Failed to install the newest version of the application. It is likely not a ClickOnce application (error: InvalidOperationException, details: {0}).", ioe.Message));
                return;
            }
            catch (Exception ex)
            {
                // some unknown error happened
                Logger.WriteToLogFile(ex);
                return;
            }

            if (info.UpdateAvailable)
            {
                var doUpdate = true;

                if (!info.IsUpdateRequired)
                {
                    var dr = MessageBox.Show("An update is available (from your current version to version " + info.AvailableVersion + "). Would you like to update the application now?",
                        Dict.ToolName + ": Update Available", 
                        MessageBoxButton.OKCancel,
                        MessageBoxImage.Question);
                    if (!(MessageBoxResult.OK == dr))
                    {
                        doUpdate = false;
                        Database.GetInstance().LogError("The user didn't want to update the tool.");
                    }
                }
                else
                {
                    // Display a message that the app MUST reboot. Display the minimum required version.
                    MessageBox.Show("A mandatory update is available (from your current version to version " + info.AvailableVersion + "). The application will now install the update and restart.",
                        Dict.ToolName + ": Mandatory Update Available", 
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                if (doUpdate)
                {
                    try
                    {
                        ad.Update();
                        Database.GetInstance().LogInfo(string.Format(CultureInfo.InvariantCulture, "Successfully updated tool to version {0}.", info.AvailableVersion));
                        MessageBox.Show("The application has been upgraded, and will now restart.", 
                            Dict.ToolName + ": Successfully Updated", 
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        // the only way the restart with a click once work and IsNetworkDeployed is not false after the restart is
                        // the following (according to this: http://blachniet.com/blog/how-not-to-restart-a-clickonce-application/)

                        Stop(false); // stop the application (restart stuff below)
                        System.Windows.Forms.Application.Restart(); // other way might be: Process.Start(App.AppPath);
                        ShutdownApplication();
                    }
                    catch (DeploymentDownloadException dde)
                    {
                        Database.GetInstance().LogError(string.Format(CultureInfo.InvariantCulture, "Updating the tool to version {0} failed (error: InvalidOperationException, see errors.log for details).", info.AvailableVersion));
                        Logger.WriteToLogFile(dde);
                        MessageBox.Show("Cannot install the latest version of the application. Please check your network connection and try again later. Please contact us in case this problem persists.",
                            Dict.ToolName + ": Error", 
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Shutdown the application only if the state is saved, database disconnected, etc.
        /// </summary>
        private void ShutdownApplication()
        {
            Environment.Exit(0); // this is not clean, otherwise, the httpserver is not shut down and then in some strange inconsistent state
            //App.Current.Shutdown(); //todo: not sure if this is the right way to do 
            //Application.Current.Shutdown();
        }


        /*   #region User Consent

           private bool _applicationHasUserConsent;
           public bool ApplicationHasUserConsent
           {
               get
               {
                   _applicationHasUserConsent = Database.GetInstance().GetSettingsBool("ApplicationHasUserConsent", false); //default: no user consent
                   return _applicationHasUserConsent;
               }
               set
               {
                   var updatedHasConsent = value;

                   // only update if settings changed
                   if (updatedHasConsent == _applicationHasUserConsent) return;

                   // update settings
                   Database.GetInstance().SetSettings("ApplicationHasUserConsent", value);

                   // log
                   Database.GetInstance().LogInfo("The participant updated the setting 'ApplicationHasUserConsent' to " + updatedHasConsent);
               }
           }

           internal bool UserConsentsToUseApplication()
           {
               // application already has consent
               if (ApplicationHasUserConsent) return true;

               // if not, ask for consent

               return true;
           }

           #endregion */

        #endregion
    }
}
