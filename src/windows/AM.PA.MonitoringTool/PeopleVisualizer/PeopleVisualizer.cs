// Created by André Meyer at MSR
// Created: 2015-12-10
// 
// Licensed under the MIT License.

using PeopleVisualizer.Visualizations;
using Shared;
using Shared.Data;
using System;
using System.Collections.Generic;

namespace PeopleVisualizer
{
    /// <summary>
    /// Visualizes the people a user is in contact with
    /// 
    /// 1. from Emails
    /// 2. from Meetings
    /// 3. from Skype for Business Events
    /// </summary>
    public class PeopleVisualizer : BaseVisualizer
    {
        private bool _defaultIsEnabled = false;

        #region ITracker Stuff

        public PeopleVisualizer()
        {
            Name = "People Visualizer";
        }

        public override bool IsEnabled()
        {
            return PeopleVisualizerEnabled;
        }

        private bool _peopleVisualizerEnabled;
        public bool PeopleVisualizerEnabled
        {
            get
            {
                _peopleVisualizerEnabled = Database.GetInstance().GetSettingsBool("PeopleVisualizerEnabled", _defaultIsEnabled);
                return _peopleVisualizerEnabled;
            }
            set
            {
                var updatedIsEnabled = value;

                // only update if settings changed
                if (updatedIsEnabled == _peopleVisualizerEnabled) return;

                // update settings
                Database.GetInstance().SetSettings("PeopleVisualizerEnabled", value);

                // start/stop tracker if necessary
                if (!updatedIsEnabled && IsRunning)
                {
                    Stop();
                }
                else if (updatedIsEnabled && !IsRunning)
                {
                    Start();
                }

                // log
                Database.GetInstance().LogInfo("The participant updated the setting 'PeopleVisualizerEnabled' to " + updatedIsEnabled);
            }
        }

        public override List<IVisualization> GetVisualizationsDay(DateTimeOffset date)
        {
            var vis = new DayPeopleGrid(date);
            return new List<IVisualization> { vis };
        }

        #endregion
    }
}
