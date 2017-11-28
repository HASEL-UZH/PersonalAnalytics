// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-15
// 
// Licensed under the MIT License.

using Shared;
using Shared.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TaskDetectionTracker.Algorithm;
using TaskDetectionTracker.Data;
using TaskDetectionTracker.Helpers;
using TaskDetectionTracker.Model;
using TaskDetectionTracker.Views;

namespace TaskDetectionTracker
{
    public class Deamon : BaseTracker, ITracker
    {
        private DispatcherTimer _popUpTimer;
        private DateTime _lastPopUpResponse = DateTime.MinValue;
        private DateTime _nextPopUp = DateTime.MinValue;

        #region ITracker Stuff

        public Deamon()
        {
            Name = Settings.TrackerName;
        }

        public override void Start()
        {
            if (_popUpTimer != null)
            {
                Stop(); // stop timers
            }

            // initialize the popup timer
            _popUpTimer = new DispatcherTimer();
            _popUpTimer.Interval = Settings.PopUpInterval;
            _popUpTimer.Tick += PopUp_Tick;
            StartPopUpTimer();

            IsRunning = true;
        }

        /// <summary>
        /// (re-)starts the timer with a given interval
        /// and also sets the timestamp for the next popup
        /// </summary>
        private void StartPopUpTimer(TimeSpan interval)
        {
            _popUpTimer.Interval = interval;
            _popUpTimer.Start();
            _nextPopUp = DateTime.Now.Add(interval);
        }

        /// <summary>
        /// (re-)starts the timer with the default interval Settings.PopUpInterval
        /// </summary>
        private void StartPopUpTimer()
        {
            StartPopUpTimer(Settings.PopUpInterval);
        }

        public override void Stop()
        {
            if (_popUpTimer != null)
            {
                _popUpTimer.Stop();
                _popUpTimer = null;
            }

            IsRunning = false;
        }

        public override void CreateDatabaseTablesIfNotExist()
        {
            DatabaseConnector.CreateTaskDetectionValidationTable();
        }

        public override string GetVersion()
        {
            var v = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;
            return Shared.Helpers.VersionHelper.GetFormattedVersion(v);
        }

        public override bool IsEnabled()
        {
            // no settings, meaning: the user cannot disable it
            return Settings.IsEnabledByDefault;
        }

        public override void UpdateDatabaseTables(int version)
        {
            //not needed
        }

        public override string GetStatus()
        {
            var _nextPopUpString = (_nextPopUp > DateTime.Now) ? _nextPopUp.ToLongTimeString() : "Due now. Please respond!";
            return (!IsRunning || _popUpTimer == null)
                ? Name + " is NOT running"
                : Name + " is running. Next task detection validation at " + _nextPopUpString + ".";
        }

        #endregion

        #region Create and Handle Validator PopUp

        private async void PopUp_Tick(object sender, EventArgs e)
        {
            // stop pop-up timer
            _popUpTimer.Stop();

            var sessionStart = DateTime.Now.AddHours(-Settings.MaximumValidationInterval.TotalHours);
            var sessionEnd = DateTime.Now;

            // the first two times the pop-up is shown, there should be no task predictions
            var numberOfValidationsCompleted = Database.GetInstance().GetSettingsInt(Settings.NumberOfValidationsCompleted_Setting, 0);
            var isCurrentPopupFirstTimeWithPredictions = numberOfValidationsCompleted == Settings.NumberOfPopUpsWithoutPredictions;
            var validatePopUpWithPredictions = (numberOfValidationsCompleted >= Settings.NumberOfPopUpsWithoutPredictions);

            // load all data first
            var taskDetections = await Task.Run(() => PrepareTaskDetectionDataForPopup(sessionStart, sessionEnd, validatePopUpWithPredictions));

            // don't show the pop-up if...
            if (taskDetections.Count == 0 || // if there are no predictions
                ((taskDetections.Last().End - taskDetections.First().Start).TotalHours * 5 < Settings.MaximumValidationInterval.TotalHours) || // if it's not 1/5th of the validation interval
                (taskDetections.Sum(t => (t.End - t.Start).TotalSeconds) < 10 * 60)) // if the total duration of the detected tasks is 10 minutes
            {
                var msg = string.Format("No tasks detected or too short interval between {0} {1} and {2} {3}.", sessionStart.ToShortDateString(), sessionStart.ToShortTimeString(), sessionEnd.ToShortDateString(), sessionEnd.ToShortTimeString());
                Database.GetInstance().LogWarning(msg);
                StartPopUpTimer();
            }
            // show pop-up if enough data is available
            else
            {
                ShowTaskDetectionValidationPopup(taskDetections, sessionStart, sessionEnd, isCurrentPopupFirstTimeWithPredictions);
            }
        }

        /// <summary>
        /// Show the task detection validation for the time since the last 
        /// time the participant answered the popup
        /// </summary>
        /// <returns></returns>
        private static List<TaskDetection> PrepareTaskDetectionDataForPopup(DateTime sessionStart, DateTime sessionEnd, bool validatePopUpWithPredictions)
        {
            var taskDetections = new List<TaskDetection>();

            try
            {
                // temporarily show hidden window
                TempShowHiddenWindow();

                // get processes in interval
                var processes = DatabaseConnector.GetProcesses(sessionStart, sessionEnd);
                if (processes.Count > 0)
                {
                    // merge processes
                    processes = DataMerger.MergeProcesses(processes, sessionEnd.Subtract(sessionStart));
                    DataMerger.AddMouseClickAndKeystrokesToProcesses(processes);
                    //option to use file and website extractor here to add more info to the timeline-hover

                    // run task detection and show predictions
                    if (validatePopUpWithPredictions)
                    { 
                        var td = new TaskDetectorImpl();
                        taskDetections = td.FindTasks(processes);
                    }
                    // show empty prediction (just last item)
                    else
                    {
                        taskDetections.Add(new TaskDetection(processes.First().Start, processes.Last().End, TaskTypes.Observation, TaskTypes.Other, processes, false));
                    }                    
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return taskDetections;
        }

        /// <summary>
        /// temporarily show hidden window 
        /// (to add an entry to the windows_activity table and not miss the last item)
        /// </summary>
        private static void TempShowHiddenWindow()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(
            () =>
            {
                var tempWindow = new Window()
                {
                    Width = 0,
                    Height = 0,
                    WindowStyle = WindowStyle.None,
                    ShowInTaskbar = false,
                    //ShowActivated = false,
                    Title = "PersonalAnalytics: Forced WindowsActvityTracker-entry"
                };
                tempWindow.Show();
                Thread.Sleep(1500);
                tempWindow.Close();
            }));
        }

        /// <summary>
        /// Shows a popup with all detected task switches and asks the user
        /// to validate them. The response is handled in a separate method.
        /// </summary>
        /// <param name="taskDetections"></param>
        /// <param name="detectionSessionStart"></param>
        /// <param name="detectionSessionEnd"></param>
        /// <param name="isCurrentPopupFirstTimeWithPredictions"></param>
        private void ShowTaskDetectionValidationPopup(List<TaskDetection> taskDetections, DateTime detectionSessionStart, DateTime detectionSessionEnd, bool isCurrentPopupFirstTimeWithPredictions)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(
                () =>
                {
                    // filter task detections (remove too short ones)
                    var taskDetections_Validated = taskDetections.Where(t => (t.End - t.Start).TotalSeconds >= Settings.MinimumTaskDuration_Seconds).ToList();
                    var taskDetections_NotValidated = taskDetections.Where(t => (t.End - t.Start).TotalSeconds < Settings.MinimumTaskDuration_Seconds).ToList();

                    // create validation popup
                    var popup = new TaskDetectionPopup(taskDetections_Validated, isCurrentPopupFirstTimeWithPredictions);
                    popup.Topmost = true;

                    // show popup & handle response
                    if (popup.ShowDialog() == true)
                    {
                        HandlePopUpResponse(popup, popup.TaskSwitchesValidated, taskDetections_NotValidated, detectionSessionStart, detectionSessionEnd);
                    }
                    else
                    {
                        // we get here when DialogResult is set to false (which should only happen when pop-up is not answered)
                        Database.GetInstance().LogInfo("DialogResult of PopUp was set to false in tracker: " + Name);
                        StartPopUpTimer();
                    }
                }));
            }
            catch (ThreadAbortException e)
            {
                Database.GetInstance().LogError(Name + ": " + e.Message);
                StartPopUpTimer();
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                StartPopUpTimer();
            }
        }

        /// <summary>
        /// Handles the popup response.
        /// - if answered: stores the validation in the database
        /// - else: re-opens the window and asks the user to do it again
        /// </summary>
        /// <param name="taskDetectionPopup"></param>
        /// <param name="popup"></param>
        private void HandlePopUpResponse(TaskDetectionPopup popup, List<TaskDetection> taskDetections_Validated, List<TaskDetection> taskDetections_NotValidated, DateTime detectionSessionStart, DateTime detectionSessionEnd)
        {
            // successful popup response
            if (popup.ValidationComplete)
            {
                // merge non-validated task detections with validated ones
                var taskDetections = taskDetections_Validated.Concat(taskDetections_NotValidated).ToList();
                taskDetections.Sort();

                // save validation responses to the database
                var sessionId = DatabaseConnector.TaskDetectionSession_SaveToDatabase(detectionSessionStart, detectionSessionEnd, DateTime.Now, 
                    popup.Comments.Text, (int)popup.Confidence_TaskSwitch, (int)popup.Confidence_TaskType);
                if (sessionId > 0) DatabaseConnector.TaskDetectionValidationsPerSession_SaveToDatabase(sessionId, taskDetections);
                else Database.GetInstance().LogError("Did not save any validated task detections for session (" + detectionSessionStart.ToString() + " to " + detectionSessionEnd.ToString() + ") due to an error.");

                // log successful validation
                var db = Database.GetInstance();
                db.LogInfo("User successfully validated task switches for session (" + detectionSessionStart.ToString() + " to " + detectionSessionEnd.ToString() + ").");
                var numberOfValidationsCompleted = 1 + db.GetSettingsInt(Settings.NumberOfValidationsCompleted_Setting, 0);
                db.SetSettings(Settings.NumberOfValidationsCompleted_Setting, numberOfValidationsCompleted.ToString());

                // next popup will start from this timestamp
                _lastPopUpResponse = detectionSessionEnd;

                // set timer interval to regular interval
                StartPopUpTimer(Settings.PopUpInterval);
            } 
            else
            {
                // we get here when DialogResult is set to false
                Database.GetInstance().LogInfo("User closed the PopUp without completing the validation in tracker: " + Name);

                // set timer interval to a short one, to try again
                StartPopUpTimer(Settings.PopUpReminderInterval_Long);
            }
        }

        #endregion
    }
}