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
    class UserActivityVisualization : BaseVisualization, IVisualization
    {
        private DateTimeOffset _date;

        public UserActivityVisualization(DateTimeOffset date)
        {
            Title = "SlackUserActivity";
            this._date = date;
            IsEnabled = true;
            Size = VisSize.Wide;
            Order = 0;
        }

        private string convertDatatoJson(List<UserActivity> activities)
        {
            HashSet<string> users = new HashSet<string>();
            users.UnionWith(activities.Select(a => a.from).ToList());
            users.UnionWith(activities.Select(a => a.to).ToList());

            JObject graph = new JObject();
            JArray nodes = new JArray();
            JArray links = new JArray();

            foreach (string user in users)
            {
                JObject node = new JObject();
                node["id"] = user;
                nodes.Add(node);
            }

            graph["nodes"] = nodes;
            foreach (UserActivity activity in activities)
            {
                JObject link = new JObject();
                link["source"] = activity.from;
                link["target"] = activity.to;
                link["start_time"] = activity.start_time;
                link["end_time"] = activity.end_time;
                link["channel_id"] = activity.channel_id;
                link["words"] = string.Join(" ", activity.words);

                links.Add(link);
            }

            graph["links"] = links;

            return graph.ToString(Formatting.None);
        }

        public override string GetHtml()
        {
            var html = string.Empty;
            var data = DatabaseConnector.GetUserActivitiesForDay(_date.DateTime);

            if (data == null || data.Count == 0)
            {
                html += VisHelper.NotEnoughData();
                return html;
            }

            string flat_data = convertDatatoJson(data);

            //CSS
            html += "<style>";
            html += @"		
            div.tooltip {	
                position: absolute;			
                text-align: left;			
                width: 120px;					
                height: 120px;					
                padding: 2px;				
                font: 12px sans-serif;		
                background: lightsteelblue;	
                border: 0px;		
                border-radius: 8px;			
                pointer-events: none;";
            html += "</style>";

            //HTML
            html += "<svg width='819' height='404'></svg>";
            
            //SCRIPT
            html += "<script>";
            html += "var graph = JSON.parse('" + flat_data + "');";
            html += @"
		            var svg = d3.select('svg'),
		            width = +svg.attr('width'),
		            height = +svg.attr('height');

            var simulation = d3.forceSimulation()
                .force('charge', d3.forceManyBody().strength(-200))
                .force('link', d3.forceLink().id(function(d) { return d.id;
                }).distance(80))
			    .force('x', d3.forceX(width / 2))
                .force('y', d3.forceY(height / 2))
                .on('tick', ticked);

            var link = svg.selectAll('.link'),
                node = svg.selectAll('.node');

            var div = d3.select('body').append('div')
                .attr('class', 'tooltip')
                .style('opacity', 0);
            var body = d3.select('body');
            
                simulation.nodes(graph.nodes);
                simulation.force('link').links(graph.links);

                link = link
                  .data(graph.links)
                  .enter().append('line')
                  .attr('class', 'link')
                  .attr('stroke', 'black')
                  .attr('stroke-width', '1%')
                  .on('mouseover', function(d, i) {
                       d3.select(this).attr('stroke', 'blue');
			            d3.select('#' + d.source.id)
				            .style('fill', 'blue')
			            d3.select('#' + d.target.id)
				            .style('fill', 'red')
                        div.transition()
                           .duration(200)
                           .style('opacity', .9);
                        div.html('from ' + d.source.id + '<br/>' + ' to ' + d.target.id + '<br/>' + ' about: ' + d.words)
                           .style('left', (d3.event.pageX) + 'px')		
                           .style('top', (d3.event.pageY - 28) + 'px');
                  })				
                .on('mouseout', function(d) {
                    d3.select(this).attr('stroke', 'black');
				    d3.select('#' + d.source.id)
				        .style('fill', 'black')
				    d3.select('#' + d.target.id)
				        .style('fill', 'black')
                    div.transition()
                        .duration(500)
                        .style('opacity', 0);
                });
				
			  node = node
                  .data(graph.nodes)
				  .enter().append('circle')
				  .attr('class', 'node')
                  .attr('id', function(d) { return d.id})
				  .attr('r', 8)
				  .style('fill', 'black');
			  
			  
		function ticked() {
            link.attr('x1', function(d) { return d.source.x; })
			      .attr('y1', function(d) { return d.source.y; })
			      .attr('x2', function(d) { return d.target.x; })
			      .attr('y2', function(d) { return d.target.y; });

            node.attr('cx', function(d) { return d.x; })
			      .attr('cy', function(d) { return d.y; });}";

            html += "</script>";


            Logger.WriteToConsole(flat_data);

            return html;
        }
    }
}
