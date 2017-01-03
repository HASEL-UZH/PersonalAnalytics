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
    internal class DayActivityTimeLine : BaseVisualization, IVisualization
    {
        private readonly DateTimeOffset _date;
        //private const int _maxNumberOfPrograms = 10;

        public DayActivityTimeLine(DateTimeOffset date)
        {
            this._date = date;

            Title = "Activities over the Day"; //hint; overwritten below
            IsEnabled = true; //todo: handle by user
            Order = 1; //todo: handle by user
            Size = VisSize.Square;
            Type = VisType.Day;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            /////////////////////
            // fetch data sets
            /////////////////////
            

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
