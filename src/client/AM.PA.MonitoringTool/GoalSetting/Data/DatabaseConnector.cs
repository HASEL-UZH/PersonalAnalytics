// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-02-24
// 
// Licensed under the MIT License.

using GoalSetting.Data;
using GoalSetting.Model;
using GoalSetting.Rules;
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
        private const string Action = "action";
        private const string Goal = "goal";
        private const string Target = "target";
        private const string Operator = "operator";
        
        //CREATE Queries
        private static readonly string CREATE_RULES_TABLE = "CREATE TABLE IF NOT EXISTS " + Settings.RuleTableName + " ("
                                                            + ID + " INTEGER PRIMARY KEY, "
                                                            + Title + " TEXT, "
                                                            + Activity + " TEXT, "
                                                            + Timespan + " TEXT, "
                                                            + Action + " TEXT, "
                                                            + Goal + " TEXT, "
                                                            + Target + " TEXT, "
                                                            + Operator + " TEXT);";


        //SELECT Queries
        private static readonly string GET_RULES_QUERY = "SELECT * FROM " + Settings.RuleTableName + ";";

        //INSERT Queries
        private static readonly string INSERT_RULES_QUERY = "INSERT INTO " + Settings.RuleTableName
                                                            + " SELECT null as " + ID + ", "
                                                            + "'{0}' AS " + Title + ", "
                                                            + "'{1}' AS " + Activity + ", "
                                                            + "'{2}' AS " + Timespan + ", "
                                                            + "'{4}' AS " + Action + ", "
                                                            + "'{5}' AS " + Goal + ", "
                                                            + "'{6}' AS " + Target + ", "
                                                            + "'{7}' AS " + Operator;

        #region INSERT

        internal static void SaveRules(ObservableCollection<PARule> rules)
        {
            //First delete all rules and then save the new rules
            Database.GetInstance().ExecuteDefaultQuery("DELETE FROM " + Settings.RuleTableName + ";");

            foreach (PARule rule in rules)
            {
                string query = string.Empty;
                query += String.Format(INSERT_RULES_QUERY, rule.Title, rule.Activity, rule.TimeSpan, rule.Action, rule.Rule.Goal, rule.Rule.TargetValue, rule.Rule.Operator);
                Console.WriteLine(query);
                Database.GetInstance().ExecuteDefaultQuery(query);
            }
        }

        #endregion

        #region CREATE

        internal static void CreateRulesTableIfNotExists()
        {
            try
            {
                Database.GetInstance().ExecuteDefaultQuery(CREATE_RULES_TABLE);
            }
            catch (Exception e)
            {
                Logger.WriteToLogFile(e);
            }
        }

        #endregion

        #region SELECT

        internal static ObservableCollection<PARule> GetStoredRules()
        {
            var rules = new ObservableCollection<PARule>();

            var table = Database.GetInstance().ExecuteReadQuery(GET_RULES_QUERY);

            foreach (DataRow row in table.Rows)
            {
                string title = row[1].ToString();
                ContextCategory activity = (ContextCategory) Enum.Parse(typeof(ContextCategory), row[2].ToString());
                RuleTimeSpan timeSpan = (RuleTimeSpan) Enum.Parse(typeof(RuleTimeSpan), row[3].ToString());
                string action = row[4].ToString();
                Goal goal = (Goal)Enum.Parse(typeof(Goal), row[5].ToString());
                string target = row[6].ToString();
                Operator op = (Operator)Enum.Parse(typeof(Operator), row[7].ToString());

                rules.Add(new PARule() { Title = title, Rule = new Rules.Rule { Goal = goal, Operator = op, TargetValue = target }, Activity = activity, TimeSpan = timeSpan });
            }

            return rules;
        }

        private static List<ActivityContext> GetActivities(string query)
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

            activities = DataHelper.SetEndDateOfActivities(activities);

            return activities;
        }

        public static List<ActivityContext> GetActivitiesSince(DateTime date)
        {
            var query = "SELECT * FROM " + Settings.ActivityTable + " WHERE Time > '" + date.ToString(Settings.DateFormat) + "';";
            return GetActivities(query);
        }

        internal static List<ActivityContext> GetActivitiesSinceAndBefore(DateTime start, DateTime end)
        {
            var query = "SELECT * FROM " + Settings.ActivityTable + " WHERE Time > '" + start.ToString(Settings.DateFormat) + "' AND Time < '" + end.ToString(Settings.DateFormat) + "';";
            return GetActivities(query);
        }

        #endregion

    }
}