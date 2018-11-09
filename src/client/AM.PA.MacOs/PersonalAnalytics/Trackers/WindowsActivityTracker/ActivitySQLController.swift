//
//  ActivitySQLController.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-29.
//

import Foundation
import GRDB

class ActivitySQLController: SQLController{
    
    override init() throws{
        try super.init()
    }
    
    fileprivate func getValidTimesQuery(taskTimes: [(Double,Double)]) -> String
    {
        var query = ""
        for tuple in taskTimes{
            query += "ZSTARTTIME >= " + String(tuple.0) + " AND "
            query += "ZENDTIME <= " + String(tuple.1) + " OR "
            query += "ZSTARTTIME <= " + String(tuple.1) + " AND ZSTARTTIME >= " + String(tuple.0)
            query += " OR ZENDTIME <= " + String(tuple.1) + " AND ZENDTIME >= " + String(tuple.0)
            query += " OR "
        }
        if(query != ""){
            query = String(query.dropLast(3))
        }
        return query
    }
    
    func getTaskTimes(date: Date, task: String) -> [(TimeInterval, TimeInterval)]{
        
        let start = getStartHour(date: date)
        let end = getEndHour(date: date)
        
        var query = "SELECT * FROM tasks"
        query += " WHERE task == " + task
        query += " AND start >= " + String(start)
        query += " AND end <= "  + String(end)
        var results: [(TimeInterval, TimeInterval)] = []
        
        
        do{
            var config = Configuration()
            config.readonly = true
            
            let dbQueueTask = try DatabaseQueue(path: applicationDocumentsDirectory.appendingPathComponent("tasks.db").absoluteString, configuration: config)
            
            let rows = try dbQueueTask.inDatabase{ db in
                try Row.fetchAll(db, query)
            }
            
            for row in rows{
                let startTime: TimeInterval = row["start"]
                let endTime: TimeInterval = row["end"]
                results.append((startTime, endTime))
            }
            
        }
        catch{
            print("error accessing task database")
        }
        
        return results
        
    }
    
    
    func GetActivityPieChartData(date: Date) -> [String:Double] {
        
        let start = getStartHour(date:date)
        let end = getEndHour(date:date)
        
        var results =  [String: Double]()
    
        do{
            var query = "SELECT ZNAME, SUM(ZENDTIME - ZSTARTTIME) AS DIFF FROM ZACTIVEAPPLICATION "
            query += "WHERE ZSTARTTIME >= " + String(start)
            query += " AND ZENDTIME <= " + String(end)
            query += " AND ZNAME <> 'Idle' "
            query += "GROUP BY ZNAME "
            query += "HAVING DIFF > 360"
            
            let rows = try dbQueue.inDatabase{ db in
                try Row.fetchAll(db, query)
            }
            
            for row in rows{
                let name: String = row["ZNAME"]
                let time: Double = row["DIFF"]
                
                results[name] = ((time/3600)*10).rounded()/10 //convert to hours, rounded to 1 decimal place
            }
        }
        catch{
            print(error)
            print("error accessing database for pie chart")
        }
        
        return results
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
    
    func GetLongestFocusOnProgram(date: Date) -> FocusedWorkDict?{
        let start = getStartHour(date:date)
        let end = getEndHour(date:date)
        
        do{
            var query = "SELECT *, ZENDTIME - ZSTARTTIME AS DIFF FROM ZACTIVEAPPLICATION "
            query += "WHERE ZSTARTTIME >= " + String(start)
            query += " AND ZENDTIME <= " + String(end)
            query += " AND ZNAME <> 'Idle' "
            query += "ORDER BY DIFF DESC "
            query += "LIMIT 1;"
            
            let row = try dbQueue.inDatabase{db in
                try Row.fetchOne(db, query)
            }
            
            if(row != nil){
                let name: String = (row?["ZNAME"])!
                let from:TimeInterval = (row?["ZSTARTTIME"])!
                let to:TimeInterval = (row?["ZENDTIME"])!
                let difference:TimeInterval = (row?["DIFF"])!
                
                return FocusedWorkDict(name: name, to: to, from: from, difference: difference)
            }
            else{
                return nil
            }
            
            
        }
        catch{
            print("error accessing database for Focus")
            return nil
        }
    }
        

    func GetDayTimelineData(date: Date) -> [Activity]{
        var results: [Activity] = []
        let start = getStartHour(date: date)
        let end = getEndHour(date: date)
        
        do{
            var query = "SELECT * FROM ZACTIVEAPPLICATION "
            query += "WHERE ZSTARTTIME >= " + String(start)
            query += " AND ZENDTIME <= " + String(end)
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
            
            results = processWebsites(results)
            
        }
        catch{
            print(error)
            print("error accessing database for GetDayTimelineData")
        }
        return results
    }
    
    
    
    fileprivate func processWebsites(_ input: [Activity]) -> [Activity]{
        var results = input
        let size = results.count
        var i = 0
        var indices: [Int] = []
        var times = [(TimeInterval,TimeInterval)]()
        while( i < size ){
            let name = results[i].name.lowercased()
            if(name == "google chrome"){
                var start = results[i].startTime
                var end = results[i].endTime
                while(i < size && results[i].name.lowercased() == name){
                    end = results[i].endTime
                    indices.append(i)
                    i += 1
                }
                times.append((start,end))
            }
            else{
                i += 1
            }
        }
        
        
        if(times.count > 0){
            do{
                var query: String = "SELECT * FROM ZWEBSITE WHERE"
                
                var lastRange = (0.0,0.0)
                for range in times{
                    var (start, end) = range
                    
                    //allow for variance in recorded time
                    start = start - 5
                    end = end + 5
                    
                    if(start < lastRange.1){
                        start = lastRange.1
                    }
                    lastRange = (start,end)
                    
                    query += " ZTIME >= " + String(start)
                    query += " AND ZTIME <= " + String(end)
                    query += " OR"
                }
                query = String(query.dropLast(3))
                query += " ORDER BY ZTIME"
                
                let rows = try dbQueue.inDatabase{ db in
                    try Row.fetchAll(db, query)
                }
                
                if(rows.count > 0){
                    //remove chrome results
                    results = results.enumerated()
                        .filter{ !indices.contains($0.offset)}
                        .map{$0.element}
                    
                    var i = 0
                    var j = 0
                    var currentRange = times[j]
                    while(i < rows.count) {
                        
                        let row = rows[i]
                        //5 second delay before website recorded, so subtract 5 seconds
                        let start: TimeInterval = row["ZTIME"] - 5.0
                        
                        while(start > currentRange.1){
                            j += 1
                            currentRange = times[j]
                        }
                        
                        var end: TimeInterval
                        
                        if(i + 1 < rows.count){
                            end = rows[i+1]["ZTIME"] - 5.0
                            if(end > currentRange.1){
                                j += 1
                                end = currentRange.1
                                if(j < times.count){
                                    currentRange = times[j]
                                    
                                }
                            }
                        }
                        else{
                            end = currentRange.1
                        }
                        
                        if(end < start){
                            print("here")
                        }
                        
                        
                        let name: String = "Google Chrome"
                        var title: String? = row["ZTITLE"]
                        let URL: String = row["ZURL"]
                        //TODO: remove later
                        if(title == nil){
                            title = ""
                        }
                        results.append(Activity(start: start, end: end, title: title!, name: name, url: URL))
                        i += 1
                    }
                    
                    
                    func sortResults(a: Activity, b: Activity) -> Bool{
                        return a.startTime > b.startTime
                    }
                    results.sort(by: sortResults)
                    
                }
                
            }
            catch{
                print(error)
                print("error accessing websites database")
            }
        }
        return results
    }
    
    
}


