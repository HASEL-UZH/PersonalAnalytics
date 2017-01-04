// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-03
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Shared;
using Shared.Helpers;
using WindowsActivityTracker.Data;
using System.Globalization;

namespace WindowsActivityTracker.Visualizations
{
    internal class DayFragmentationTimeline : BaseVisualization, IVisualization
    {
        private readonly DateTimeOffset _date;

        public DayFragmentationTimeline(DateTimeOffset date)
        {
            this._date = date;

            Title = "Activities over the Day"; //hint; overwritten below
            IsEnabled = true; //todo: handle by user
            Order = 2; //todo: handle by user
            Size = VisSize.Wide;
            Type = VisType.Day;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            /////////////////////
            // fetch data sets
            /////////////////////
            var orderedTimelineList = Queries.GetDayTimelineData(_date, true);

            /////////////////////
            // data cleaning
            /////////////////////



            /////////////////////
            // HTML
            /////////////////////



            /////////////////////
            // JS
            /////////////////////


            return html;
        }
    }
}
