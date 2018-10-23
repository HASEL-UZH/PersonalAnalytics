//
//  DayFragmentationTimeline.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-29.
//
//  Adapted from Windows version created by André Meyer


import Foundation


class DayMostFocusedProgram: Visualization{
    
    let Size: String
    let color = AppConstants.retrospectiveColor
    let title: String
    let sql: ActivitySQLController
    var _type: [String] = [VisConstants.Day]
    let _minFocusTime = 2.0

    
    required init() throws {
        Size = "Small"
        title = "Longest Time Focused in an App"
        sql = try ActivitySQLController()
    }
    
    struct FocusedWorkDict{
        var name: String
        var to: TimeInterval
        var from: TimeInterval
        var difference: TimeInterval
        
        init(name: String, to: TimeInterval, from: TimeInterval, difference: TimeInterval){
            self.name = name
            self.to = to
            self.from = from
            self.difference = difference
        }
        
    }
    
    func getHtml(_ _date: Date, type: String) -> String {
        
        if(!_type.contains(type)){
            return ""
        }
        var html = ""
        
        /////////////////////
        // fetch data sets
        /////////////////////
        let queryResultsLocal = sql.GetLongestFocusOnProgram(date: _date);
        
        if(queryResultsLocal == nil){
            return ""
        }
        
        let durInMin = (queryResultsLocal == nil) ? 0 : queryResultsLocal!.difference / 60.0;
        
        let from = Date(timeIntervalSince1970: queryResultsLocal!.from)
        let to = Date(timeIntervalSince1970: queryResultsLocal!.to)
        
        let dateFormatter = DateFormatter()
        dateFormatter.dateFormat = "h:mma"

        
        
        
        if (queryResultsLocal == nil || durInMin <= _minFocusTime)
        {
            html += VisHelper.NotEnoughData("We either don't have enough data or you didn't focus on a single program for more than \(_minFocusTime) minutes on this day.")
            return html;
        }
        
        /////////////////////
        // HTML
        /////////////////////
        html += "<p style='text-align: center; margin-top:-0.7em;'><strong style='font-size:2.5em; color:" + color + ";'>" + String(durInMin.rounded()) + "</strong> min</p>"
        html += "<p style='text-align: center; margin-top:-0.7em;'>in \(queryResultsLocal!.name) <br />from \(dateFormatter.string(from: from)) to \(dateFormatter.string(from: to))</p>"
        
        return html
    }

}
