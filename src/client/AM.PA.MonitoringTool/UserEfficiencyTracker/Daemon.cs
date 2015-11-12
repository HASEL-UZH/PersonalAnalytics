// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.Data;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Shared;
using Shared.Data;
using UserEfficiencyTracker.Models;

namespace UserEfficiencyTracker
{
    public class Daemon : BaseTracker, ITracker
    {
        private DispatcherTimer _timer;
        private static TimeSpan _timeRemainingUntilNextSurvey;

        #region METHODS

        #region ITracker Stuff

        public Daemon()
        {
            Name = "User Efficiency Survey";

            _timeRemainingUntilNextSurvey = Settings.GetDefaultInterval();
        }

        public override void Start()
        {
            if (_timer != null)
                Stop();
            _timer = new DispatcherTimer();
            _timer.Interval = Settings.SurveyCheckerInterval;
            _timer.Tick += TimerTick;
            _timer.Start();

            IsRunning = true;
        }

        public override void Stop()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }

            IsRunning = false;
        }

        public override void CreateDatabaseTablesIfNotExist()
        {
            //AM: added some empty fields for additional questions in the future
            Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.DbTable + " (id INTEGER PRIMARY KEY, time TEXT, surveyNotifyTime TEXT, surveyStartTime TEXT, surveyEndTime TEXT, userProductivity NUMBER, userTasksWorkedOn TEXT, userComments TEXT, userSatisfaction NUMBER, userEmotions NUMBER, userTaskDifficulty NUMBER, userSlowNetwork NUMBER, column1 TEXT, column2 TEXT, column3 TEXT, column4 TEXT, column5 TEXT, column6 TEXT, column7 TEXT, column8 TEXT )");
        }

        public override string GetStatus()
        {
            var nextSurveyTs = DateTime.Now.Add(_timeRemainingUntilNextSurvey);
            return ! IsRunning
                ? Name + " is NOT running"
                : Name + " is running. Next mini-survey at " + nextSurveyTs.ToShortDateString() + " " + nextSurveyTs.ToShortTimeString() + ".";
        }

        public override bool IsEnabled()
        {
            return Settings.IsEnabled;
        }

        #endregion

        #region Daemon

        /// <summary>
        /// loop runs in a separate thread
        /// </summary>
        private void TimerTick(object sender, EventArgs args)
        {
            // only show survey when its ready to be shown
            if (_timeRemainingUntilNextSurvey > Settings.SurveyCheckerInterval)
            {
              _timeRemainingUntilNextSurvey = _timeRemainingUntilNextSurvey.Subtract(Settings.SurveyCheckerInterval);
                return;
            }

            // show survey
            try
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(
                    () =>
                    {
                        var notifyTimeStamp = DateTime.Now;
                        var notify = new UserSurveyNotification();
                        if (notify.ShowDialog() == true)
                        {
                            if (notify.TakeSurveyNow)
                            {
                                RunSurvey(notifyTimeStamp);
                            }
                            else if (notify.PostPoneSurvey != PostPoneSurvey.None)
                            {
                                PostponeSurvey(notify);
                            }
                            else
                            {
                                _timeRemainingUntilNextSurvey = Settings.PostponeShortInterval;
                            }
                        }
                        else
                        {
                            //TODO: what happens here?
                            Database.GetInstance().LogErrorUnknown(Name);
                        }
                    }));

                
            }
            catch (ThreadAbortException e) { Database.GetInstance().LogError(Name + ": " + e.Message); }
            catch (Exception e) { Database.GetInstance().LogError(Name + ": " + e.Message); }
        }

        /// <summary>
        /// handler to postpone the survey for the selected time
        /// Hint: the selected time (e.g. postpone 1 hour) equals 1 hour of computer running (i.e. developer working) time
        /// </summary>
        /// <param name="notify"></param>
        private static void PostponeSurvey(UserSurveyNotification notify)
        {
            switch (notify.PostPoneSurvey)
            {
                case (PostPoneSurvey.PostponeShort):
                    _timeRemainingUntilNextSurvey = Settings.PostponeShortInterval;
                    break;
                case (PostPoneSurvey.PostponeHour):
                    _timeRemainingUntilNextSurvey = TimeSpan.FromHours(1); // in one hour
                    break;
                case (PostPoneSurvey.PostponeDay):
                    _timeRemainingUntilNextSurvey = TimeSpan.FromHours(6); // in one workday
                    //var now = DateTime.Now;
                    //var nextDay = now.AddDays(1).Date.AddHours(8); //next day at 8 o'clock
                    //var totalHours = (nextDay - now).TotalHours;
                    //_timeRemainingUntilNextSurvey = TimeSpan.FromHours(totalHours);
                    break;
                default:
                    _timeRemainingUntilNextSurvey = Settings.PostponeShortInterval;
                    break;
            }
        }

        /// <summary>
        /// manually run survey (click on ContextMenu)
        /// </summary>
        public void ManualTakeSurveyNow()
        {
            RunSurvey(DateTime.Now);
        }

        /// <summary>
        /// runs the survey and handles the response
        /// </summary>
        /// <returns></returns>
        private static bool RunSurvey(DateTime notifyTimeStamp)
        {
            var res = false;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var neededSurveyWindowData = GetPreviousSurveyEntry(notifyTimeStamp);
                var dialog = new UserSurveyWindow(neededSurveyWindowData);
                res = dialog.ShowDialog() == true
                    ? HandleUserAnswered(dialog)
                    : HandleUserDidntAnswerSurvey();
            });

            // ask again in a couple of minutes if survey wasn't filled out
            //_timeRemainingUntilNextSurvey = (res ? Settings.DefaultInterval : Settings.PostponeShortInterval);
            _timeRemainingUntilNextSurvey = Settings.GetDefaultInterval();

            return res;
        }

        /// <summary>
        /// returns the previous survey entry or a default (empty) entry item
        /// </summary>
        /// <returns></returns>
        private static NeededSurveyWindowData GetPreviousSurveyEntry(DateTime notifyTimeStamp)
        {
            var data = new NeededSurveyWindowData {CurrentSurveyEntryNotificationTimeStamp = notifyTimeStamp};
            data = GetLastSurveyEntriesFromDatabase(data);
            data = SetDefaultSurveyValuesIfNeeded(data);
            return data;
        }
        
        /// <summary>
        /// set needed default values for survey window
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static NeededSurveyWindowData SetDefaultSurveyValuesIfNeeded(NeededSurveyWindowData data)
        {
            if (data.PreviousSurveyEntryProductivity == 0)
            {
                data.PreviousSurveyEntryProductivity = 4; // neutral
            }
            if (data.PreviousSurveyEntrySatisfaction == 0)
            {
                data.PreviousSurveyEntrySatisfaction = 4; // neutral
            }
            if (data.PreviousSurveyEntryTaskDifficulty == 0)
            {
                data.PreviousSurveyEntryTaskDifficulty = 4; // neutral
            }
            if (data.PreviousSurveyEntryInterruptibility == 0)
            {
                data.PreviousSurveyEntryInterruptibility = 3; // neutral
            }
            
            return data;
        }

        /// <summary>
        /// Get the last suvey entries from the database
        /// to populate the survey window
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static NeededSurveyWindowData GetLastSurveyEntriesFromDatabase(NeededSurveyWindowData data)
        {
            var res = Database.GetInstance().ExecuteReadQuery("SELECT surveyEndTime, userProductivity, userTasksWorkedOn, column1 as interruptibility, column2 as userSatisfaction, column3 as userEmotions, column4 as userTaskDifficulty FROM " + Settings.DbTable + " ORDER BY time DESC LIMIT " + Settings.NumberOfPreviousTasksShown);
            if (res == null || res.Rows.Count == 0) return data;

            if (res.Rows[0]["surveyEndTime"] != null)
            {
                try
                {
                    var val = DateTime.Parse((string) res.Rows[0]["surveyEndTime"]);
                    data.PreviousSurveyTimestamp = val;
                }
                catch { } // necessary, if we run it after the DB initialization, there is no value
            }
            if (res.Rows[0]["userProductivity"] != null)
            {
                try
                {
                    var val = Convert.ToInt32(res.Rows[0]["userProductivity"]);
                    data.PreviousSurveyEntryProductivity = val;
                }
                catch { } // necessary, if we run it after the DB initialization, there is no value
            }
            // Hint: added Dec 2014, db column: column2
            if (res.Rows[0]["userSatisfaction"] != null)
            {
                try
                {
                    var val = Convert.ToInt32(res.Rows[0]["userSatisfaction"]);
                    data.PreviousSurveyEntrySatisfaction = val;
                }
                catch { } // necessary, if we run it after the DB initialization, there is no value
            }
            // Hint: added Dec 2014, db column: column3
            if (res.Rows[0]["userEmotions"] != null)
            {
                try
                {
                    var val = Convert.ToInt32(res.Rows[0]["userEmotions"]);
                    data.PreviousSurveyEntryEmotions = val;
                }
                catch { } // necessary, if we run it after the DB initialization, there is no value
            }
            // Hint: added Dec 2014, db column: column4
            if (res.Rows[0]["userTaskDifficulty"] != null)
            {
                try
                {
                    var val = Convert.ToInt32(res.Rows[0]["userTaskDifficulty"]);
                    data.PreviousSurveyEntryTaskDifficulty = val;
                }
                catch { } // necessary, if we run it after the DB initialization, there is no value
            }
            // Hint: Added at 27.01.15, db column: column1
            if (res.Rows[0]["interruptibility"] != null)
            {
                try
                {
                    var val = Convert.ToInt32(res.Rows[0]["interruptibility"]);
                    data.PreviousSurveyEntryInterruptibility = val;
                }
                catch { } // necessary, if we run it after the DB initialization, there is no value
            }

            foreach (DataRow row in res.Rows)
            {
                if (row["userProductivity"] == null) continue;
                var tasksSplit = Convert.ToString(row["userTasksWorkedOn"]).Split(';');

                foreach (var task in tasksSplit)
                {
                    if (data.PreviousSurveyEntryTasksWorkedOn.Contains(task) || string.IsNullOrWhiteSpace(task)) continue;
                    if (data.PreviousSurveyEntryTasksWorkedOn.Count >= Settings.NumberOfPreviousTasksShown) break;
                    data.PreviousSurveyEntryTasksWorkedOn.Add(task);
                }
            }

            return data;
        }

        /// <summary>
        /// user participated in the survey
        /// </summary>
        /// <param name="dialog"></param>
        /// <returns></returns>
        private static bool HandleUserAnswered(UserSurveyWindow dialog)
        {
            Database.GetInstance().LogInfo("The participant completed the survey.");
            SaveSuccessfulSurveyEntry(dialog.CurrentSurveyEntry);
            return true;
        }

        /// <summary>
        /// save survey entry to the database
        /// </summary>
        /// <param name="entry"></param>
        private static void SaveSuccessfulSurveyEntry(SurveyEntry entry)
        { 
            // save to database
            var query = "INSERT INTO " + Settings.DbTable + " (time, surveyNotifyTime, surveyStartTime, surveyEndTime, userProductivity, userTasksWorkedOn, column2, column3, column4, column1) VALUES (strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'), " +
                Database.GetInstance().QTime(entry.TimeStampNotification) + ", " +
                Database.GetInstance().QTime(entry.TimeStampStarted) + ", " + 
                Database.GetInstance().QTime(entry.TimeStampFinished) + ", " +
                Database.GetInstance().Q(entry.Productivity.ToString()) + ", " +
                Database.GetInstance().Q(entry.TasksWorkedOn) + ", " +
                Database.GetInstance().Q(entry.Satisfaction.ToString()) + ", " + // column2
                Database.GetInstance().Q(entry.Emotions.ToString()) + ", " + // column3
                Database.GetInstance().Q(entry.TaskDifficulty.ToString()) + ", " + // column4
                //Database.GetInstance().Q(entry.SlowNetwork.ToString()) + ", " +
                //Database.GetInstance().Q(entry.Comments) + ", " +
                Database.GetInstance().Q(entry.Interruptibility.ToString()) + ");"; // hint: added 27.01.15, column1

            Database.GetInstance().ExecuteDefaultQuery(query);
        }

        /// <summary>
        /// user didn't fill out anything or closed the window
        /// </summary>
        /// <returns></returns>
        private static bool HandleUserDidntAnswerSurvey()
        {
            Database.GetInstance().LogInfo("The participant did NOT complete the survey.");
            //MessageBox.Show("You didn't fill out the survey :( \n\nWe will ask you again to participate in " + Settings.PostponeShortInterval.TotalMinutes + " minutes. Please contact andre.meyer@uzh.ch in case you have any questions.",
            //    "Please fill out the survey",
            //    MessageBoxButton.OK);
            return false;
        }

        #endregion

        #endregion
    }
}
