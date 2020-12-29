// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using Shared;
using Shared.Data;
using System.Globalization;
using PersonalAnalytics.Helpers;
using System.Collections.Generic;
using System.Deployment.Application;
using Shared.Helpers;

namespace PersonalAnalytics
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
    {
        private const string UniqueAppName = "AM.PersonalAnalytics"; // unique app name needed to only have one instance at a time

        //public App()
        [STAThread]
        public static void Main(string[] args)
        {
            // before actually starting up, check if there is already an instance running
            if (SingleInstance<App>.InitializeAsFirstInstance(UniqueAppName))
            {
                var application = new App();
                application.InitializeComponent();
                application.Run();

                // Allow single instance code to perform cleanup operations
                SingleInstance<App>.Cleanup();

                // then do the rest
                if (null == Current)
                {
                    new Application();
                }

                Current.DispatcherUnhandledException += App_DispatcherUnhandledException;
                Current.SessionEnding += App_SessionEnding;
            }
            //else
            //{
            //    var msg = "An instance of PersonalAnalytics was already running. Access it from the task bar on the bottom right of your screen.";
            //    MessageBox.Show("Info", msg);
            //}
        }

        /// <summary>
        /// This method is used to handle the command line arguments of the
        /// second instance. 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            if (! Retrospection.Handler.GetInstance().OpenRetrospection())
            {
                var msg = "PersonalAnalytics is already running in the background! Access the it from the task bar icon on the bottom right of your screen.";
                MessageBox.Show(msg, "PersonalAnalytics - An instance is already running");
                // todo: show balloontooltip
            }

            return true;
        }

        /// <summary>
        /// OnStartup registers all trackers to be available to the user
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            // Create log directory if it doesn't already exist
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PersonalAnalytics");
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }

            //////////////////////////////////////////////////////
            // Various Initializations
            //////////////////////////////////////////////////////

            // don't automatically shut down
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Initialize & Connect the database
            Database.GetInstance().Connect();

            // prepare settings
            TrackerManager.GetInstance().PrepareSettings();

            // register app for PC startup
            RegisterAppForPcStartup();

            // add a firewall exception
            //AddFirewallException(); // disabled because it causes problems if not system admin

            //////////////////////////////////////////////////////
            // initialize task bar icon & context menu
            //////////////////////////////////////////////////////
            TrackerManager.GetInstance().InitializeTaskBarIcon();

            //////////////////////////////////////////////////////
            // Start Tracker Manager (i.e. the monitoring tool)
            //////////////////////////////////////////////////////
            var trackers = TrackerManager.GetInstance().RegisterTrackers();
            TrackerManager.GetInstance().SetAppVersion(GetPublishedAppVersion());
            TrackerManager.GetInstance().Start();

            //////////////////////////////////////////////////////
            // Start the Retrospection
            //////////////////////////////////////////////////////
            if (Retrospection.Settings.IsEnabled)
            {
                Retrospection.Handler.GetInstance().Start(trackers, GetPublishedAppVersion()); // register the same trackers from the monitoring tool for the retrospection
            }
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
        //private static void AddFirewallException()
        //{
        //    try
        //    {
        //        var type = Type.GetTypeFromCLSID(new Guid("{304CE942-6E39-40D8-943A-B913C40C9CD4}"));
        //        var firewallManager = (INetFwMgr)Activator.CreateInstance(type);
        //        type = Type.GetTypeFromProgID("HNetCfg.FwAuthorizedApplication");
        //        var authapp = (INetFwAuthorizedApplication)Activator.CreateInstance(type);
        //        authapp.Name = "PersonalAnalytics";
        //        authapp.ProcessImageFileName = Assembly.GetExecutingAssembly().Location; // location of application
        //        authapp.Scope = NET_FW_SCOPE_.NET_FW_SCOPE_ALL;
        //        authapp.IpVersion = NET_FW_IP_VERSION_.NET_FW_IP_VERSION_ANY;
        //        authapp.Enabled = true;
        //        firewallManager.LocalPolicy.CurrentProfile.AuthorizedApplications.Add(authapp);
        //    }
        //    catch (Exception e)
        //    {
        //        // Known exception if user doesn't run the application as an admin
        //        Logger.WriteToLogFile(e);
        //    }
        //}

        /// <summary>
        /// Application is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            TrackerManager.GetInstance().Stop();
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
                Database.GetInstance().LogError(string.Format(CultureInfo.InvariantCulture, "An error occurred. Please see the log file. ({0})", e.Exception.Message));
            }
            catch (Exception e2) { Logger.WriteToLogFile(e2); }

            // Write to the logfile
            Logger.WriteToLogFile(e.Exception);

            // Tell the user
            MessageBox.Show("Oops, something really bad happened. Please try again later. If the problem persists, please contact us via " + Settings.EmailAddress1 + " and attach the logfile.",
                "Error", MessageBoxButton.OK);


            // Prevent default unhandled exception processing
            e.Handled = true;
        }

        /// <summary>
        /// Gets and Formats the currently published
        /// application version.
        /// </summary>
        /// <returns></returns>
        private static string GetPublishedAppVersion()
        {
            Version v = null;
            if (ApplicationDeployment.IsNetworkDeployed)
            {
                v = ApplicationDeployment.CurrentDeployment.CurrentVersion;
            }
            return VersionHelper.GetFormattedVersion(v);
        }
    }
}
