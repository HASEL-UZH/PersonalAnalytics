// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using System;
using System.IO;
using System.Resources;
using System.Web.UI;
using PersonalAnalytics.Tracker;
using Shared;
using Shared.Data;

namespace PersonalAnalytics.Visualizations
{
    public class PersonalAnalyticsHttp
    {
        private readonly HttpServer _server;
        private readonly ResourceManager _resourceManager;

        public PersonalAnalyticsHttp()
        {
            _server = new HttpServer();
            _server.AddHandler("stats", OnStats); // load stats website
            _server.AddHandler("d3.min.js", OnResource); // load d3 js framework
            _server.AddHandler("c3.min.js", OnResource); // load c3 js framework
            _server.AddHandler("c3.min.css", OnStylesheets); // load css for c3 js framework
            _server.AddHandler("d3.timeline.js", OnResource); // load d3 timeline js framework
            _server.AddHandler("styles.css", OnStylesheets); // load css
            _server.AddHandler("settings", OnSettings); // load settings
            _server.AddHandler("setsettings", OnSetSettings); // user clicked save in settings

            try
            {
                _resourceManager = new ResourceManager("PersonalAnalytics.Properties.Resources", GetType().Assembly);
            }
            catch { }
        }

        public void Start()
        {
            _server.Start(Settings.Port, false);
        }

        public void Stop()
        {
            _server.Stop();
        }

        private int OnStats(HttpReqResp req)
        {
            try
            {
                var html = string.Empty;
                var date = VisHelper.GetVisualizationDateFromUrlParameters(req);
                //var visType = VisHelper.GetVisualizationTypesFromUrlParameters(req);

                // log request
                Database.GetInstance().LogInfo(string.Format("The participant opened the retrospection/visualization for '{0}'.", date));

                // prepare charts
                var contextTimelineChart = new ContextTimelineChart(date);
                var productivityGauge = new ProductivityGaugeChart(date);
                var activityPie = new ActivityPieChart(date);


                // organize & draw charts
                html += "<style type='text/css'>";
                html += "td { border: 3px solid #B0B0B0; }"; 
                html += "</style>";

                html += "<table cellpadding='20' style='margin-left: auto; margin-right: auto;'>"; // border-color: #F1F1F1;  vertical-align: top; horizontal-align: center;'
                html += "<tr><td style='vertical-align:top;'>" + productivityGauge.GetHtml() + "</td><td style='vertical-align:top;' rowspan='2'>" + contextTimelineChart.GetHtml() + "</td></tr>";
                html += "<tr><td style='vertical-align:top;'>" + activityPie.GetHtml() + "</td></tr>";
                html += "</table>";

                var title = Dict.RetrospectionPageTitle + " for the " + date.Date.ToShortDateString();
                if (RemoteDataHandler.VisualizeWithRemoteData()) title += " (visualizing local and remote data)";
                //title += "<a href=\"settings\">" + Settings.SettingsTitle + "</a>";

                html = ((string) _resourceManager.GetObject("personalanalytics_html"))
                    .Replace("{content}", html)
                    //.Replace("{menu}", Menu)
                    .Replace("{title}", title);
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

        private int OnSettings(HttpReqResp req)
        {
            try
            {
                var screenshotterCheck = (Database.GetInstanceSettings().WindowsContextTrackerEnabled ? "checked=\"checked\" " : "");
                var miniSurveysCheck = (Database.GetInstanceSettings().MiniSurveysEnabled ? "checked=\"checked\"" : "");
                var idleCheck = (Database.GetInstanceSettings().IdleEnabled ? "checked=\"checked\"" : "");
                var miniSurveyInterval = Database.GetInstanceSettings().MiniSurveyInterval;

                var html = "<form method=\"post\" action=\"setsettings\"><table border=\"0\" >" +
                    "<tr><td><label for=\"idle\">Mini-Surveys enabled</label>:</td><td><input type=\"checkbox\" name=\"miniSurveysCheck\" id=\"miniSurveysCheck\" " + miniSurveysCheck + "/></td><td></td></tr>" +
                    "<tr><td><label for=\"history\">Mini-Survey Interval</label>:</td><td><input type=\"text\" size=\"4\" type=\"number\" min=\"1\" maxlength=\"5\" name=\"miniSurveyInterval\" id=\"miniSurveyInterval\" value=\"" + miniSurveyInterval + "\" /></td><td>In minutes (e.g. '60', the tracker will ask you once an hour to fill out the mini-survey).</td></tr>" +
                    "<tr><td><label for=\"idle\">Windows Context Tracker enabled</label>:</td><td><input type=\"checkbox\" name=\"screenshotterCheck\" id=\"screenshotterCheck\" " + screenshotterCheck + "/></td><td>Hint: The tracker regularly takes screenshots that will later be automatically analyced using OCR techniques by the researchers.</td></tr>" +
                    "<tr><td><label for=\"idle\">IDLE enabled</label>:</td><td><input type=\"checkbox\" name=\"idleCheck\" id=\"idleCheck\" " + idleCheck + "/></td><td>Hint: Disable it when you regularly leave your PC on for hours without using it.</td></tr>" +
                    "<tr><td colspan='3'>&nbsp;</td></tr>" +
                    "<tr><td><input type=\"submit\" value=\"save\" /></td><td colspan='2' style=\"color: red;\">Hint: please restart the monitoring tool after enabling/disabling one of the trackers (via Task Manager).</td></tr>" +
                    "</table></form>";

                var title = Settings.SettingsTitle;

                html = ((string)_resourceManager.GetObject("personalanalytics_html"))
                    .Replace("{content}", html)
                    //.Replace("{menu}", Menu)
                    .Replace("{title}", title);
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

        private static int OnSetSettings(HttpReqResp reqresp)
        {
            //TrackerManager.GetInstance().EnableDisableUserEfficiencyTracker(reqresp["miniSurveysCheck"] == "on");
            Database.GetInstanceSettings().MiniSurveysEnabled = (reqresp["miniSurveysCheck"] == "on");
            //TrackerManager.GetInstance().EnableDisableWindowsContextTracker(reqresp["screenshotterCheck"] == "on");
            Database.GetInstanceSettings().WindowsContextTrackerEnabled = (reqresp["screenshotterCheck"] == "on");
            
            Database.GetInstanceSettings().IdleEnabled = (reqresp["idleCheck"] == "on");

            int miniSurveyInterval;
            var res = int.TryParse(reqresp["miniSurveyInterval"], out miniSurveyInterval);

            if (res && miniSurveyInterval > 0)
            {
                Database.GetInstanceSettings().MiniSurveyInterval = miniSurveyInterval;
            }
            else
            {
                Database.GetInstanceSettings().MiniSurveyInterval = Settings.MiniSurveyIntervalDefaultValue;
            }

            reqresp.SetHeader("Location", "settings");

            return 301;
        }

        private int OnResource(HttpReqResp req)
        {
            try
            {
                req.Write((byte[])_resourceManager.GetObject(req.Script.Replace('.', '_')));
                req.SetHeader("Content-Type", req.Script.EndsWith(".png") ? "image/png" : "text/javascript");
            }
            catch { }
            return 200;
        }

        private int OnStylesheets(HttpReqResp req)
        {
            try
            {
                req.Write((byte[])_resourceManager.GetObject(req.Script.Replace('.', '_')));
                req.SetHeader("Content-Type", req.Script.EndsWith(".png") ? "image/png" : "text/css");
            }
            catch { }
            return 200;
        }
    }
}
