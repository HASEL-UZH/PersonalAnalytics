//
//  UserEfficiencyQueries.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-06-05.
//

import Foundation
import GRDB

class UserEfficiencyQueries {
    
    static func createDatabaseTablesIfNotExist() {
        let dbController = DatabaseController.getDatabaseController()

        do{
            try dbController.executeUpdate(query: "CREATE TABLE IF NOT EXISTS \(UserEfficiencySettings.DbTableIntervalPopup) (id INTEGER PRIMARY KEY, time TEXT, surveyNotifyTime TEXT, surveyStartTime TEXT, surveyEndTime TEXT, userProductivity NUMBER, column1 TEXT, column2 TEXT, column3 TEXT, column4 TEXT, column5 TEXT, column6 TEXT, column7 TEXT, column8 TEXT )");
            try dbController.executeUpdate(query: "CREATE TABLE IF NOT EXISTS \(UserEfficiencySettings.DbTableDailyPopup) (id INTEGER PRIMARY KEY, time TEXT, workDay TEXT, surveyNotifyTime TEXT, surveyStartTime TEXT, surveyEndTime TEXT, userProductivity NUMBER, column1 TEXT, column2 TEXT, column3 TEXT, column4 TEXT, column5 TEXT, column6 TEXT, column7 TEXT, column8 TEXT )");
        }
        catch{
            print(error)
        }
    }
    
    static func saveUserEfficiency(userProductivity: Int, surveyNotifyTime: Date, surveyStartTime: Date, surveyEndTime: Date){
        let dbController = DatabaseController.getDatabaseController()
        
        do {
            let args:StatementArguments = [
                Date(),
                surveyNotifyTime,
                surveyStartTime,
                surveyEndTime,
                userProductivity
            ]
            
            let q = """
                    INSERT INTO \(UserEfficiencySettings.DbTableIntervalPopup) (time, surveyNotifyTime, surveyStartTime, surveyEndTime, userProductivity)
                    VALUES (?, ?, ?, ?, ?)
                    """
                    
            try dbController.executeUpdate(query: q, arguments:args)
                    
        } catch {
            print(error)
        }
    }
    
    static func CalculateLineChartAxisTicks(date: Date) -> String {
        let start = date.getStartHour()
        let end = date.getEndHour()
        
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
    
    static func GetTopProgramsUsedWithTimes(date: Date, type: String, max: Int) -> [Activity]{
        var results: [Activity] = []
        let dbController = DatabaseController.getDatabaseController()
        let start: TimeInterval
        let end: TimeInterval
        if(type == "week"){
            start = date.startOfWeek!.timeIntervalSince1970
            end = date.endOfWeek!.timeIntervalSince1970
        }
        else{
            start = date.getStartHour()
            end = date.getEndHour()
        }
        
        let startStr = DateFormatConverter.interval1970ToDateStr(interval: start)
        let endStr = DateFormatConverter.interval1970ToDateStr(interval: end)
        
        do{
            let query = """
                        SELECT * FROM \(WindowsActivitySettings.DbTable)
                        WHERE tsStart >= '\(startStr)' AND tsEnd <= '\(endStr)' AND process IN
                        (SELECT process FROM \(WindowsActivitySettings.DbTable)
                            WHERE tsStart >= '\(startStr)' AND tsEnd <= '\(endStr)' AND process <> 'Idle' AND process <> 'System Events'
                            GROUP BY process
                            ORDER BY SUM((strftime('%s', tsEnd) - strftime('%s', tsStart))) DESC
                            LIMIT \(max))
                        ORDER BY tsStart
                        """
            
            let rows = try dbController.executeFetchAll(query: query)
            
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
    
    static func GetUserProductivityTimelineData(date: Date, type: String) -> Dictionary<TimeInterval, Int> {
        let dbController = DatabaseController.getDatabaseController()
        let start: TimeInterval
        let end: TimeInterval
        let table: String
        
        if(type == "week"){
            start = date.startOfWeek!.timeIntervalSince1970
            end = date.endOfWeek!.timeIntervalSince1970
            // TODO: table name from settings
            table = UserEfficiencySettings.DbTableDailyPopup
        }
        else{
            start = date.getStartHour()
            end = date.getEndHour()
            table = UserEfficiencySettings.DbTableIntervalPopup
        }
        
        var resultDict = [TimeInterval:Int]()
        let startStr = DateFormatConverter.interval1970ToDateStr(interval: start)
        let endStr = DateFormatConverter.interval1970ToDateStr(interval: end)
        
        do{
            let query = """
                        SELECT * FROM \(table)
                        WHERE time >= '\(startStr)' AND time < '\(endStr)'
                        """

            let results = try dbController.executeFetchAll(query: query)
            
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
