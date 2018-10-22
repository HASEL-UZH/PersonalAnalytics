//
//  SQLController.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-29.
//

import Foundation
import GRDB

class SQLController{
    
    var applicationDocumentsDirectory: URL = {
        // The directory the application uses to store the Core Data store file. This code uses a directory named "PersonalAnalytics" in the user's Application Support directory.
        let urls = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)
        let appSupportURL = urls[urls.count - 1]
        return appSupportURL.appendingPathComponent("PersonalAnalytics")
    }()
    
    struct ActiveApplicationEntry {
        var windowTitle: String
        var appName: String
        var startTime: Double
        var endTime: Double
    }
    
    struct AggregatedInputEntry {
        var clickCount : Int
        var distance: Int
        var keyTotal: Int
        var scrollDelta: Int
        var time: Double
    }
    
    func fetchActiveApplicationsSince(time: Double) -> [ActiveApplicationEntry] {
        var results: [ActiveApplicationEntry] = []

        do{
            var query: String = "SELECT * FROM ZACTIVEAPPLICATION WHERE ZSTARTTIME >= " + String(time) + " ORDER BY ZSTARTTIME"
            let rows = try dbQueue.inDatabase{ db in
                try Row.fetchAll(db, query)
            }
            for row in rows {
                let startTime: Double = row["ZSTARTTIME"]
                let endTime: Double = row["ZENDTIME"]
                let appName: String = row["ZNAME"]
                let windowTitle: String = row["ZTITLE"]
                
                results.append(ActiveApplicationEntry(windowTitle: windowTitle, appName: appName, startTime: startTime, endTime: endTime))
            }
        }
        catch{
            print(error)
        }
        return results
    }
    
    func fetchAggregatedInputSince(time: Double) -> [AggregatedInputEntry] {
        var results: [AggregatedInputEntry] = []
        
        do{
            var query: String = "SELECT * FROM ZAGGREGATEDINPUT WHERE ZTIME >= " + String(time) + " ORDER BY ZTIME"
            let rows = try dbQueue.inDatabase{ db in
                try Row.fetchAll(db, query)
            }
            for row in rows {
                let clickCount: Int = row["ZCLICKCOUNT"]
                let distance: Int = row["ZDISTANCE"]
                let keyTotal: Int = row["ZKEYTOTAL"]
                let scrollDelta: Int = row["ZSCROLLDELTA"]
                let time: Double = row["ZTIME"]
                
                results.append(AggregatedInputEntry(clickCount: clickCount, distance: distance, keyTotal: keyTotal, scrollDelta: scrollDelta, time: time))
            }
        }
        catch{
            print(error)
        }
        return results
    }
    
    let dbQueue: DatabaseQueue
    
    init() throws{
        ()
        var config = Configuration()
        config.readonly = true
        do{
            dbQueue = try DatabaseQueue(path: applicationDocumentsDirectory.appendingPathComponent("CocoaAppCD.dat").absoluteString, configuration: config)
        }
        catch{
            DataObjectController.sharedInstance.saveContext()
            dbQueue = try DatabaseQueue(path: applicationDocumentsDirectory.appendingPathComponent("CocoaAppCD.dat").absoluteString, configuration: config)
        }
        
    }
    
    
    func getStartHour(date: Date) -> TimeInterval{
        let s = NSCalendar.current.startOfDay(for: date)
        let e = NSCalendar.current.date(byAdding: .day, value: 1, to: s)
        
        let start = s.timeIntervalSince1970
        let end = e!.timeIntervalSince1970
        
        do{
            var query:String = "SELECT * FROM ZACTIVEAPPLICATION WHERE ZSTARTTIME>=" + String(start)
            query += " AND ZSTARTTIME <" + String(end)
            query += " AND ZNAME <> 'Idle'"
            query += " ORDER BY ZSTARTTIME"
            let rows = try dbQueue.inDatabase{ db in
                
                try Row.fetchAll(db, query)
            }
            var min: TimeInterval
            if(rows.count > 0){
                min = rows[0]["ZSTARTTIME"]
                min = min - min.truncatingRemainder(dividingBy: 3600) // round down
            }
            else{
                min = -1
            }
            return min
            
        }
        catch{
            print("error in getStartHour")
            return -1 as TimeInterval
        }
    }
    
    func getEndHour(date: Date) -> TimeInterval{
        let s = NSCalendar.current.startOfDay(for: date)
        let e = NSCalendar.current.date(byAdding: .day, value: 1, to: s)
        
        let start = s.timeIntervalSince1970
        let end = e!.timeIntervalSince1970
        
        do{
            var query:String = "SELECT * FROM ZACTIVEAPPLICATION WHERE ZENDTIME>=" + String(start)
            query += " AND ZENDTIME <" + String(end)
            query += " AND ZNAME <> 'Idle'"
            let rows = try dbQueue.inDatabase{ db in
                try Row.fetchAll(db, query)
            }
            var max: TimeInterval
            if(rows.count > 0){
                max = rows[rows.count - 1]["ZENDTIME"]
                max = max + (3600 - max.truncatingRemainder(dividingBy: 3600)) // round up
            }
            else{
                max = -1
            }
            return max
        }
        catch{
            print("error in getStartHour")
            return -1 as TimeInterval
        }

    }
    
}
