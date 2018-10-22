
//
//  VisHelper.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-24.
//

import Foundation

class VisHelper{
    
    
    static func NotEnoughData(_ message: String = "We do not have enough data to create a visualization.") -> String{
        
        return "<br/><div style='text-align: center; font-size: 0.66em;'>" + message + "</div>"

    }
    
    static func CreateChartHtmlTitle(title: String) -> String{
        return title.replacingOccurrences(of: " ", with: "_")
    }
    
    static func getMax(_ dict: Dictionary<TimeInterval, Int>) -> Int{
        var max = 0
        for (_, value) in dict{
            if value > max{
                max = value
            }
        }
        return max
    }
    
    static func getAvg(_ dict: Dictionary<TimeInterval, Int>) -> Int{
        var avg = 0
        var count = 0
        for (_, value) in dict{
            avg += value
            count += 1
        }
        
        if(count > 0){
            avg /= count
        }
        
        return avg
        
    }
    
    static func makeTimeAxis(_ dict: Dictionary<TimeInterval, Int>) -> String{
        var axis = ""
        
        for (key, _) in dict{
            axis += String(key * 1000) + ", "// convert to javascript timestamp +
        }
        axis = axis.trimmingCharacters(in: [" ", ","])
        return axis
    }
    
    static func makeFormattedData(_ dict: Dictionary<TimeInterval, Int>) -> String{
        //chartQueryResultsLocal.Aggregate("", (current, p) => current + (p.Value + ", ")).Trim().TrimEnd(",")
        var data = ""
        
        for(_, value) in dict{
            data += String(value) + ", "
        }
        data = data.trimmingCharacters(in: [" ", ","])
        return data
    }
    
}
