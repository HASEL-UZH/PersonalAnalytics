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
using TaskDetectionTracker.Visualizations;

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

            // log current state
            var numberOfValidationsCompleted = Database.GetInstance().GetSettingsInt(Settings.NumberOfValidationsCompleted_Setting, 0);
            Database.GetInstance().LogInfo(Name + " is running. " + numberOfValidationsCompleted + " pop-ups completed so far. After " + 
                Settings.NumberOfPopUpsWithoutPredictions + " pop-ups predictions will be shown to validate.");

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
            DatabaseConnector.CreateTaskDetectionValidationTables();
        }

        public override string GetVersion()
        {
            var v = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;
            return Shared.Helpers.VersionHelper.GetFormattedVersion(v);
        }

        public override List<IVisualization> GetVisualizationsDay(DateTimeOffset date)
        {
            var vis1 = new DayNumberOfValidationsCompleted();
            return new List<IVisualization> { vis1 };
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
            var _nextPopUpString = (_nextPopUp > DateTime.Now) ? ("at " + _nextPopUp.ToLongTimeString()) : "due now. Please respond!";

            return (!IsRunning || _popUpTimer == null)
                ? Name + " is NOT running"
                : Name + " is running. Next task detection pop-up is " + _nextPopUpString + ". You answered it " + Database.GetInstance().GetSettingsInt(Settings.NumberOfValidationsCompleted_Setting, 0) + " times so far.";
        }

        #endregion

        #region Create and Handle Validator PopUp

        private async void PopUp_Tick(object sender, EventArgs e)
        {
            // stop pop-up timer
            _popUpTimer.Stop();

            var sessionStart = DateTime.Now.AddHours(-Settings.MaximumValidationInterval.TotalHours);
            var sessionEnd = DateTime.Now;

            // save session infos to database
            var sessionId = DatabaseConnector.TaskDetectionSession_Insert_SaveToDatabase(sessionStart, sessionEnd, DateTime.Now);
            if (sessionId == 0)
            {
                // saving session infos to database failed, skip this validation
                Database.GetInstance().LogInfo(Name + ": Did not save any validated task detections for session (" + sessionStart + " to " + sessionEnd + ") due to an error in saving the sessionId.");
                StartPopUpTimer();
                // don't show pop-up, stop here
            }
            // saving the session was successful, continue with validation
            else
            {
                // at the beginning of the study (i.e. Settings.NumberOfPopUpsWithoutPredictions times), we don't want to show task detection predictions
                var numberOfValidationsCompleted = Database.GetInstance().GetSettingsInt(Settings.NumberOfValidationsCompleted_Setting, 0);
                var isCurrentPopupFirstTimeWithPredictions = numberOfValidationsCompleted == Settings.NumberOfPopUpsWithoutPredictions;
                var validatePopUpWithPredictions = (numberOfValidationsCompleted >= Settings.NumberOfPopUpsWithoutPredictions);

                // prepare task detection predictions for validation
                var taskDetections = await Task.Run(() => PrepareTaskDetectionDataForPopup(validatePopUpWithPredictions, sessionId, sessionStart, sessionEnd));

                // don't show the pop-up if...
                if (taskDetections.Count == 0 || // if there are no predictions
                    ((taskDetections.Last().End - taskDetections.First().Start).TotalHours * 5 < Settings.MaximumValidationInterval.TotalHours) || // if it's not 1/5th of the validation interval
                    (taskDetections.Sum(t => t.Duration_InSeconds()) < 10 * 60)) // if the total duration of the detected tasks is 10 minutes
                {
                    var msg = string.Format(Name + ": No tasks detected or too short interval between {0} {1} and {2} {3}.", sessionStart.ToShortDateString(), sessionStart.ToShortTimeString(), sessionEnd.ToShortDateString(), sessionEnd.ToShortTimeString());
                    Database.GetInstance().LogWarning(msg);
                    StartPopUpTimer();
                }
                // show pop-up if enough data is available
                else
                {
                    ShowTaskDetectionValidationPopup(isCurrentPopupFirstTimeWithPredictions, sessionId, taskDetections, sessionStart, sessionEnd);
                }
            }
        }

        /// <summary>
        /// Fetches all processes for the session and runs the task detection (switch and type prediction).
        /// Stores the predictions.
        /// If validatePopUpWithPredictions is true, return the predicted tasks, else return an empty list.
        /// </summary>
        /// <returns></returns>
        private static List<TaskDetection> PrepareTaskDetectionDataForPopup(bool validatePopUpWithPredictions, int sessionId, DateTime sessionStart, DateTime sessionEnd)
        {
            var tasksToValidate = new List<TaskDetection>();

            try
            {
                // temporarily show hidden window
                TempShowHiddenWindow();

                // get processes in interval
                var processes = DatabaseConnector.GetProcesses(sessionStart, sessionEnd);
                if (processes.Count == 0) return new List<TaskDetection>(); // return empty list

                // merge processes
                processes = DataMerger.MergeProcesses(processes, sessionEnd.Subtract(sessionStart));
                DataMerger.AddMouseClickAndKeystrokesToProcesses(processes);
                // option to use file and website extractor here to add more info to the timeline-hover

                // run task detection and show predictions
                var td = new TaskDetectorImpl();
                var taskPredictions = td.FindTasks(processes);

                // save original task (type and switch) predictions
                DatabaseConnector.TaskDetectionPredictionsPerSession_SaveToDatabase(sessionId, taskPredictions);

                // show user predicted tasks for validation
                if (validatePopUpWithPredictions)
                {
                    tasksToValidate = taskPredictions;
                }
                // show empty prediction (just one task until interruption at popup-time)
                else
                {
                    tasksToValidate.Add(new TaskDetection(processes.First().Start, processes.Last().End, TaskType.Other, TaskType.Other, processes));
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }

            return tasksToValidate;
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
        /// <param name="taskDetections_predicted"></param>
        /// <param name="detectionSessionStart"></param>
        /// <param name="detectionSessionEnd"></param>
        /// <param name="isCurrentPopupFirstTimeWithPredictions"></param>
        private void ShowTaskDetectionValidationPopup(bool isCurrentPopupFirstTimeWithPredictions, int sessionId, List<TaskDetection> taskDetections_predicted, DateTime detectionSessionStart, DateTime detectionSessionEnd)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(
                () =>
                {

                    // filter task detections (remove too short ones), merge them with next task
                    for (var i = 0; i < taskDetections_predicted.Count; i++)
                    {
                        var thisTask = taskDetections_predicted[i];

                        // skip only if task is long enough
                        if (thisTask.Duration_InSeconds() >= Settings.MinimumTaskDuration_Seconds) continue;

                        TaskDetection nextTask = null;
                        if (i + 1 < taskDetections_predicted.Count)
                        {
                            nextTask = taskDetections_predicted[i + 1];  // default: next task
                        }
                        else
                        {
                            var j = i;
                            while (taskDetections_predicted[j].Duration_InSeconds() < Settings.MinimumTaskDuration_Seconds)
                            {
                                nextTask = taskDetections_predicted[j - 1]; // previous task (only for last item)
                                j--;
                            }
                        }

                        // merge the too short task with the next one
                        nextTask.TimelineInfos.AddRange(thisTask.TimelineInfos);
                        nextTask.TimelineInfos.Sort();
                        nextTask.Start = nextTask.TimelineInfos.First().Start;
                        nextTask.End = nextTask.TimelineInfos.Last().End;
                    }

                    // remove all to short tasks and create new list
                    var taskDetections_toValidate = taskDetections_predicted.Where(t => t.Duration_InSeconds() >= Settings.MinimumTaskDuration_Seconds).ToList();

                    // create validation popup
                    var popup = new TaskDetectionPopup(taskDetections_toValidate, isCurrentPopupFirstTimeWithPredictions) { Topmost = true };
                    Database.GetInstance().LogInfo(Name + ": Show fresh/new task detection validation PopUp to user.");

                    // show popup & handle response
                    if (popup.ShowDialog() == true)
                    {
                        HandlePopUpResponse(popup, sessionId, popup.TaskSwitchesValidated, detectionSessionStart, detectionSessionEnd);
                    }
                    else if (popup.WasPostponed)
                    {
                        // we get here when DialogResult is set to false
                        Database.GetInstance().LogInfo(Name + ": User postponed the PopUp for (" + Settings.PopUpReminderInterval_Long + "h) without completing the validation.");

                        // save empty validation responses to the task validation table
                        DatabaseConnector.TaskDetectionSession_Update_SaveToDatabase(sessionId, DateTime.Now, popup.PostponedInfo, string.Empty, -1, -1);

                        // set timer interval to come up again in several hours
                        StartPopUpTimer(Settings.PopUpReminderInterval_Long);
                    }

                    else
                    {
                        // save empty validation responses to the task validation table
                        DatabaseConnector.TaskDetectionSession_Update_SaveToDatabase(sessionId, DateTime.Now, popup.PostponedInfo, string.Empty, -1, -1);

                        // we get here when DialogResult is set to false (which should only happen when pop-up is not answered)
                        Database.GetInstance().LogInfo(Name + ": DialogResult of PopUp was set to false.");
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
        /// <param name="popup"></param>
        /// <param name="taskDetections_Validated"></param>
        /// <param name="taskDetections_NotValidated"></param>
        /// <param name="detectionSessionStart"></param>
        /// <param name="detectionSessionEnd"></param>
        private void HandlePopUpResponse(TaskDetectionPopup popup, int sessionId, List<TaskDetection> taskDetections_Validated, DateTime detectionSessionStart, DateTime detectionSessionEnd)
        {
            var timePopUpResponded = DateTime.Now;

            // successful popup response
            if (popup.ValidationComplete)
            {
                // save validation responses to task validation table
                DatabaseConnector.TaskDetectionSession_Update_SaveToDatabase(sessionId, timePopUpResponded, popup.PostponedInfo, popup.Comments.Text, popup.Confidence_TaskSwitch, popup.Confidence_TaskType);
                DatabaseConnector.TaskDetectionValidationsPerSession_SaveToDatabase(sessionId, taskDetections_Validated);

                // log successful validation
                var db = Database.GetInstance();
                db.LogInfo(Name + ": User successfully validated task switches for session (" + detectionSessionStart + " to " + detectionSessionEnd + ").");
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
                Database.GetInstance().LogInfo(Name + ": User closed the PopUp without completing the validation.");

                // set timer interval to a short one, to try again
                StartPopUpTimer(Settings.PopUpReminderInterval_Middle);
            }
        }

        #endregion
    }
}