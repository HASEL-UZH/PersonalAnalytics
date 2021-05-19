using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsActivityTracker.Data;
using Shared;
using Shared.Helpers;
using WindowsActivityTracker.Models;

namespace WindowsActivityTracker.Visualizations
{
    internal class WindowTitleList : BaseVisualization, IVisualization
    {
        private readonly DateTimeOffset _date;

        public WindowTitleList(DateTimeOffset date)
        {
            this._date = date;

            Title = "Window Title List";
            IsEnabled = true; //todo: handle by user
            Order = 20; //todo: handle by user
            Size = VisSize.Small;
            Type = VisType.Day;
        }

        public override string GetHtml()
        {
            var html = string.Empty;

            /////////////////////
            // fetch data sets
            /////////////////////
            var orderedTimelineList = Queries.GetDayTimelineData(_date);

            /////////////////////
            // Create HTML
            /////////////////////

            html += "<tbody>";
            foreach (var i in orderedTimelineList) // for each activity 
            {
                List<WindowProcessItem> windowList = i.WindowProcessList;
                foreach (var j in windowList) // for each window
                {
                    html += "<tr>";
                    html += "<td>" + j.WindowTitle + "</td>";
                    html += "</tr>";

                }

            }
            html += "</tbody>";
            html += "</table>";

            return html;
        }
    }
}

