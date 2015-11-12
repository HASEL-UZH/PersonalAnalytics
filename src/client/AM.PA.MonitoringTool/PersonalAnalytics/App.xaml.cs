// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using NetFwTypeLib;
using System.Windows.Threading;
using Microsoft.Win32;
using PersonalAnalytics.Tracker;
using Shared;
using Shared.Data;

namespace PersonalAnalytics
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Current.DispatcherUnhandledException += App_DispatcherUnhandledException;
            Current.SessionEnding += App_SessionEnding;
        }

        /// <summary>
        /// OnStartup registers all trackers to be available to the user
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            // Create log directory if it doesn't already exist
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PersonalAnalytics");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            //////////////////////////////////////////////////////
            // Set Window Options
            //////////////////////////////////////////////////////

            // don't automatically shut down
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;


            //////////////////////////////////////////////////////
            // Initialize & Connect the database
            //////////////////////////////////////////////////////

            RegisterAppForPcStartup();
            //AddFirewallException(); // TODO: problems if not system admin

            // Connect to the database
            Database.GetInstance().Connect();


            //////////////////////////////////////////////////////
            // Register Tracker
            //////////////////////////////////////////////////////
            ITracker t = new WindowsActivityTracker.Daemon();
            TrackerManager.GetInstance().Register(t);

            ITracker t2 = new UserEfficiencyTracker.Daemon();
            TrackerManager.GetInstance().Register(t2);

            //ITracker t3 = new TaskSwitchTracker.Daemon();
            //TrackerManager.GetInstance().Register(t3);

            ITracker t4 = new UserInputTracker.Daemon();
            TrackerManager.GetInstance().Register(t4);

            ITracker t5 = new WindowsContextTracker.Daemon();
            TrackerManager.GetInstance().Register(t5);


            //////////////////////////////////////////////////////
            // initialize task bar icon & context menu
            //////////////////////////////////////////////////////
            TrackerManager.GetInstance().InitializeTaskBarIcon();


            //////////////////////////////////////////////////////
            // Start Tracker Manager
            //////////////////////////////////////////////////////
            TrackerManager.GetInstance().Start();
        }

        /// <summary>
        /// Adds the program in the registry as a startup program if it
        /// isn't already set.
        /// </summary>
        private static void RegisterAppForPcStartup()
        {
            // The path to the key where Windows looks for startup applications
            var rkApp = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (rkApp == null) return;
            //if (rkApp.GetValue(regAppName) != null) return; // only set once

            // path to launch shortcut
            var startPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs) + Settings.RegAppPath;
            startPath = "\"" + startPath + "\"";
            var registeredStartPath = (string)rkApp.GetValue(Settings.RegAppName);

            if (startPath != registeredStartPath)
            {
                // (re-)set the 
                rkApp.SetValue(Settings.RegAppName, startPath);
            }
        }

        /// <summary>
        /// Ads a firewall exception
        /// </summary>
        private static void AddFirewallException()
        {
            try
            {
                var type = Type.GetTypeFromCLSID(new Guid("{304CE942-6E39-40D8-943A-B913C40C9CD4}"));
                var firewallManager = (INetFwMgr)Activator.CreateInstance(type);
                type = Type.GetTypeFromProgID("HNetCfg.FwAuthorizedApplication");
                var authapp = (INetFwAuthorizedApplication)Activator.CreateInstance(type);
                authapp.Name = "Personal Analytics";
                authapp.ProcessImageFileName = Assembly.GetExecutingAssembly().Location; // location of application
                authapp.Scope = NET_FW_SCOPE_.NET_FW_SCOPE_ALL;
                authapp.IpVersion = NET_FW_IP_VERSION_.NET_FW_IP_VERSION_ANY;
                authapp.Enabled = true;
                firewallManager.LocalPolicy.CurrentProfile.AuthorizedApplications.Add(authapp);
            }
            catch (Exception e)
            {
                // Known exception if user doesn't run the application as an admin
                Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// On Shutdown all trackers are stopped and the database closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            //DisposeTaskbarIcon();
            TrackerManager.GetInstance().Stop();
            Database.GetInstance().Disconnect();
        }

        private static void DisposeTaskbarIcon()
        {
            var taskbarIcon = TrackerManager.GetInstance().TaskbarIcon;
            if (taskbarIcon != null)
            {
                taskbarIcon.Dispatcher.Invoke(taskbarIcon.Dispose);
            }
        }

        /// <summary>
        /// Handles all the exceptions that are not caught by the application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                Database.GetInstance().LogError(string.Format("An error occurred. Please see the log file. ({0})", e.Exception.Message));
            }
            catch (Exception e2) { Logger.WriteToLogFile(e2); }

            // Write to the logfile
            Logger.WriteToLogFile(e.Exception);

            // Tell the user
            MessageBox.Show("Oops, something really bad happened. Please try again later. If the problem persists, please contact us via andre.meyer@uzh.ch and attach the logfile.",
                "Error", MessageBoxButton.OK);


            // Prevent default unhandled exception processing
            e.Handled = true;
        }
    }
}
