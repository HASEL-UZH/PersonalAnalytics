// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.Resources;
using Shared;
using Shared.Data;
using System.Collections.Generic;
using System.Linq;
using Shared.Helpers;
using Retrospection.Feedback;
using System.Diagnostics;
using System.Windows;
using System.Globalization;
using System.Threading.Tasks;

namespace Retrospection
{
    public class PersonalAnalyticsHttp
    {
        private readonly HttpServer _server;
        private readonly ResourceManager _resourceManager;
        private List<ITracker> _trackers = new List<ITracker>();

        #region HTTP Localhost Stuff

        public PersonalAnalyticsHttp()
        {
            _server = new HttpServer();
            _server.AddHandler("stats", OnStats); // load stats website
            _server.AddHandler("jquery.1.11.3.min.js", OnResource); // load jQuery js framework
            _server.AddHandler("d3.min.js", OnResource); // load d3 js framework
            _server.AddHandler("c3.min.js", OnResource); // load c3 js framework
            _server.AddHandler("c3.min.css", OnStylesheets); // load css for c3 js framework
            _server.AddHandler("d3.timeline.js", OnResource); // load d3 timeline js framework
            _server.AddHandler("masonry.pkgd.min.js", OnResource); // load masonry grid layout js framework
            _server.AddHandler("tablefilter.js", OnResource); // load css
            _server.AddHandler("tablefilter.css", OnStylesheets); // load css
            _server.AddHandler("thumbUpBlue.png", OnResource); // load image
            _server.AddHandler("thumbDownGray.png", OnResource); // load image
            _server.AddHandler("thumbUpGray.png", OnResource); // load image
            _server.AddHandler("thumbDownBlue.png", OnResource); // load image
            _server.AddHandler("styles.css", OnStylesheets); // load css

            try
            {
                _resourceManager = new ResourceManager("Retrospection.Properties.Resources", GetType().Assembly);
            }
            catch { }
        }

        public void Start()
        {
            if (_server != null)
            {
                _server.Start(Settings.Port, true); // true -> IPAddress.Loopback (loopback address (localhost / 127.0.0.1)) 
            }
        }

        public void Stop()
        {
            if (_server != null)
            {
                _server.Stop();
            }
        }

        private int OnStats(HttpReqResp req)
        {
            try
            {
                var date = GetVisualizationDateFromUrlParameters(req);
                date = VerifyDateForRetrospection(date);
                var visType = GetVisualizationTypesFromUrlParameters(req);

                // get data to display
                var title = GetRetrospectionTitle(visType, date);
                var dashboard = ((string)_resourceManager.GetObject("personalanalytics_html"));
                var visualizations = GetVisualizationsHtml(visType, date);
                visualizations.Wait(); // wait for the async task to complete

                // prepare html which is displayed in the browser control
                var html = dashboard.Replace("{title}", title).Replace("{visualizations}", visualizations.Result);
                req.Write(html);

                req.SetHeader("Content-Type", "text/html; charset=utf-8");
            }
            catch (Exception e)
            {
                req.Write(e.ToString());
                req.SetHeader("Content-Type", "text/html; charset=utf-8");
            }
            return 200;
        }



        private int OnResource(HttpReqResp req)
        {
            try
            {
                req.Write((byte[])_resourceManager.GetObject(req.Script.Replace('.', '_')));
                req.SetHeader("Content-Type", req.Script.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "text/javascript");
            }
            catch { }
            return 200;
        }

        private int OnStylesheets(HttpReqResp req)
        {
            try
            {
                req.Write((byte[])_resourceManager.GetObject(req.Script.Replace('.', '_')));
                req.SetHeader("Content-Type", req.Script.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "text/css");
            }
            catch { }
            return 200;
        }

        #endregion

        #region Populate Dashboard

        /// <summary>
        /// link to the current tracker list is needed to fetch the
        /// visualization of every tracker
        /// </summary>
        /// <param name="trackers"></param>
        public void SetTrackers(List<ITracker> trackers)
        {
            _trackers = trackers;
        }

        /// <summary>
        /// Creates a nice title depending on the setting
        /// </summary>
        /// <param name="visType"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        private string GetRetrospectionTitle(VisType visType, DateTimeOffset date)
        {
            switch (visType)
            {
                case VisType.Day:
                    return "Your Retrospection for the " + date.Date.ToShortDateString();
                case VisType.Week:
                    return string.Format(CultureInfo.InvariantCulture, "Your Retrospection for Week {0} ({1} - {2})",
                        DateTimeHelper.GetWeekOfYear_Iso8601(date.Date),
                        DateTimeHelper.GetFirstDayOfWeek_Iso8801(date.Date).Date.ToShortDateString(),
                        DateTimeHelper.GetLastDayOfWeek_Iso8801(date.Date).Date.ToShortDateString());
            }

            return VisHelper.Error("Retrospection not supported!");
        }

        /// <summary>
        /// Gets the week OR day visualizations from each tracker and prepares
        /// the HTML for showing on the website
        /// </summary>
        /// <param name="type"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        internal async Task<string> GetVisualizationsHtml(VisType type, DateTimeOffset date)
        {
            // get updated visualizations (if enabled)
            var visualizations = new List<IVisualization>();
            foreach (var tracker in _trackers.Where(t => t.IsEnabled() == true && t.IsRunning))
            {
                switch (type)
                {
                    case VisType.Day:
                        visualizations.AddRange(tracker.GetVisualizationsDay(date).Where(i => i.IsEnabled));
                        break;
                    case VisType.Week:
                        visualizations.AddRange(tracker.GetVisualizationsWeek(date).Where(i => i.IsEnabled));
                        break;
                }
            }

            // add the feedback features (if enabled)
            var feedback = new FeedbackBox(type);
            if (feedback.IsEnabled == true) visualizations.Add(feedback);

            // add visualizations in right order
            var html = string.Empty;
            foreach (var vis in visualizations.OrderBy(v => v.Order))
            {
                //We want to avoid that a failing visualization stops the whole dashboard from working. Therefore we exclude failing visualizations and log the error.
                try
                {
                    html += await Task.Run(() => CreateDashboardItem(vis, date));
                }
                catch (Exception e)
                {
                    Logger.WriteToLogFile(e);
                }
            }

            return html;
        }

        private string CreateDashboardItem(IVisualization vis, DateTimeOffset date)
        {
            try
            {
                var feedbackButtons = FeedbackThumbs.GetInstance().GetFeedbackThumbsUpDown(vis, date);
                var chartTitle = VisHelper.FormatChartTitle(vis.Title);
                var html = vis.GetHtml();

                var itemTemplate = "<div class='item {3}'>{0}{1}{2}</div>";
                return string.Format(CultureInfo.InvariantCulture, itemTemplate, feedbackButtons, chartTitle, html, vis.Size);
            }
            catch(Exception e)
            {
                Logger.WriteToLogFile(e);
                return VisHelper.Error(string.Format(CultureInfo.InvariantCulture, "<div id='item {2}'>An error occurred when creating the visualization: '{0}'.</div>", vis.Title, vis.Size));
            }
        }

        /// <summary>
        /// Parses the request parameter (for "date") as a DateTime
        /// or returns a fallback value if there is no or if it fails.
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static DateTimeOffset GetVisualizationDateFromUrlParameters(HttpReqResp req)
        {
            var fallbackDate = DateTimeOffset.Now;
            try
            {
                var dateParam = req["date"];
                if (dateParam != null)
                {
                    return DateTimeOffset.Parse(dateParam, CultureInfo.InvariantCulture);
                }
            }
            catch { }

            return fallbackDate;
        }

        /// <summary>
        /// If the date is in the future, prompt an error message and use the current date.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTimeOffset VerifyDateForRetrospection(DateTimeOffset date)
        {
            return (date.Date <= DateTime.Now.Date) ? date : DateTimeOffset.Now.Date;
        }

        /// <summary>
        /// tries to parse the url
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static VisType GetVisualizationTypesFromUrlParameters(HttpReqResp req)
        {
            try
            {
                if (req["type"] == null) return VisType.Day; // default
                return (VisType)Enum.Parse(typeof(VisType), req["type"], true);
            }
            catch
            {
                return VisType.Day; // default
            }
        }

        #endregion
    }
}
