// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-27
// 
// Licensed under the MIT License.

using Shared;
using Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GoalSetting.Model;

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

            List<IVisualization> visualizations = new List<IVisualization>();
            foreach (var goal in goals)
            {
                if (goal.IsVisualizationEnabled)
                {
                    if (goal.TimeSpan == RuleTimeSpan.Hour)
                    {
                        visualizations.Add(new DayVisualizationForHourlyGoals(date, goal));
                    }
                    else
                    {
                        visualizations.Add(new DayVisualizationForDailyGoals(date, goal));
                    }
                }
            }
            return visualizations;
        }

        public override List<IVisualization> GetVisualizationsWeek(DateTimeOffset date)
        {
            var goals = GoalSettingManager.Instance.GetActivityGoals();

            //For the weekly visualization we only use rules that are on weekly or monthly basis
            goals = goals.Where(r => r.TimeSpan == RuleTimeSpan.Month || r.TimeSpan == RuleTimeSpan.Week).ToList();

            List<IVisualization> visualizations = new List<IVisualization>();
            foreach (var goal in goals)
            {
                if (goal.IsVisualizationEnabled)
                {
                    visualizations.Add(new WeekVisualization(date, goal));
                }
            }
            return visualizations;
        }
    }
}