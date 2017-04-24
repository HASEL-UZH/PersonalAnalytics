// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-27
// 
// Licensed under the MIT License.

using Shared;
using Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using GoalSetting.Model;
using GoalSetting.Visualizers.Day;
using GoalSetting.Visualizers.Week;
using GoalSetting.Visualizers.Summary;

namespace GoalSetting.Visualizers
{
    public class GoalVisualizer : BaseVisualizer
    {
        private bool _isEnabled = true;

        public GoalVisualizer()
        {
            Name = "Goal Visualizer";
        }

        public override bool IsEnabled()
        {
            return _isEnabled;
        }

        public override string GetVersion()
        {
            var v = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;
            return VersionHelper.GetFormattedVersion(v);
        }

        public override List<IVisualization> GetVisualizationsDay(DateTimeOffset date)
        {
            var goals = GoalSettingManager.Instance.GetActivityGoals();

            //For the daily visualization we ignore rules that are on weekly or monthly basis
            goals.RemoveAll(r => r.TimeSpan == RuleTimeSpan.Month || r.TimeSpan == RuleTimeSpan.Week);
            
            //We use the following strategy to display visualizations for goals:
            //Specific point in time --> Progress bar
            //Hour --> bar chart
            //Day --> line chart
            //Specific day --> line chart (but ignore if it is not the specific day)
            //Week --> Ignore
            //Month --> Ignore
            //Afternoon --> Ignore if visualization is displayed in the morning, otherweise line chart
            //Morning --> line chart
            List<IVisualization> visualizations = new List<IVisualization>();
            foreach (var goal in goals)
            {
                if (goal.IsVisualizationEnabled)
                {
                    if (goal.TimeSpan == RuleTimeSpan.Hour)
                    {
                        visualizations.Add(new DayVisualizationForHourlyGoals(date, goal));
                    }
                    else if (goal.TimeSpan == RuleTimeSpan.EveryDay)
                    {
                        visualizations.Add(new DayVisualizationForDailyGoals(date, goal));
                    }
                    else if ( (goal.TimeSpan == RuleTimeSpan.Monday && date.DayOfWeek == DayOfWeek.Monday) ||
                              (goal.TimeSpan == RuleTimeSpan.Tuesday && date.DayOfWeek == DayOfWeek.Tuesday) ||
                              (goal.TimeSpan == RuleTimeSpan.Wednesday && date.DayOfWeek == DayOfWeek.Wednesday) ||
                              (goal.TimeSpan == RuleTimeSpan.Thursday && date.DayOfWeek == DayOfWeek.Thursday) ||
                              (goal.TimeSpan == RuleTimeSpan.Friday && date.DayOfWeek == DayOfWeek.Friday) ||
                              (goal.TimeSpan == RuleTimeSpan.Saturday && date.DayOfWeek == DayOfWeek.Saturday) ||
                              (goal.TimeSpan == RuleTimeSpan.Sunday && date.DayOfWeek == DayOfWeek.Sunday))
                    {
                        visualizations.Add(new DayVisualizationForDailyGoals(date, goal));
                    }
                    else if (goal.TimeSpan == RuleTimeSpan.Morning)
                    {
                        visualizations.Add(new DayVisualizationForDailyGoals(date, goal));
                    }
                    else if (goal.TimeSpan == RuleTimeSpan.Afternoon && date.DateTime.Hour > 11)
                    {
                        visualizations.Add(new DayVisualizationForDailyGoals(date, goal));
                    }
                }
            }

            //Add summary visualization if it's today
            if (date.Date == DateTime.Today)
            {
                visualizations.Add(new GoalSummaryVisualization(date));
            }
            return visualizations;
        }

        public override List<IVisualization> GetVisualizationsWeek(DateTimeOffset date)
        {
            var goals = GoalSettingManager.Instance.GetActivityGoals();

            List<IVisualization> visualizations = new List<IVisualization>();
            foreach (var goal in goals)
            {
                if (goal.IsVisualizationEnabled)
                {
                    if (goal.TimeSpan == RuleTimeSpan.Hour || goal.TimeSpan == RuleTimeSpan.Afternoon || goal.TimeSpan == RuleTimeSpan.Morning)
                    {
                        visualizations.Add(new WeekVisualizationForHourlyGoal(date, goal));
                    }

                    if (goal.TimeSpan == RuleTimeSpan.Week || goal.TimeSpan == RuleTimeSpan.Month)
                    {
                        visualizations.Add(new WeekVisualizationForWeeklyGoal(date, goal));
                    }

                    if (goal.TimeSpan == RuleTimeSpan.EveryDay)
                    {
                        visualizations.Add(new WeekVisualizationForDailyGoal(date, goal));
                    }

                    if (goal.TimeSpan == RuleTimeSpan.Monday || goal.TimeSpan == RuleTimeSpan.Tuesday || goal.TimeSpan == RuleTimeSpan.Wednesday || goal.TimeSpan == RuleTimeSpan.Thursday ||
                        goal.TimeSpan == RuleTimeSpan.Friday || goal.TimeSpan == RuleTimeSpan.Saturday || goal.TimeSpan == RuleTimeSpan.Sunday)
                    {
                        visualizations.Add(new WeekVisualizationForSpecificDailyGoal(date, goal));
                    }
                }
            }

            //Add summary visualization
            if (date.Date == DateTime.Today)
            {
                visualizations.Add(new GoalSummaryVisualization(date));
            }
            return visualizations;
        }
    }
}