//
//  DayProductivityTimeline.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-06-05.
//
//  Adapted from Windows version created by André Meyer


import Foundation

class DayProductivityTimeline: Visualization{
    
    var Size: String
    var color: String
    var title: String
    var sql: ProductivitySQLController
    var _type: [String] = [VisConstants.Day]
    
    required init() throws {
        Size = "Square"
        color = AppConstants.retrospectiveColor
        title = "Percieved Productivity over the Day"
        sql = try ProductivitySQLController()
    }
    
    func getHtml(_ _date: Date, type: String) -> String {
        
        if(!_type.contains(type)){
            return ""
        }
        
        var html = ""
        
        /////////////////////
        // fetch data sets
        /////////////////////
        let chartQueryResultsLocal = sql.GetUserProductivityTimelineData(date: _date, type: type)//, VisType.Day);
        
        if (chartQueryResultsLocal.count < 3) // 3 is the minimum number of input-data-items
        {
            html += VisHelper.NotEnoughData("It is not possible to give you insights into your productivity as you didn't fill out the pop-up often enough. Try to fill it out at least 3 times per day.")
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
        html += "<div id='" + VisHelper.CreateChartHtmlTitle(title: title) + "' style='height:75%;' align='center'></div>";
        html += "<p style='text-align: center; font-size: 0.66em;'>Hint: Interpolates your perceived productivity, based on your pop-up responses.</p>";
        
        
        /////////////////////
        // JS
        /////////////////////
        let ticks: String = sql.CalculateLineChartAxisTicks(date: _date)
        let timeAxis: String = VisHelper.makeTimeAxis(chartQueryResultsLocal)
        let productivityFormattedData: String = VisHelper.makeFormattedData(chartQueryResultsLocal)
        let colors: String = "'User_Input_Level' : '" + color + "'"
        var data: String = "x: 'timeAxis', columns: [['timeAxis', " + timeAxis
        data += "], ['Productivity', " + productivityFormattedData
        data += " ] ], type: 'line', colors: { " + colors
        data += " }, axis: { 'PerceivedProductivity': 'y' } " // type options: spline, step, line
        let grid: String = "y: { lines: [ { value: 1, text: 'not at all productive' }, { value: 4, text: 'moderately productive' }, { value: 7, text: 'very productive' } ] } "
        var axis: String = "x: { localtime: true, type: 'timeseries', tick: { values: [ " + ticks
        axis += "], format: function(x) { return formatDate(x.getHours()); }}  }, y: { min: 1, max: 7 }" // show: false,
        let tooltip: String = "show: true, format: { title: function(d) { return 'Pop-Up answered: ' + formatTime(d.getHours(),d.getMinutes()); }}"
        var parameters: String = " bindto: '#" + VisHelper.CreateChartHtmlTitle(title: title)
        parameters += "', data: { " + data
        parameters += " }, padding: { left: 15, right: 0, bottom: -10, top: 0}, legend: { show: false }, axis: { " + axis
        parameters += " }, grid: { " + grid
        parameters += " }, tooltip: { "
        parameters += tooltip + " }, point: { show: true }";
        
        
        html += "<script type='text/javascript'>";
        html += "var formatDate = function(hours) { var suffix = 'AM'; if (hours >= 12) { suffix = 'PM'; hours = hours - 12; } if (hours == 0) { hours = 12; } if (hours < 10) return '0' + hours + ' ' + suffix; else return hours + ' ' + suffix; };";
        html += "var formatTime = function(hours, minutes) { var minFormatted = minutes; if (minFormatted < 10) minFormatted = '0' + minFormatted; var suffix = 'AM'; if (hours >= 12) { suffix = 'PM'; hours = hours - 12; } if (hours == 0) { hours = 12; } if (hours < 10) return '0' + hours + ':' + minFormatted + ' ' + suffix; else return hours + ':' + minFormatted + ' ' + suffix; };";
        html += "var " + VisHelper.CreateChartHtmlTitle(title: title)
        html += " = c3.generate({ " + parameters
        html += " });"; // return x.getHours() + ':' + x.getMinutes();
        html += "</script>";
        
        return html;
    }
    
}
