// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.

using GoalSetting.Data;
using GoalSetting.Goals;
using GoalSetting.Model;
using Shared;
using Shared.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;

namespace GoalSetting
{
    public class DatabaseConnector
    {

        //Database fields
        private const string ID = "id";
        private const string Title = "title";
        private const string Activity = "contextcategory";
        private const string Timespan = "timespan";
        private const string Timepoint = "timepoint";
        private const string Time = "time";
        private const string Action = "action";
        private const string Goal = "goal";
        private const string Target = "target";
        private const string Operator = "operator";
        private const string VisualizationEnabled = "visualizationEnabled";

        //CREATE Queries
        private static readonly string CREATE_GOALS_TABLE = "CREATE TABLE IF NOT EXISTS " + Settings.GoalTableName + " ("
                                                            + ID + " INTEGER PRIMARY KEY, "
                                                            + Title + " TEXT, "
                                                            + Activity + " TEXT, "
                                                            + Timespan + " TEXT, "
                                                            + Timepoint + " TEXT, "
                                                            + Time + " TEXT, "
                                                            + Action + " TEXT, "
                                                            + Goal + " TEXT, "
                                                            + Target + " TEXT, "
                                                            + Operator + " TEXT, "
                                                            + VisualizationEnabled + " TEXT);";


        //SELECT Queries
        private static readonly string GET_GOALS_QUERY = "SELECT * FROM " + Settings.GoalTableName + ";";

        //INSERT Queries
        private static readonly string INSERT_GOALS_QUERY = "INSERT INTO " + Settings.GoalTableName
                                                            + " SELECT null as " + ID + ", "
                                                            + "'{0}' AS " + Title + ", "
                                                            + "'{1}' AS " + Activity + ", "
                                                            + "'{2}' AS " + Timespan + ", "
                                                            + "'{3}' AS " + Timepoint + ", "
                                                            + "'{4}' AS " + Time + ", "
                                                            + "'{5}' AS " + Action + ", "
                                                            + "'{6}' AS " + Goal + ", "
                                                            + "'{7}' AS " + Target + ", "
                                                            + "'{8}' AS " + Operator + ", "
                                                            + "'{9}' AS " + VisualizationEnabled;

        #region INSERT

        internal static bool SaveGoals(ObservableCollection<Goal> goals)
        {
            try
            {
                //First delete all goals and then save the new goals
                Database.GetInstance().ExecuteDefaultQuery("DELETE FROM " + Settings.GoalTableName + ";");

                foreach (Goal goal in goals)
                {
                    string query = string.Empty;

                    if (goal is GoalActivity)
                    {
                        query += String.Format(INSERT_GOALS_QUERY, (goal.Title == null ? "" : goal.Title), (goal as GoalActivity).Activity, (goal as GoalActivity).TimeSpan, "", "", (goal.Action == null ? "" : goal.Action), goal.Rule.Goal, goal.Rule.TargetValue, goal.Rule.Operator, goal.IsVisualizationEnabled);
                    }
                    else if (goal is GoalEmail)
                    {
                        query += String.Format(INSERT_GOALS_QUERY, (goal.Title == null ? "" : goal.Title), "", "", (goal as GoalEmail).TimePoint, (goal as GoalEmail).Time, (goal.Action == null ? "" : goal.Action), goal.Rule.Goal, goal.Rule.TargetValue, goal.Rule.Operator, goal.IsVisualizationEnabled);
                    }

                    Console.WriteLine(query);
                    Database.GetInstance().ExecuteDefaultQuery(query);
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
                return false;
            }
            return true;
        }

        #endregion

        #region CREATE

        internal static void CreateGoalsTableIfNotExists()
        {
            try
            {
                Database.GetInstance().ExecuteDefaultQuery(CREATE_GOALS_TABLE);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        #endregion

        #region SELECT

        internal static ObservableCollection<Goal> GetStoredGoals()
        {
            var rules = new ObservableCollection<Goal>();

            try
            {
                var table = Database.GetInstance().ExecuteReadQuery(GET_GOALS_QUERY);

                foreach (DataRow row in table.Rows)
                {
                    string title = row[1].ToString();

                    ContextCategory? activity = null;
                    if (!string.IsNullOrEmpty(row[2].ToString())) {
                        activity = (ContextCategory)Enum.Parse(typeof(ContextCategory), row[2].ToString());
                    }

                    RuleTimeSpan? timeSpan = null;
                    if (!string.IsNullOrEmpty(row[3].ToString()))
                    {
                        timeSpan = (RuleTimeSpan)Enum.Parse(typeof(RuleTimeSpan), row[3].ToString());
                    }

                    RuleTimePoint? timePoint = null;
                    if (!string.IsNullOrEmpty(row[4].ToString()))
                    {
                        timePoint = (RuleTimePoint)Enum.Parse(typeof(RuleTimePoint), row[4].ToString());
                    }

                    string time = null;
                    if (!string.IsNullOrEmpty(row[5].ToString()))
                    {
                        time = row[5].ToString();
                    }

                    string action = row[6].ToString();
                    RuleGoal goal = (RuleGoal)Enum.Parse(typeof(RuleGoal), row[7].ToString());

                    string target = row[8].ToString();
                    RuleOperator op = (RuleOperator)Enum.Parse(typeof(RuleOperator), row[9].ToString());

                    string visualizationEnabled = row[10].ToString();
                    bool visualization = Boolean.Parse(visualizationEnabled);

                    if (timeSpan.HasValue)
                    {
                        rules.Add(new GoalActivity() { Title = title, Rule = new Model.Rule { Goal = goal, Operator = op, TargetValue = target }, Activity = activity.Value, TimeSpan = timeSpan, IsVisualizationEnabled = visualization });
                    }
                    else
                    {
                        rules.Add(new GoalEmail() { Title = title, Rule = new Model.Rule { Goal = goal, Operator = op, TargetValue = target }, TimePoint = timePoint, Time = time, IsVisualizationEnabled = visualization });
                    }
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
            return rules;
        }

        private static List<ActivityContext> GetActivities(string query, DateTime lastDate)
        {
            var activities = new List<ActivityContext>();

            Logger.WriteToConsole(query);
            var table = Database.GetInstance().ExecuteReadQuery(query);

            try
            {
                if (table != null && table.Rows.Count > 0)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        var dto = new ContextDto { Context = new ContextInfos { ProgramInUse = row["process"].ToString(), WindowTitle = row["window"].ToString() } };
                        activities.Add(new ActivityContext { Activity = ContextMapper.GetContextCategory(dto), Start = DateTime.ParseExact(row["time"].ToString(), Settings.DateFormat, CultureInfo.InvariantCulture) });
                    }
                }
                else
                {
                    table.Dispose();
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
            finally
            {
                table.Dispose();
            }

            activities = DataHelper.SetEndDateOfActivities(activities, lastDate);

            return activities;
        }

        public static List<ActivityContext> GetActivitiesSince(DateTime date)
        {
            var query = "SELECT * FROM " + Settings.ActivityTable + " WHERE Time >= '" + date.ToString(Settings.DateFormat) + "';";
            return GetActivities(query, DateTime.Now);
        }

        internal static List<ActivityContext> GetActivitiesSinceAndBefore(DateTime start, DateTime end)
        {
            var query = "SELECT * FROM " + Settings.ActivityTable + " WHERE Time >= '" + start.ToString(Settings.DateFormat) + "' AND Time <= '" + end.ToString(Settings.DateFormat) + "';";
            return GetActivities(query, end);
        }

        internal static int GetLatestEmailInboxCount()
        {
            if (Database.GetInstance().HasTable("emails"))
            {

                var query = "Select time, inbox from emails order by time desc limit 1;";
                var table = Database.GetInstance().ExecuteReadQuery(query);

                try
                {
                    if (table != null && table.Rows.Count > 0)
                    {
                        return int.Parse(table.Rows[0]["inbox"].ToString());
                    }
                    else
                    {
                        table.Dispose();
                        return -1;
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteToLogFile(e);
                }
                finally
                {
                    if (table != null)
                    {
                        table.Dispose();
                    }
                }
            }
            return -1;
        }

        #endregion
    }
}