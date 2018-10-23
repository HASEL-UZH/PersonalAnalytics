//
//  DayProgramsUsedPieChart.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-29.
//
//  Adapted from Windows version created by André Meyer


import Foundation

class DayProgamsUsedPieChart: Visualization {
    
    let Size:String
    let color = AppConstants.retrospectiveColor
    var title: String
    let sql: ActivitySQLController
    var _type: [String] = [VisConstants.Day]
    let _minTimeWorked = 0.0
    
    required init() throws{
        Size = "Square"
        sql = try ActivitySQLController()
        title = "Top Programs Used"
    }
    
    func getHoursWorked(dict: Dictionary<String, Double> ) -> Double{
        var sum: Double = 0
        for (_, value) in dict{
            sum += value
        }
        return sum
    }
    
    func getHtml(_ _date: Date, type: String) -> String {
        
        if(!_type.contains(type)){
            print("return")
            return ""
        }
        var html = ""
        /////////////////////
        // fetch data sets
        /////////////////////
        //var chartQueryPieChartData = new Dictionary<string, long>();
        var chartQueryResultsLocal: [String:Double]
        chartQueryResultsLocal = sql.GetActivityPieChartData(date: _date)
        
        
        // merge with remote data if necessary //TODO: REMOTE DATA
        //chartQueryPieChartData = RemoteDataHandler.VisualizeWithRemoteData()
        //    ? RemoteDataHandler.MergeActivityData(chartQueryResultsLocal, Queries.GetActivityPieChartData(_date))
        //    : chartQueryResultsLocal;
        
        /////////////////////
        // data cleaning
        /////////////////////
        //// remove IDLE (doesn't belong to activity on computer)
        //if (chartQueryResultsLocal.ContainsKey(Dict.Idle))
        //    chartQueryResultsLocal.Remove(Dict.Idle);
        
        // calculate total active time
        let totalHoursWorked = getHoursWorked(dict: chartQueryResultsLocal)
        print("total hours:",totalHoursWorked)
        // check if we have enough data
        if (chartQueryResultsLocal.count == 0 || totalHoursWorked < _minTimeWorked)
        {
            html += VisHelper.NotEnoughData()
            return html;
        }
    
        PrepareDataForVisualization(chartQueryResultsLocal)
        
        
        /////////////////////
        // HTML
        /////////////////////
        html += "<p style='text-align: center;'>Total hours worked on your computer: <strong>" + String(totalHoursWorked) + "</strong>.</p>";
        html += "<div id='" + VisHelper.CreateChartHtmlTitle(title: title) + "' style='height:75%;'  align='center'></div>"
        
        
        /////////////////////
        // JS
        /////////////////////
        var columns = ""
        for (program, time) in chartQueryResultsLocal{
            columns += "['\(program)', \(time)], "
        }
        
        let data = "columns: [ " + columns + "], type: 'pie'"
        
        html += "<script type='text/javascript'>";
        
        let timeType = "h"
        
        html += "var " + VisHelper.CreateChartHtmlTitle(title: title) + " = c3.generate({ bindto: '#" + VisHelper.CreateChartHtmlTitle(title: title) + "', data: { " + data + "}, pie: { label: { format: function (value, ratio, id) { return value + '" + timeType + "';}}}, padding: { top: 0, right: 0, bottom: 0, left: 0 }, legend: { show: true, position: 'bottom' }});"
        html += "</script>"
        
        return html
    }
    
    /// <summary>
    /// Adds all items in the list (in case more than 10) to an Other group
    /// </summary>
    /// <param name="chartQueryResultsLocal"></param>
    func PrepareDataForVisualization(_ chartQueryResultsLocal: Dictionary<String,Double>){
          /*  //if (chartQueryResultsLocal.Count >= _maxNumberOfPrograms)
            //{
            // summarize small parts of work
            var totalHoursWorked = getHoursWorked(chartQueryResultsLocal)

        
        // remove OTHER if it is too small
            if (chartQueryResultsLocal.ContainsKey(Dict.Other) && chartQueryResultsLocal[Dict.Other] <= small)
                keysToRemove.Add(Dict.Other);
                
            foreach (var key in keysToRemove)
            {
                chartQueryResultsLocal.Remove(key);
            }*/
                
            title = "Top \(String(chartQueryResultsLocal.count)) Programs Used"
    }
    
    
}
