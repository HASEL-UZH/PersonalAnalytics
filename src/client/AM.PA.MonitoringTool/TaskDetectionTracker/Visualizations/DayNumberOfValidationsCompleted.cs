// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2018-01-26
// 
// Licensed under the MIT License.


using Shared;
using Shared.Data;

namespace TaskDetectionTracker.Visualizations
{
    internal class DayNumberOfValidationsCompleted : BaseVisualization, IVisualization
    {
        public DayNumberOfValidationsCompleted()
        {
            Title = "Number of TaskDetection PopUps Completed";
            IsEnabled = true; //todo: handle by user
            Order = 20; //todo: handle by user
            Size = VisSize.Small;
            Type = VisType.Day;
        }

        public override string GetHtml()
        {
            var numberOfPopUpResponses = Database.GetInstance().GetSettingsInt(Settings.NumberOfValidationsCompleted_Setting, 0);

            return "<p style='text-align: center; margin-top:-0.7em;'><strong style='font-size:2.5em; color:" + Shared.Settings.RetrospectionColorHex + ";'>" + numberOfPopUpResponses + "</strong></p>"
                   + "<p style='text-align: center; margin-top:-0.7em;'>Thank you for supporting this research!</p>";
        }
    }
}
