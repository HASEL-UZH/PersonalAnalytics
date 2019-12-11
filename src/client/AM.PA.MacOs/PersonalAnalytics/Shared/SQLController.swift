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

    struct EmotionalStateEntry {
        var timestamp: Double
        var activity: String
        var valence: Int
        var arousal: Int
    }
    
    func fetchActiveApplicationsSince(time: Double) -> [ActiveApplicationEntry] {
        var results: [ActiveApplicationEntry] = []

        do{
            let timeStr = DateFormatConverter.interval1970ToDateStr(interval: time)
            let query: String = "SELECT * FROM windows_activity WHERE tsStart >= \(timeStr) ORDER BY tsStart"
            let rows = try dbQueue.inDatabase{ db in
                try Row.fetchAll(db, sql: query)
            }
            for row in rows {
                let startTime: Double = DateFormatConverter.dateStrToInterval1970(str: row["tsStart"])
                let endTime: Double = DateFormatConverter.dateStrToInterval1970(str: row["tsEnd"])
                let appName: String = row["process"]
                let windowTitle: String = row["window"]
                
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
        let timeStr = DateFormatConverter.interval1970ToDateStr(interval: time)
        
        do{
            let query: String = "SELECT * FROM user_input WHERE time >= \(timeStr) ORDER BY time"
            let rows = try dbQueue.inDatabase{ db in
                try Row.fetchAll(db, sql: query)
            }
            for row in rows {
                let clickCount: Int = row["clickTotal"]
                let distance: Int = row["movedDistance"]
                let keyTotal: Int = row["keyTotal"]
                let scrollDelta: Int = row["scrollDelta"]
                let time: Double = row["time"]
                
                results.append(AggregatedInputEntry(clickCount: clickCount, distance: distance, keyTotal: keyTotal, scrollDelta: scrollDelta, time: time))
            }
        }
        catch{
            print(error)
        }
        return results
    }

    func fetchEmotionalStateSince(time: Double) -> [EmotionalStateEntry] {

        var results: [EmotionalStateEntry] = []
        let timeStr = DateFormatConverter.interval1970ToDateStr(interval: time)
        
        do {
            let query = """
                        SELECT * FROM emotional_state
                        WHERE timestamp >= '\(timeStr)'
                        ORDER BY timestamp
                        """
            
            let rows = try dbQueue.inDatabase { db in
                try Row.fetchAll(db, sql: query)
            }
            
            for row in rows {

                let timestamp: Double = DateFormatConverter.dateStrToInterval1970(str: row["timestamp"])
                let activity: String = row["activity"]
                let valence: Int = row["valence"]
                let arousal: Int = row["arousal"]

                results.append(EmotionalStateEntry(timestamp: timestamp, activity: activity, valence: valence, arousal: arousal))
            }

        } catch {
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
            dbQueue = try DatabaseQueue(path: applicationDocumentsDirectory.appendingPathComponent("PersonalAnalytics.dat").absoluteString, configuration: config)
        }
        catch{
            dbQueue = try DatabaseQueue(path: applicationDocumentsDirectory.appendingPathComponent("PersonalAnalytics.dat").absoluteString, configuration: config)
        }
        
    }
    
    
    func getStartHour(date: Date) -> TimeInterval{
        let s = NSCalendar.current.startOfDay(for: date)
        let e = NSCalendar.current.date(byAdding: .day, value: 1, to: s)
        
        let startStr = DateFormatConverter.dateToStr(date: s)
        let endStr = DateFormatConverter.dateToStr(date: e!)
       
        do{
            let query = """
                        SELECT * FROM windows_activity
                        WHERE tsStart >= '\(startStr)' AND tsStart < '\(endStr)' AND process <> 'Idle'
                        ORDER BY tsStart
                        """
            
            let rows = try dbQueue.inDatabase{ db in
                try Row.fetchAll(db, sql: query)
            }
            
            if(rows.count > 0){
                let min = DateFormatConverter.dateStrToInterval1970(str: rows[0]["tsStart"])
                return min - min.truncatingRemainder(dividingBy: 3600) // round down
            }
                
            return -1
            
        }
        catch{
            print("error in getStartHour")
            return -1 as TimeInterval
        }
    }
    
    func getEndHour(date: Date) -> TimeInterval{
        let s = NSCalendar.current.startOfDay(for: date)
        let e = NSCalendar.current.date(byAdding: .day, value: 1, to: s)
        
        let start = DateFormatConverter.dateToStr(date: s)
        // TODO: is force unwrapping dangerous here?
        let end = DateFormatConverter.dateToStr(date: e!)
        
        do{
            var query:String = "SELECT * FROM windows_activity WHERE tsEnd >= '" + start
            query += "' AND tsEnd < '" + end
            query += "' AND process <> 'Idle'"
            
            let rows = try dbQueue.inDatabase{ db in
                try Row.fetchAll(db, sql: query)
            }
            
            if(rows.count > 0){
                let max = DateFormatConverter.dateStrToInterval1970(str:  rows[rows.count - 1]["tsEnd"])
                return max + (3600 - max.truncatingRemainder(dividingBy: 3600)) // round up
            }
                
            return -1
           
        }
        catch{
            print("error in getEndHour")
            return -1 as TimeInterval
        }

    }
}
