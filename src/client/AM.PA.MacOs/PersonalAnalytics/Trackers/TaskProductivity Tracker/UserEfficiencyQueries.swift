//
//  UserEfficiencyQueries.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-06-05.
//

import Foundation
import GRDB

class UserEfficiencyQueries: SQLController{
    
    
    func CalculateLineChartAxisTicks(date: Date) -> String
    {
        let start = getStartHour(date: date)
        let end = getEndHour(date: date)
        
        let intervalSize: TimeInterval = 3600 //60 minute intervals
        
        var current = start //+ intervalSize * 8
        var ticks = ""
        
        while (current <= end){
            ticks += String(current * 1000) + ", "
            current += intervalSize
        }
        ticks = ticks.trimmingCharacters(in: [" ", ","])
        
        return ticks
    }
    
    func GetTopProgramsUsedWithTimes(date: Date, type: String, max: Int) -> [Activity]{
        var results: [Activity] = []
        let start: TimeInterval
        let end: TimeInterval
        if(type == "week"){
            start = date.startOfWeek!.timeIntervalSince1970
            end = date.endOfWeek!.timeIntervalSince1970
        }
        else{
            start = getStartHour(date: date)
            end = getEndHour(date: date)
        }
        
        let startStr = DateFormatConverter.interval1970ToDateStr(interval: start)
        let endStr = DateFormatConverter.interval1970ToDateStr(interval: end)
        
        do{
            let query = """
                        SELECT * FROM windows_activity
                        WHERE tsStart >= '\(startStr)' AND tsEnd <= '\(endStr)' AND process IN
                            (SELECT process FROM windows_activity
                            WHERE tsStart >= '\(startStr)' AND tsEnd <= '\(endStr)' AND process <> 'Idle' AND process <> 'System Events'
                            GROUP BY process
                            ORDER BY SUM((strftime('%s', tsEnd) - strftime('%s', tsStart))) DESC
                            LIMIT \(max))
                        ORDER BY tsStart
                        """
            
            let rows = try dbQueue.inDatabase{ db in
                try Row.fetchAll(db, sql: query)
            }
            
            for row in rows{
                let start: TimeInterval = DateFormatConverter.dateStrToInterval1970(str: row["tsStart"])
                let end: TimeInterval = DateFormatConverter.dateStrToInterval1970(str: row["tsEnd"])
                let name: String = row["process"]
                var title: String? = row["window"]
                //TODO: remove later
                if(title == nil){
                    title = ""
                }
                results.append(Activity(start: start, end: end, title: title!, name: name))
            }
        }
        catch{
            print(error)
            print("error accessing database for GetTopProgramsUsedWithTimes")
        }
        return results
    }
    
    func GetUserProductivityTimelineData(date: Date, type: String) -> Dictionary<TimeInterval, Int> {
        
        let start: TimeInterval
        let end: TimeInterval
        let table: String
        
        if(type == "week"){
            start = date.startOfWeek!.timeIntervalSince1970
            end = date.endOfWeek!.timeIntervalSince1970
            // TODO: table name from settings
            table = "user_efficiency_survey_day"
        }
        else{
            start = getStartHour(date: date)
            end = getEndHour(date: date)
            table = "user_efficiency_survey"
        }
        
        var resultDict = [TimeInterval:Int]()
        let startStr = DateFormatConverter.interval1970ToDateStr(interval: start)
        let endStr = DateFormatConverter.interval1970ToDateStr(interval: end)
        
        do{
            let query = """
                        SELECT * FROM \(table)
                        WHERE time >= '\(startStr)' AND time < '\(endStr)'
                        """

            let results = try dbQueue.inDatabase{ db in
                try Row.fetchAll(db, sql: query)
            }
            
            
            for result in results{
                let interval = DateFormatConverter.dateStrToInterval1970(str: result["time"])
                resultDict[interval] = result["userProductivity"]
            }
        }
        catch{
            print("error in GetUserProductivityTimelineData SQL")
        }
        
        return resultDict
        
    }
}
