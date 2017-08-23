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
        /// (re-)starts the timer (as usual)
        /// and also sets the timestamp for the next popup
        /// </summary>
        private void StartPopUpTimer()
        {
            _popUpTimer.Start();
            _nextPopUp = DateTime.Now.Add(Settings.PopUpInterval);
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

        private async void PopUp_Tick(object sender, EventArgs e)
        {
            // stop pop-up timer
            _popUpTimer.Stop();

            // get session start and end
            //var sessionStart = _lastPopUpResponse;
            //if (_lastPopUpResponse == DateTime.MinValue || _lastPopUpResponse.Date != DateTime.Now.Date)
            //{
            //    sessionStart = DateTime.Now.AddHours(- Settings.MaximumValidationInterval.TotalHours);
            //}

            //// make sure, sessionStart is not too long
            //var sessionEnd = DateTime.Now;
            //if (sessionEnd - sessionStart > Settings.MaximumValidationInterval)
            //{
            //    sessionStart = sessionEnd - Settings.MaximumValidationInterval;
            //}

            var sessionStart = DateTime.Now.AddHours(-Settings.MaximumValidationInterval.TotalHours);
            var sessionEnd = DateTime.Now;

            // if it's not 1/10th of the validation interval, don't show the pop-up
            if ((sessionEnd - sessionStart).TotalHours * 10 < Settings.MaximumValidationInterval.TotalHours)
            {
                StartPopUpTimer();
                return;
            }
            else
            {
                // load all data first
                var taskDetections = await Task.Run(() => PrepareTaskDetectionDataForPopup(sessionStart, sessionEnd));

                // show pop-up 
                ShowTaskDetectionValidationPopup(taskDetections, sessionStart, sessionEnd);
            }            
        }

        /// <summary>
        /// Show the task detection validation for the time since the last 
        /// time the participant answered the popup
        /// </summary>
        /// <returns></returns>
        private List<TaskDetection> PrepareTaskDetectionDataForPopup(DateTime sessionStart, DateTime sessionEnd)
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
                    //TODO Andre: use file and website extractor here

                    // run task detection
                    var td = new TaskDetectorImpl();
                    taskDetections = td.FindTasks(processes);
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
        private void TempShowHiddenWindow()
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
                Thread.Sleep(2000);
                tempWindow.Close();
            }));

        }

        /// <summary>
        /// Shows a popup with all detected task switches and asks the user
        /// to validate them. The response is handled in a separate method.
        /// </summary>
        /// <param name="taskDetections"></param>
        private void ShowTaskDetectionValidationPopup(List<TaskDetection> taskDetections, DateTime detectionSessionStart, DateTime detectionSessionEnd)
        {
            if (taskDetections.Count > 0)
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(
                    () =>
                    {
                        // filter task detections
                        var taskDetections_Validated = taskDetections.Where(t => (t.End - t.Start).TotalSeconds >= Settings.MinimumTaskDurationInSeconds).ToList();
                        var taskDetections_NotValidated = taskDetections.Where(t => (t.End - t.Start).TotalSeconds < Settings.MinimumTaskDurationInSeconds).ToList();

                        // create validation popup
                        var popup = new TaskDetectionPopup(taskDetections_Validated);

                        // show popup & handle response
                        if (popup.ShowDialog() == true)
                        {
                            HandlePopUpResponse(popup, taskDetections_Validated, taskDetections_NotValidated, detectionSessionStart, detectionSessionEnd);
                        }
                        else
                        {
                            // we get here when DialogResult is set to false (which should never happen) 
                            Database.GetInstance().LogErrorUnknown("DialogResult of PopUp was set to false in tracker: " + Name);
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
            // no tasks in timeline detected
            else
            {
                var msg = string.Format("No tasks detected between {0} {1} and {2} {3}.", detectionSessionStart.ToShortDateString(), detectionSessionStart.ToShortTimeString(), detectionSessionEnd.ToShortDateString(), detectionSessionEnd.ToShortTimeString());
                Database.GetInstance().LogWarning(msg);
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

                // save validation responses to the database
                var sessionId = DatabaseConnector.TaskDetectionSession_SaveToDatabase(detectionSessionStart, detectionSessionEnd, DateTime.Now, popup.Comments.Text);
                if (sessionId > 0) DatabaseConnector.TaskDetectionValidationsPerSession_SaveToDatabase(sessionId, taskDetections);
                else Database.GetInstance().LogWarning("Did not save any validated task detections for session (" + detectionSessionStart.ToString() + " to " + detectionSessionEnd.ToString() + ") due to an error.");

                // next popup will start from this timestamp
                _lastPopUpResponse = detectionSessionEnd;

                // set timer interval to regular interval
                _popUpTimer.Interval = Settings.PopUpInterval;
            } 
            else
            {
                // we get here when DialogResult is set to false (which never happens) 
                Database.GetInstance().LogErrorUnknown("User closed the PopUp without completing the validation in tracker: " + Name);

                // set timer interval to a short one, to try again
                _popUpTimer.Interval = Settings.PopUpReminderInterval;
            }

            // restart timer
            StartPopUpTimer();
        }
    }
}