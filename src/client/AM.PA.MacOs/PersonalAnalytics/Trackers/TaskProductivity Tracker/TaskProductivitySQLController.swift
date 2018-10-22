//
//  ProductivitySQLController.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-06-05.
//

import Foundation
import GRDB

class ProductivitySQLController: SQLController{
    
    
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
        
        
        do{
            var query = "SELECT * FROM ZACTIVEAPPLICATION"
            query += " WHERE ZSTARTTIME >= " + String(start)
            query += " AND ZENDTIME <= " + String(end)
            query += " AND ZNAME IN (SELECT ZNAME FROM ZACTIVEAPPLICATION"
            query += " WHERE ZSTARTTIME >= " + String(start)
            query += " AND ZENDTIME <= " + String(end)
            query += " AND ZNAME <> 'Idle'"
            query += " AND ZNAME <> 'System Events'"
            query += " GROUP BY ZNAME"
            query += " ORDER BY SUM(ZENDTIME-ZSTARTTIME) DESC"
            query += " LIMIT " + String(max) + ")"
            query += " ORDER BY ZSTARTTIME"
            
            let rows = try dbQueue.inDatabase{ db in
                try Row.fetchAll(db, query)
            }
            
            for row in rows{
                let start: TimeInterval = row["ZSTARTTIME"]
                let end: TimeInterval = row["ZENDTIME"]
                let name: String = row["ZNAME"]
                var title: String? = row["ZTITLE"]
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
        if(type == "week"){
            start = date.startOfWeek!.timeIntervalSince1970
            end = date.endOfWeek!.timeIntervalSince1970
        }
        else{
            start = getStartHour(date: date)
            end = getEndHour(date: date)
        }
        
        var resultDict = [TimeInterval:Int]()

        
        do{
            var query = "SELECT * FROM ZSUMMARY "
            query += "WHERE ZSUBMISSIONTIME >= " + String(start)
            query += " AND ZSUBMISSIONTIME < " + String(end)
            
            let results = try dbQueue.inDatabase{ db in
                try Row.fetchAll(db, query)
            }
            
            
            for result in results{
                resultDict[result["ZSUBMISSIONTIME"]] = result["ZPERCIEVEDPRODUCTIVITY"]
            }
        }
        catch{
            print("error in GetUserProductivityTimelineData SQL")
        }
        
        return resultDict
        
    }
    
}
