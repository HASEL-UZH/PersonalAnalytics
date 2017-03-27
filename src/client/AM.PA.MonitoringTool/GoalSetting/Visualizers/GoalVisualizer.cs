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
            var rules = GoalSettingManager.Instance.GetRules();

            //For the daily visualization we ignore rules that are on weekly or monthly basis
            rules.RemoveAll(r => r.TimeSpan == Rules.RuleTimeSpan.Month || r.TimeSpan == Rules.RuleTimeSpan.Week);

            List<IVisualization> visualizations = new List<IVisualization>();
            foreach (var rule in rules)
            {
                if (rule.IsVisualizationEnabled)
                {
                    visualizations.Add(new DayVisualization(date, rule));
                }
            }
            return visualizations;
        }

        public override List<IVisualization> GetVisualizationsWeek(DateTimeOffset date)
        {
            var rules = GoalSettingManager.Instance.GetRules();

            //For the weekly visualization we only use rules that are on weekly or monthly basis
            rules = rules.Where(r => r.TimeSpan == Rules.RuleTimeSpan.Month || r.TimeSpan == Rules.RuleTimeSpan.Week).ToList();

            List<IVisualization> visualizations = new List<IVisualization>();
            foreach (var rule in rules)
            {
                if (rule.IsVisualizationEnabled)
                {
                    visualizations.Add(new WeekVisualization(date, rule));
                }
            }
            return visualizations;
        }
    }
}