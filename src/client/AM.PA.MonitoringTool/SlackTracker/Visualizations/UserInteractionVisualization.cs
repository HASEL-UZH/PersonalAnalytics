using Newtonsoft.Json.Linq;
using Shared;
using SlackTracker.Data.SlackModel;
using System;
using System.Collections.Generic;
using System.Linq;
using Accord;
using SlackTracker.Data;
using Shared.Helpers;
using Newtonsoft.Json;

namespace SlackTracker.Visualizations
{
    class UserInteractionVisualization : BaseVisualization, IVisualization
    {
        private DateTimeOffset _date;

        public UserInteractionVisualization(DateTimeOffset date)
        {
            Title = "SlackUserInteraction";
            this._date = date;
            IsEnabled = true;
            Size = VisSize.Wide;
            Order = 0;
        }

        private string convertDatatoJson(List<UserInteraction> activities)
        {
            JArray links = new JArray();

            foreach (UserInteraction activity in activities)
            {
                JObject link = new JObject();
                link["channel"] = activity.channel_id;
                link["from"] = activity.from;
                link["to"] = activity.to;
                link["duration"] = activity.duration;
                link["topics"] = string.Join(" ", activity.topics);
                links.Add(link);
            }

            return links.ToString(Formatting.None);
        }

        public override string GetHtml()
        {
            var html = string.Empty;
            var data = DatabaseConnector.GetUserInteractionsForDay(_date.DateTime);

            if (data == null || data.Count == 0)
            {
                html += VisHelper.NotEnoughData();
                return html;
            }

            string flat_data = convertDatatoJson(data);
            Logger.WriteToConsole(flat_data);

            //HTML
            html += "<div id='activity' width='819' height='404'></div>";
            
            //SCRIPT
            html += "<script>";
            html += "var data = JSON.parse('" + flat_data + "');";
            html += @"
	                var columns = ['channel', 'to', 'from', 'duration', 'topics'];
	                var table = d3.select('#activity').append('table').attr('class', 'example1');
	                var thead = table.append('thead');
	                var	tbody = table.append('tbody');
	                
	                
	                // append the header row
	                thead.append('tr')
	                  .selectAll('th')
	                  .data(columns).enter()
	                  .append('th')
		                .text(function (column) { return column; });
	                
	                // create a row for each object in the data
	                var rows = tbody.selectAll('tr')
	                  .data(data)
	                  .enter()
	                  .append('tr');

	                // create a cell in each row for each column
	                var cells = rows.selectAll('td')
	                  .data(function (row) {
		                return columns.map(function (column) {
		                  return {column: column, value: row[column]};
		                });
	                  })
	                  .enter()
	                  .append('td')
		                .text(function (d) { return d.value; });
		                
	                var tfConfig = {
		                alternate_rows: true,
		                grid_layout: {width: '100%'},
		                rows_counter: {
			                text: 'Count: '
		                },
		                btn_reset: {
			                text: 'Clear'
		                },
		                loader: true,
		                no_results_message: true,

		                // columns data types
		                col_types: [
			                'string',
			                'string',
			                'string',
			                'string',
			                'string'
		                ],

		                extensions: [{ name: 'sort' }]
	                };
	                var tf = new TableFilter(document.querySelector('.example1'), tfConfig);
	                tf.init();";
            html += "</script>";

            return html;
        }
    }
}
