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

        private string ConvertDataToJson(List<UserActivity> activities)
        {
            JObject json = new JObject();
            HashSet<string> users = new HashSet<string>();
            
            //first get all the unique users
            foreach (UserActivity activity in activities)
            {
                users.Add(activity.From);
                users.Add(activity.To);
            }

            //add a key and value for users to json
            json["users"] = new JArray(users.ToList());

            //then for all the users add a new element to the json
            foreach (string user in users)
            {
                List<UserActivity> activity_for_user = activities.Where(a => a.From == user).ToList();

                if(activity_for_user.Count == 0) { continue;}

                JArray user_activity = new JArray();

                foreach (UserActivity activity in activity_for_user)
                {
                    JObject o = new JObject();
                    o["to"] = activity.To;
                    o["time"] = activity.Time.ToString("HH:MM");
                    o["intensity"] = activity.Intensity;

                    user_activity.Add(o);
                }

                json[user] = user_activity;
            }

            return json.ToString(Formatting.None);
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

            string flat_data = ConvertDataToJson(data);
            Logger.WriteToConsole(flat_data);

            //HTML
            html += "<svg id='user_activity' width='819' height='404'></svg>";

            //SCRIPT
            html += "<script>";
            html += "var data = JSON.parse('" + flat_data + "');";

            html += @"var svg = d3.select('#user_activity'),
                    margin = { top: 20, right: 80, bottom: 30, left: 50},
			        width = svg.attr('width') - margin.left - margin.right,
			        height = svg.attr('height') - margin.top - margin.bottom,
			        g = svg.append('g').attr('transform', 'translate(' + margin.left + ',' + margin.top + ')');

                    var offset = 5;

                    var parseTime = d3.utcParse('%H:%M');
                    var midnight = parseTime('00:00');

                    var x = d3.scaleUtc()
                              .domain([midnight, d3.utcDay.offset(midnight, 1)])
				              .range([0, width]),
			        y = d3.scaleLinear().range([height, 0]);

                    var line = d3.line()
                                 .x(function(d) { return x(parseTime(d['time'])); })
			                     .y(function(d) { return y(d['intensity']); });

                    d3.json('data.json').then(function(data) {
                        y.domain([0, 10]);

                        var user_list = data['users'];

                        for (user in user_list)
                        {
                            user_list[user]['color'] = getRandomColor();
                        }

                        //apend the legend
                        var users = svg.selectAll('.users')
                                    .data(user_list)
                                    .enter().append('g')
                                    .attr('class', 'users')
                                    .attr('id', function(d, i) { return user_list[i].name; });

                        //apend the axis
                        g.append('g')
                          .attr('class', 'axis axis--x')
                          .attr('transform', 'translate(0,' + height + ')')
                          .call(d3.axisBottom(x));

                        g.append('g')
                          .attr('class', 'axis axis--y')
                          .call(d3.axisLeft(y))
                          .append('text')
                          .attr('transform', 'rotate(-90)')
                          .attr('y', 6)
                          .attr('dy', '0.71em')
                          .attr('fill', '#000')
                          .text('Intensity');

                        users.append('path')
                             .attr('transform', 'translate(' + margin.left + ',' + margin.top + ')')
                             .attr('fill', 'none')
                             .attr('stroke', function(d){ return d.color; })
			                 .attr('stroke-linejoin', 'round')
                             .attr('stroke-linecap', 'round')
                             .attr('stroke-width', 1.5)
                             .attr('d', function(d){
                                return d.visible ? line(data[d.name]) : null;
                             });

                        users.append('rect')
                          .attr('width', 10)
                          .attr('height', 10)
                          .attr('x', width + (margin.right / 3) - 15)
                          .attr('y', function(d, i){ return 20 + 20 * i - 8; })
			              .attr('fill', function(d){ return d.visible ? '#000000' : '#F1F1F2'; })
			              .attr('class', 'legend-box')
                          .on('click', function(d){
                            d.visible = !d.visible;

                            users.select('path')
                                 .transition()
                                 .attr('d', function(d){
                                    return d.visible ? line(data[d.name]) : null;
                                })
					            
				            users.select('.nodes')
                                    .transition()
                                    .attr('visibility', function(d) { return d.visible ? 'visible' : 'hidden'});
                            })
			              .on('mouseover', function(d) {
                                d3.select(this)
                                    .transition()
                                    .attr('fill', function(d) { return d.color; });
                            })
			               .on('mouseout', function(d){
                                d3.select(this)
                                  .transition()
                                  .attr('fill', function(d){
                                    return d.visible ? d.color : '#F1F1F2';
                                });
                            });

                        users.append('text')
                             .attr('x', width + (margin.right / 3))
                             .attr('y', function(d, i) { return 20 * i + 20; })
			                 .text(function(d){ return d.name});

                        var nodes = users.append('g')
                            .attr('class', 'nodes')
                            .attr('transform', 'translate(' + margin.left + ',' + margin.top + ')');

                        var dots = nodes.selectAll('circle')
                             .data(function(d) { console.log(d); return data[d.name]; })
				             .enter();

                        dots.append('circle')
                            .attr('class', 'dot')
                            .attr('id', function(d) { return d.name; })
				            .attr('r', 3.5)
                            .attr('cx', function(d) { return x(parseTime(d.time)); })
				            .attr('cy', function(d) { return y(d.intensity); })
				            .attr('visibility', 'hidden');

                        dots.append('svg:text')
                             .text(function(d){ return d.to})
				             .attr('x', function(d) { return x(parseTime(d.time)); })
				             .attr('y', function(d) { return y(d.intensity); })
				             .attr('class', 'node-labels')
                             .visibility;
                    });

                    function getRandomColor()
                    {
                        var letters = '0123456789ABCDEF';
                        var color = '#';
                        for (var i = 0; i < 6; i++)
                        {
                            color += letters[Math.floor(Math.random() * 16)];
                        }
                        return color;
            }";
            html += "</script>";

            return html;
        }
    }
}
