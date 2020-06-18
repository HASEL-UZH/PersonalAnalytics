// Created by Philip Hofmann (philip.hofmann@uzh.ch) from the University of Zurich
// Created: 2020-02-11
// 
// Licensed under the MIT License.

using System;
using System.Data;

namespace FocusSession.Data
{
    public static class Queries
    {
        internal static void CreateFocusTable()
        {
            try
            {
                Shared.Data.Database.GetInstance().ExecuteDefaultQuery("CREATE TABLE IF NOT EXISTS " + Settings.FocusTimerTable + " (id INTEGER PRIMARY KEY, startTime TEXT, endTime TEXT, " + Settings.FocusTimerSessionDuration + " TEXT, type TEXT, emailEnabled TEXT, emailResponseEnabled TEXT, emailsReceived INT, emailsReplied INT, slackEnabled TEXT, slackResponseEnabled TEXT, slackReceived INT, slackReplied INT, flaggerDisplayed INT);");

            }
            catch (System.Exception e)
            {
                Shared.Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Updated Database tables if the version changed
        /// </summary>
        /// <param name="version"></param>
        internal static void UpdateDatabaseTables(int version)
        {
            try
            {
            }
            catch (System.Exception e)
            {
                Shared.Logger.WriteToLogFile(e);
            }
        }

        /// <summary>
        /// Saves the timestamp into the database
        /// </summary>
        /// <param name="date"> Provide the start and endDate</param>

        internal static void SaveTime(System.DateTime startTime, System.DateTime stopTime, System.TimeSpan timeDuration, string type, string emailEnabled, string emailResponseEnabled, int numberOfReceivedEmailMessages, int emailsReplied,  string slackEnabled, string slackResponseEnabled, int numberOfReceivedSlackMessages, int slackReplied, int flaggerDisplayed)
        {
            try
            {
                Shared.Data.Database.GetInstance().ExecuteDefaultQuery("INSERT INTO " + Settings.FocusTimerTable + " (startTime, endTime, " + Settings.FocusTimerSessionDuration + ", type, emailEnabled, emailResponseEnabled, emailsReceived, emailsReplied, slackEnabled, slackResponseEnabled, slackReceived, slackReplied, flaggerDisplayed) VALUES (" + Shared.Data.Database.GetInstance().QTime(startTime) + ", " + Shared.Data.Database.GetInstance().QTime(stopTime) + ", " + Shared.Data.Database.GetInstance().QTime(timeDuration) + ", " + Shared.Data.Database.GetInstance().Q(type) + ", " + Shared.Data.Database.GetInstance().Q(emailEnabled) + ", " + Shared.Data.Database.GetInstance().Q(emailResponseEnabled) + ", " + Shared.Data.Database.GetInstance().Q(numberOfReceivedEmailMessages) + ", " + Shared.Data.Database.GetInstance().Q(emailsReplied) + ", " + Shared.Data.Database.GetInstance().Q(slackEnabled) + ", " + Shared.Data.Database.GetInstance().Q(slackResponseEnabled) + ", " + Shared.Data.Database.GetInstance().Q(numberOfReceivedSlackMessages) + ", " + Shared.Data.Database.GetInstance().Q(slackReplied) + ", " + Shared.Data.Database.GetInstance().Q(flaggerDisplayed) + ");");
            }
            catch (System.Exception e)
            {
                Shared.Logger.WriteToLogFile(e);
            }
        }


        /// <summary>
        /// Retrieves the total amount of focused time from a specific day. Today, beginning of week, beginning of month ...
        /// </summary>
        /// <param name="date"> Provide the day. Returns TimeSpan.Zero if unsuccessful </param>

        internal static TimeSpan GetFocusTimeFromDay(DateTime day)
        {
            try
            {
                DataTable timeDurationDayTable = Shared.Data.Database.GetInstance().ExecuteReadQuery("SELECT " + Settings.FocusTimerSessionDuration + " FROM " + Settings.FocusTimerTable + " WHERE startTime >= '" + Convert.ToDateTime(day.Date).ToString("yyyy-MM-dd HH:mm:ss") + "' ;");

                TimeSpan totalDay = TimeSpan.Zero;

                foreach (DataRow row in timeDurationDayTable.Rows)
                {
                    totalDay = totalDay.Add(TimeSpan.Parse(row.ItemArray[0].ToString()));
                }

                return totalDay;

            }
            catch (System.Exception e)
            {
                Shared.Logger.WriteToLogFile(e);
                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Retrieves the total amount of emails received stored in the database
        /// </summary>

        internal static int GetEmailMessagesReceived()
        {
            try
            {
                DataTable emailMessagesReceivedTable = Shared.Data.Database.GetInstance().ExecuteReadQuery("SELECT SUM(emailsReceived) as MyColumnSum FROM " + Settings.FocusTimerTable + ";");

                int emailMessagesReceived = 0;

                foreach (DataRow row in emailMessagesReceivedTable.Rows)
                {
                    emailMessagesReceived += Int16.Parse(row.ItemArray[0].ToString());
                    Console.WriteLine(emailMessagesReceived);
                }

                return emailMessagesReceived;

            }
            catch (System.Exception e)
            {
                Shared.Logger.WriteToLogFile(e);
                return 0;
            }
        }

        /// <summary>
        /// Retrieves the total amount of email messages replied stored in the database
        /// </summary>

        internal static int GetEmailMessagesReplied()
        {
            try
            {
                DataTable emailMessagesRepliedTable = Shared.Data.Database.GetInstance().ExecuteReadQuery("SELECT SUM(emailsReplied) as MyColumnSum FROM " + Settings.FocusTimerTable + ";");

                int emailMessagesReplied = 0;

                foreach (DataRow row in emailMessagesRepliedTable.Rows)
                {
                    emailMessagesReplied += Int16.Parse(row.ItemArray[0].ToString());
                    Console.WriteLine(emailMessagesReplied);
                }

                return emailMessagesReplied;

            }
            catch (System.Exception e)
            {
                Shared.Logger.WriteToLogFile(e);
                return 0;
            }
        }

        /// <summary>
        /// Retrieves the total amount of emails received stored in the database
        /// </summary>

        internal static int GetSlackMessagesReceived()
        {
            try
            {
                DataTable slackMessagesReceivedTable = Shared.Data.Database.GetInstance().ExecuteReadQuery("SELECT SUM(slackReceived) as MyColumnSum FROM " + Settings.FocusTimerTable + ";");

                int slackMessagesReceived = 0;

                foreach (DataRow row in slackMessagesReceivedTable.Rows)
                {
                    slackMessagesReceived += Int16.Parse(row.ItemArray[0].ToString());
                    Console.WriteLine(slackMessagesReceived);
                }

                return slackMessagesReceived;

            }
            catch (System.Exception e)
            {
                Shared.Logger.WriteToLogFile(e);
                return 0;
            }
        }

        /// <summary>
        /// Retrieves the total amount of slack messages replied stored in the database
        /// </summary>

        internal static int GetSlackMessagesReplied()
        {
            try
            {
                DataTable slackMessagesRepliedTable = Shared.Data.Database.GetInstance().ExecuteReadQuery("SELECT SUM(slackReplied) as MyColumnSum FROM " + Settings.FocusTimerTable + ";");

                int slackMessagesReplied = 0;

                foreach (DataRow row in slackMessagesRepliedTable.Rows)
                {
                    slackMessagesReplied += Int16.Parse(row.ItemArray[0].ToString());
                    Console.WriteLine(slackMessagesReplied);
                }

                return slackMessagesReplied;

            }
            catch (System.Exception e)
            {
                Shared.Logger.WriteToLogFile(e);
                return 0;
            }
        }

        /// <summary>
        /// Retrieves the total amount of flagger displayed stored in the database
        /// </summary>

        internal static int GetFlaggerDisplayed()
        {
            try
            {
                DataTable flaggerDisplayedTable = Shared.Data.Database.GetInstance().ExecuteReadQuery("SELECT SUM(flaggerDisplayed) as MyColumnSum FROM " + Settings.FocusTimerTable + ";");

                int flaggerDisplayed = 0;

                foreach (DataRow row in flaggerDisplayedTable.Rows)
                {
                    flaggerDisplayed += Int16.Parse(row.ItemArray[0].ToString());
                    Console.WriteLine(flaggerDisplayed);
                }

                return flaggerDisplayed;

            }
            catch (System.Exception e)
            {
                Shared.Logger.WriteToLogFile(e);
                return 0;
            }
        }

        /// <summary>
        /// Retrieves the total amount of sessions run stored in the database
        /// </summary>

        internal static int GetSessionsRun()
        {
            try
            {
                DataTable sessionsRunTable = Shared.Data.Database.GetInstance().ExecuteReadQuery("SELECT COUNT(*) as MyColumnSum FROM " + Settings.FocusTimerTable + ";");

                int sessionsRun = 0;

                foreach (DataRow row in sessionsRunTable.Rows)
                {
                    sessionsRun += Int16.Parse(row.ItemArray[0].ToString());
                    Console.WriteLine(sessionsRun);
                }

                return sessionsRun;

            }
            catch (System.Exception e)
            {
                Shared.Logger.WriteToLogFile(e);
                return 0;
            }
        }

        /// <summary>
        /// Retrieves the desired setting value
        /// </summary>
        internal static bool GetReplyMessageEnabled()
        {
            return Shared.Data.Database.GetInstance().GetSettingsBool(Settings.REPLYMESSAGE_ENEABLED_SETTING, true);
        }

        /// <summary>
        /// Retrieves the desired setting value
        /// </summary>
        internal static bool GetWindowFlaggingEnabled()
        {
            return Shared.Data.Database.GetInstance().GetSettingsBool(Settings.WINDOWFLAGGING_ENEABLED_SETTING, true);
        }

        /// <summary>
        /// Retrieves the desired setting value
        /// </summary>
        internal static bool GetCustomizedReplyMessageEnabled()
        {
            return Shared.Data.Database.GetInstance().GetSettingsBool(Settings.CUSTOMIZEDREPLYMESSAGE_ENEABLED_SETTING, true);
        }

        /// <summary>
        /// Retrieves the desired setting value
        /// </summary>
        internal static string GetCustomizedReplyMessage()
        {
            return Shared.Data.Database.GetInstance().GetSettingsString(Settings.CUSTOMIZEDREPLYMESSAGE_TEXT_SETTING, Settings.IsTextMessageByDefault);
        }

        /// <summary>
        /// Retrieves the desired setting value
        /// </summary>
        internal static bool GetCustomizedFlaggingListEnabled()
        {
            return Shared.Data.Database.GetInstance().GetSettingsBool(Settings.CUSTOMIZEDFLAGGINGLIST_ENEABLED_SETTING, true);
        }

        /// <summary>
        /// Retrieves the desired setting value
        /// </summary>
        internal static string GetCustomizedFlaggingList()
        {
            return Shared.Data.Database.GetInstance().GetSettingsString(Settings.CUSTOMIZEDFLAGGINGLIST_TEXT_SETTING, Settings.IsTextListByDefault);
        }

        /// <summary>
        /// Retrieves the desired setting value
        /// </summary>
        internal static int GetCustomizedSessionDuration()
        {
            return Shared.Data.Database.GetInstance().GetSettingsInt(Settings.CUSTOMIZEDTIMERDURATION_INT_SETTING, 0);
        }

        /// <summary>
        /// Log info as FocusInfo as to distringuish from rest of application for simpler analysis
        /// </summary>
        internal static void LogInfo(string text)
        {
            Shared.Data.Database.GetInstance().LogFocusInfo(text);
        }

    }
}
