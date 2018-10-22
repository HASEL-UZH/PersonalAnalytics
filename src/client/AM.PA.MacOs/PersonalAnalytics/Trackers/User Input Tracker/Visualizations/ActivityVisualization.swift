//
//  ActivityVisualization.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-24.
//
//  Adapted from Windows version created by André Meyer


import Foundation


class ActivityVisualization: Visualization{
  
    let Size: String

    let color = AppConstants.retrospectiveColor
    //TODO
    var title: String
    var _type = [VisConstants.Day]
    
    let sql: InputSQLController
    
    required init() throws {
        sql = try InputSQLController()
        Size = "Square"
        title = "Active Times"
    }
    
    func getHtml(_ _date: Date, type: String) -> String {
        
        if(!_type.contains(type)){
            return ""
        }
        
        var html = ""
        
        
        /////////////////////
        // fetch data sets
        /////////////////////
        //TODO: add date information
        //returns a dictionary
        var chartQueryResultsLocal: Dictionary<TimeInterval,Int> = sql.GetUserInputTimelineData(date: _date)

        // 3 is the minimum number of input-data-items - else, it makes no sense to show a visualization
        if (chartQueryResultsLocal.count < 3)
        {
            html += VisHelper.NotEnoughData();
            return html;
        }
        
        /////////////////////
        // CSS
        /////////////////////
        html += "<style type='text/css'>";
        html += ".c3-line { stroke-width: 2px; }";
        html += ".c3-grid text, c3.grid line { fill: gray; }";
        html += "</style>";


        /////////////////////
        // HTML
        /////////////////////
        html += "<div id='" + VisHelper.CreateChartHtmlTitle(title: title) + "' style='height:75%;' align='center'></div>"
        html += "<p style='text-align: center; font-size: 0.66em;'>Hint: Visualizes your active times, based on your keyboard and mouse input.</p>"


        /////////////////////
        // JS
        /////////////////////
        let ticks = sql.CalculateLineChartAxisTicks(date: _date)
        
        //aggregate the results of the dictionary to make axis
        let timeAxis = VisHelper.makeTimeAxis(chartQueryResultsLocal)//chartQueryResultsLocal.Aggregate("", (current, a) => current + (DateTimeHelper.JavascriptTimestampFromDateTime(a.Key) + ", ")).Trim().TrimEnd(",")
        let userInputFormattedData = VisHelper.makeFormattedData(chartQueryResultsLocal)//chartQueryResultsLocal.Aggregate("", (current, p) => current + (p.Value + ", ")).Trim().TrimEnd(",")
        
        let maxUserInput = VisHelper.getMax(chartQueryResultsLocal)
        let avgUserInput = VisHelper.getAvg(chartQueryResultsLocal)

        let colors: String = "'User_Input_Level' : '" + color + "'"
        let data: String = "x: 'timeAxis', columns: [['timeAxis', " + timeAxis + "], ['User_Input_Level', " + userInputFormattedData + " ] ], type: 'area', colors: { " + colors + " }, axis: { 'PerceivedProductivity': 'y' }"
        let grid: String = "y: { lines: [ { value: 0, text: 'not active' }, { value: " + String(avgUserInput) + ", text: 'average activity today' }, { value: " + String(maxUserInput) + ", text: 'max activity today' } ] } "
        let axis: String = "x: { localtime: true, type: 'timeseries', tick: { values: [ " + ticks + "], format: function(x) { return formatDate(x.getHours()); }}  }, y: { show: false, min: 0 }"
        let parameters: String = " bindto: '#" + VisHelper.CreateChartHtmlTitle(title: title) + "', data: { " + data + " }, padding: { left: 0, right: 0, bottom: -10, top: 0}, legend: { show: false }, axis: { " + axis + " }, grid: { " + grid + " }, tooltip: { show: false }, point: { show: false }"
        // padding: { left: 0, right: 0 },

        html += "<script type='text/javascript'>";
        html += "var formatDate = function(hours) { var suffix = 'AM'; if (hours >= 12) { suffix = 'PM'; hours = hours - 12; } if (hours == 0) { hours = 12; } if (hours < 10) return '0' + hours + ' ' + suffix; else return hours + ' ' + suffix; };";
        html += "var " + VisHelper.CreateChartHtmlTitle(title: title) + " = c3.generate({ " + parameters + " });"; // return x.getHours() + ':' + x.getMinutes();
        html += "</script>";

        return html;
    }

    
    //
        
}
