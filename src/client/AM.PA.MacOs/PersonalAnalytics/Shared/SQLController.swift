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
    
    struct EmotionalStateEntry {
        var timestamp: Double
        var activity: String
        var valence: Int
        var arousal: Int
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
}


// TODO: what to do with this function?
func getStartHour(date: Date) -> TimeInterval{
    let dbController = DatabaseController.getDatabaseController()

    let start = NSCalendar.current.startOfDay(for: date)
    let end = NSCalendar.current.date(byAdding: .day, value: 1, to: start)
    
    let startStr = DateFormatConverter.dateToStr(date: start)
    let endStr = DateFormatConverter.dateToStr(date: end!)
   
    do{
        let query = """
                    SELECT * FROM windows_activity
                    WHERE tsStart >= '\(startStr)' AND tsStart < '\(endStr)' AND process <> 'Idle'
                    ORDER BY tsStart
                    """
        
        let rows = try dbController.executeFetchAll(query: query)
        
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


// TODO: what to do with this function?
func getEndHour(date: Date) -> TimeInterval{
    let dbController = DatabaseController.getDatabaseController()

    let start = NSCalendar.current.startOfDay(for: date)
    let end = NSCalendar.current.date(byAdding: .day, value: 1, to: start)
    
    let startStr = DateFormatConverter.dateToStr(date: start)
    let endStr = DateFormatConverter.dateToStr(date: end!)
    
    do{
        let query = """
                    SELECT * FROM windows_activity
                    WHERE tsEnd >= '\(startStr)' AND tsEnd < '\(endStr)' AND process <> 'Idle'
                    """
        
        let rows = try dbController.executeFetchAll(query: query)
        
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
