//
//  ActivitySQLController.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-29.
//

import Foundation
import GRDB

class WindowsActivityQueries {
    
//    func getTaskTimes(date: Date, task: String) -> [(TimeInterval, TimeInterval)]{
//
//        let startStr = DateFormatConverter.interval1970ToDateStr(interval: date.getStartHour())
//        let endStr = DateFormatConverter.interval1970ToDateStr(interval: date.getEndHour())
//
//        let query = "SELECT * FROM tasks WHERE task == \(task) AND start >= \(startStr) AND end <= \(endStr)"
//        var results: [(TimeInterval, TimeInterval)] = []
//
//        do{
//            var config = Configuration()
//            config.readonly = true
//
//            let dbQueueTask = try DatabaseQueue(path: applicationDocumentsDirectory.appendingPathComponent("tasks.db").absoluteString, configuration: config)
//
//            let rows = try dbQueueTask.inDatabase{ db in
//                try Row.fetchAll(db, sql: query)
//            }
//
//            for row in rows{
//                let startTime: TimeInterval = row["start"]
//                let endTime: TimeInterval = row["end"]
//                results.append((startTime, endTime))
//            }
//
//        }
//        catch{
//            print("error accessing task database")
//        }
//
//        return results
//
//    }
    
      static func createDatabaseTablesIfNotExist() {
          let dbController = DatabaseController.getDatabaseController()
          let query: String = "CREATE TABLE IF NOT EXISTS \(WindowsActivitySettings.DbTable) (id INTEGER PRIMARY KEY, time TEXT, tsStart TEXT, tsEnd TEXT, window TEXT, process TEXT);"
          do{
              try dbController.executeUpdate(query: query)
          }
          catch{
              print(error)
          }
      }
      
    
      static func saveActiveApplication(app: ActiveApplication) {
          let dbController = DatabaseController.getDatabaseController()
          
          do {
              let args:StatementArguments = [
                  app.time,
                  app.tsStart,
                  app.tsEnd,
                  app.window,
                  app.process
              ]
              
              let q = """
                      INSERT INTO \(WindowsActivitySettings.DbTable) (time, tsStart, tsEnd, window, process)
                      VALUES (?, ?, ?, ?, ?)
                      """
                     
              try dbController.executeUpdate(query: q, arguments:args)
                     
          } catch {
              print(error)
          }
      }
    
    
    static func GetActivityPieChartData(date: Date) -> [String:Double] {
        let dbController = DatabaseController.getDatabaseController()
        let startStr = DateFormatConverter.interval1970ToDateStr(interval: date.getStartHour())
        let endStr = DateFormatConverter.interval1970ToDateStr(interval: date.getEndHour())
        
        var results =  [String: Double]()
    
        do{
            let query = """
                        SELECT process, SUM(strftime('%s', tsEnd) - strftime('%s', tsStart)) AS diff
                        FROM \(WindowsActivitySettings.DbTable)
                        WHERE tsStart >= '\(startStr)' AND tsEnd <= '\(endStr)' AND process <> 'Idle'
                        GROUP BY process
                        HAVING diff > 360
                        """
                    
            let rows = try dbController.executeFetchAll(query: query)
            
            for row in rows{
                let name: String = row["process"]
                let time: Double = row["diff"]
                
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
    
    static func GetLongestFocusOnProgram(date: Date) -> FocusedWorkDict? {
        let dbController = DatabaseController.getDatabaseController()
        let startStr = DateFormatConverter.interval1970ToDateStr(interval: date.getStartHour())
        let endStr = DateFormatConverter.interval1970ToDateStr(interval: date.getEndHour())
        
        do{
            let query = """
                        SELECT process, tsStart, tsEnd, (strftime('%s', tsEnd) - strftime('%s', tsStart)) AS diff
                        FROM \(WindowsActivitySettings.DbTable)
                        WHERE tsStart >= '\(startStr)' AND tsEnd <= '\(endStr)' AND process <> 'Idle'
                        GROUP BY id, tsStart
                        ORDER BY diff DESC
                        LIMIT 1
                        """
            
            let row = try dbController.executeFetchOne(query: query)
            
            let name: String = row["process"]
            let from:TimeInterval = DateFormatConverter.dateStrToInterval1970(str: row["tsStart"])
            let to:TimeInterval = DateFormatConverter.dateStrToInterval1970(str: row["tsEnd"])
            let difference:TimeInterval = row["diff"]
                
            return FocusedWorkDict(name: name, to: to, from: from, difference: difference)
        }
        catch{
            print("error accessing database for Focus")
            return nil
        }
    }
        

    static func GetDayTimelineData(date: Date) -> [Activity]{
        var results: [Activity] = []
        let dbController = DatabaseController.getDatabaseController()
        let startStr = DateFormatConverter.interval1970ToDateStr(interval: date.getStartHour())
        let endStr = DateFormatConverter.interval1970ToDateStr(interval: date.getEndHour())
        
        do{
            let query = """
                        SELECT * FROM \(WindowsActivitySettings.DbTable)
                        WHERE tsStart >= '\(startStr)' AND tsEnd <= '\(endStr)'
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
            print("error accessing database for GetDayTimelineData")
        }
        return results
    }
}


