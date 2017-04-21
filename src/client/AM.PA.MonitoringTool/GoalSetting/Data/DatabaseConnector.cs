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
        private const string IsActive = "isActive";
        private const string Created = "created";
        private const string Deleted = "deleted";
        private const string TargetValue = "targetValue";
        private const string ActualValue = "actualValue";
        private const string Success = "success";
        private const string TimeChecked = "timeChecked";
        private const string TimeSaved = "timeSaved";
        private const string GoalID = "goalID";

        //CREATE Goals
        private static readonly string CREATE_GOALS_TABLE = "CREATE TABLE IF NOT EXISTS " + Settings.GoalTableName + " ("
                                                            + ID + " TEXT PRIMARY KEY, "
                                                            + Title + " TEXT, "
                                                            + Activity + " TEXT, "
                                                            + Timespan + " TEXT, "
                                                            + Timepoint + " TEXT, "
                                                            + Time + " TEXT, "
                                                            + Action + " TEXT, "
                                                            + Goal + " TEXT, "
                                                            + Target + " TEXT, "
                                                            + Operator + " TEXT, "
                                                            + VisualizationEnabled + " TEXT, "
                                                            + IsActive + " TEXT, "
                                                            + Created + " DATETIME, "
                                                            + Deleted + " DATETIME);";
        
        //CREATE Achievements
        private static readonly string CREATE_ACHIEVEMENTS_TABLE = "CREATE TABLE IF NOT EXISTS " + Settings.AchievementsTableName + " ("
                                                            + ID + " TEXT PRIMARY KEY, "
                                                            + TimeChecked + " DATETIME, "
                                                            + TimeSaved +  " DATETIME, "
                                                            + GoalID + " TEXT, "
                                                            + TargetValue + " TEXT ,"
                                                            + ActualValue + " TEXT ,"
                                                            + Success + " TEXT);";

        //SELECT Queries
        private static readonly string GET_GOALS_QUERY = "SELECT " + ID + ", " + Title + ", " + Activity + ", " + Timespan + ", " + Timepoint + ", " + Time + ", " + Action + ", " + Goal + ", " + Target + ", " + Operator + ", " + VisualizationEnabled + " FROM " + Settings.GoalTableName + " WHERE isActive == 'True';";

        //INSERT Queries
        private static readonly string INSERT_GOALS_QUERY = "INSERT INTO " + Settings.GoalTableName + " VALUES ("
                                                            + "'{0}', "
                                                            + "'{1}', "
                                                            + "'{2}', "
                                                            + "'{3}', "
                                                            + "'{4}', "
                                                            + "'{5}', "
                                                            + "'{6}', "
                                                            + "'{7}', "
                                                            + "'{8}', "
                                                            + "'{9}', "
                                                            + "'{10}', "
                                                            + "'{11}', "
                                                            + "{12}, "
                                                            + "{13});";

        //REMOVE Queries
        private static readonly string REMOVE_GOAL_QUERY = "UPDATE " + Settings.GoalTableName + " SET " + Deleted + " = '{0}', " + IsActive + " = '" + false + "' WHERE " + ID + " == '{1}';";
        
        #region Add

        internal static void AddGoal(Goal goal)
        {
            try
            {
                string query = string.Empty;

                goal.ID = Guid.NewGuid();

                if (goal is GoalActivity)
                {
                    query += String.Format(INSERT_GOALS_QUERY, goal.ID, (goal.Title == null ? "" : goal.Title), (goal as GoalActivity).Activity, (goal as GoalActivity).TimeSpan, "", "", (goal.Action == null ? "" : goal.Action), goal.Rule.Goal, goal.Rule.TargetValue, goal.Rule.Operator, goal.IsVisualizationEnabled, true, "'" + DateTime.Now.ToString(Settings.DateFormat) + "'", "null");
                }
                else if (goal is GoalEmail)
                {
                    query += String.Format(INSERT_GOALS_QUERY, goal.ID, (goal.Title == null ? "" : goal.Title), "", "", (goal as GoalEmail).TimePoint, (goal as GoalEmail).Time, (goal.Action == null ? "" : goal.Action), goal.Rule.Goal, goal.Rule.TargetValue, goal.Rule.Operator, goal.IsVisualizationEnabled, true, "'" + DateTime.Now.ToString(Settings.DateFormat) + "'", "null");
                }

                Console.WriteLine(query);
                Database.GetInstance().ExecuteDefaultQuery(query);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        #endregion

        #region Remove

        internal static void RemoveGoal(Goal goal)
        {
            try
            {
                var query = String.Format(REMOVE_GOAL_QUERY, DateTime.Now.ToString(Settings.DateFormat), goal.ID);
                Console.WriteLine(query);
                Database.GetInstance().ExecuteDefaultQuery(query);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        #endregion
        
        #region CREATE

        internal static void CreateGoalsTableIfNotExists()
        {
            try
            {
                Database.GetInstance().ExecuteDefaultQuery(CREATE_GOALS_TABLE);
                Database.GetInstance().ExecuteDefaultQuery(CREATE_ACHIEVEMENTS_TABLE);
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
                    Guid id = Guid.Parse(row[0].ToString());

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
                        rules.Add(new GoalActivity() { ID = id, Title = title, Rule = new Model.Rule { Goal = goal, Operator = op, TargetValue = target }, Activity = activity.Value, TimeSpan = timeSpan, IsVisualizationEnabled = visualization });
                    }
                    else
                    {
                        rules.Add(new GoalEmail() { ID = id, Title = title, Rule = new Model.Rule { Goal = goal, Operator = op, TargetValue = target }, TimePoint = timePoint, Time = time, IsVisualizationEnabled = visualization });
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