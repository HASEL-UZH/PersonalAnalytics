//
//  SQLController.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-15.
//

import Foundation
import GRDB

class UserInputQueries {

    
//    init() {
//
//        let dbController = DatabaseController.getDatabaseController()
//
//        do{
//            try dbController.dbQueue.inDatabase{ db in
//                let rows = try Row.fetchCursor(db, sql: "SELECT COUNT(*) FROM \(UserInputSettings.DbTableUserInput_v2)")
//                if let row = try rows.next(){
//                    let count:Int = row["COUNT(*)"]
//
//                    if count > 100 {
//                        let rows = try Row.fetchCursor(db, sql: "SELECT SUM(clickTotal), SUM(keyTotal), SUM(scrollDelta), SUM(movedDistance) FROM \(UserInputSettings.DbTableUserInput_v2)")
//                        if let row = try rows.next(){
//                            let clicks:Double = row["SUM(clickTotal)"]
//                            let keystrokes:Double = row["SUM(keyTotal)"]
//                            let scrolls:Double = row["SUM(scrollDelta)"]
//                            let distance:Double = row["SUM(movedDistance)"]
//                            if(!(clicks == 0 || scrolls == 0 || distance == 0 || keystrokes == 0)){
//                                MouseClickKeyboardRatio = (keystrokes)/(clicks)
//                                MouseScrollingKeyboardRatio = (keystrokes)/(scrolls)
//                                MouseMovementKeyboardRatio = (keystrokes)/(distance)
//                            }
//                        }
//                    }
//                    print(MouseClickKeyboardRatio, MouseScrollingKeyboardRatio, MouseMovementKeyboardRatio)
//                }
//            }
//        }
//        catch{
//            print(error)
//        }
//    }
    
    
    static func createDatabaseTablesIfNotExist() {
        let dbController = DatabaseController.getDatabaseController()
        do {
            try dbController.executeUpdate(query: "CREATE TABLE IF NOT EXISTS \(UserInputSettings.DbTableUserInput_v2) (id INTEGER PRIMARY KEY, time TEXT, tsStart TEXT, tsEnd TEXT, keyTotal INTEGER, keyOther INTEGER, keyBackspace INTEGER, keyNavigate INTEGER, clickTotal INTEGER, clickOther INTEGER, clickLeft INTEGER, clickRight INTEGER, scrollDelta INTEGER, movedDistance INTEGER)")
            try dbController.executeUpdate(query: "CREATE TABLE IF NOT EXISTS \(UserInputSettings.DbTableKeyboard_v1) (id INTEGER PRIMARY KEY, time TEXT, timestamp TEXT, keystrokeType TEXT)")
            try dbController.executeUpdate(query: "CREATE TABLE IF NOT EXISTS \(UserInputSettings.DbTableMouseClick_v1) (id INTEGER PRIMARY KEY, time TEXT, timestamp TEXT, x INTEGER, y INTEGER, button TEXT)")
            try dbController.executeUpdate(query: "CREATE TABLE IF NOT EXISTS \(UserInputSettings.DbTableMouseScrolling_v1) (id INTEGER PRIMARY KEY, time TEXT, timestamp TEXT, x INTEGER, y INTEGER, scrollDelta INTEGER)")
            try dbController.executeUpdate(query: "CREATE TABLE IF NOT EXISTS \(UserInputSettings.DbTableMouseMovement_v1) (id INTEGER PRIMARY KEY, time TEXT, timestamp TEXT, x INTEGER, y INTEGER, movedDistance INTEGER)")
        }
        catch {
            print(error)
        }
    }
    
    
    static func saveUserInput(aggregatedInput input: UserInputTracker) {
        let dbController = DatabaseController.getDatabaseController()
        
        let keyTotal = input.keyCount + input.deleteCount + input.navigateCount
        let clicksTotal = input.leftClickCount + input.rightClickCount
                
        do {
            let args:StatementArguments = [
                Date(),
                input.tsStart,
                input.tsEnd,
                keyTotal,
                input.keyCount,
                input.deleteCount,
                input.navigateCount,
                clicksTotal,
                -1, // TODO: clickOther
                input.leftClickCount,
                input.rightClickCount,
                input.scrollDelta,
                input.distance]
            
            let q = """
                    INSERT INTO \(UserInputSettings.DbTableUserInput_v2) (time, tsStart, tsEnd, keyTotal, keyOther, keyBackspace, keyNavigate, clickTotal, clickOther, clickLeft, clickRight, scrollDelta, movedDistance)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    """
            
            try dbController.executeUpdate(query: q, arguments:args)
            
        } catch {
            print(error)
        }
    }
    
    
    // TODO: needs refactoring
    struct AggregatedInputEntry {
        var clickCount : Int
        var distance: Int
        var keyTotal: Int
        var scrollDelta: Int
        var time: Double
    }

    
    static func fetchAggregatedInputSince(time: Double) -> [AggregatedInputEntry] {
        var results: [AggregatedInputEntry] = []
        let timeStr = DateFormatConverter.interval1970ToDateStr(interval: time)
        let dbController = DatabaseController.getDatabaseController()

        do{
            let query: String = "SELECT * FROM \(UserInputSettings.DbTableUserInput_v2) WHERE time >= \(timeStr) ORDER BY time"
            
            let rows = try dbController.executeFetchAll(query: query)
            
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
    
    
    private static func calculateInputLevel(row: Row) -> Int {
        var inputLevel: Double = 0
        inputLevel += row["clickTotal"] * UserInputSettings.MouseClickKeyboardRatio
        inputLevel += row["keyTotal"]
        inputLevel += row["scrollDelta"] * UserInputSettings.MouseScrollingKeyboardRatio
        inputLevel += row["movedDistance"] * UserInputSettings.MouseMovementKeyboardRatio
    
        inputLevel.round()
        return Int(inputLevel)
    }
    
 /*   func getAverageInputLevel() -> Int
    {
        var inputLevel = 0

        do{

            try dbQueue.inDatabase{ db in
     let rows = try Row.fetchCursor(db, "SELECT * FROM \(UserInputSettings.DbTableUserInput_v2)")
                inputLevel = calculateInputLevel(row: rows)
            }
        }
        catch{
            print(error)
        }
        print(inputLevel)
        return inputLevel
        
    }*/
    
    static func CalculateLineChartAxisTicks(date: Date) -> String {
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
        //return dict.Aggregate("", (current, a) => current + (DateTimeHelper.JavascriptTimestampFromDateTime(a.Key) + ", ")).Trim().TrimEnd(',');
    }

    
    static func GetUserInputTimelineData(date: Date) -> Dictionary<TimeInterval,Int> {
        let dbController = DatabaseController.getDatabaseController()
        let start = getStartHour(date:date)
        let end = getEndHour(date:date)
        
        let startStr = DateFormatConverter.interval1970ToDateStr(interval: start)
        let endStr = DateFormatConverter.interval1970ToDateStr(interval: end)
        
        var current = start
        
        var results = [TimeInterval: Int]()
        
        do{
            let query = "SELECT * FROM \(UserInputSettings.DbTableUserInput_v2) WHERE tsStart >= '\(startStr)' AND tsEnd < '\(endStr)'"
           
            let rows = try dbController.executeFetchAll(query: query)
            
            while(current < end){
                let next = current + UserInputSettings.UserInputVisInterval
                var inputLevel: Int = 0

                for row in rows{
                    let time = DateFormatConverter.dateStrToInterval1970(str: row["time"])
                    if(time < current){
                        continue
                    }
                    else if(time >= next){
                        break
                    }
                    else{
                        inputLevel += calculateInputLevel(row: row)
                    }
                }
                results[current] = inputLevel
                current = next
            }
            
        }
        catch{
            print("error accessing database for input timeline")
        }
        
        return results


    }
    
    /*func getInputLevelsForDate(date: Date){
        let start = NSCalendar.current.startOfDay(for: date)
        let end = NSCalendar.current.date(byAdding: .day, value: 1, to: start)
        
        let x: TimeInterval = (end?.timeIntervalSince(start))!
        let intervalSize: TimeInterval = x/48
        
        var results: [Int] = []
        var tmp = start
        
        while(tmp < end!){
            let tmp2 = tmp.addingTimeInterval(intervalSize)
            print("tmp: ", tmp)
            print("tmp2: ", tmp2)
            results.append(getInputLevelBetweenInterval(startTime: tmp, endTime: tmp2))
            tmp = tmp2
        }
        
        
        print(date)
        print(start)
        print(end)
        print(results)
    }*/

    
}
