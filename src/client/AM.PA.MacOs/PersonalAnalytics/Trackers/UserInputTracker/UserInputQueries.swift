//
//  SQLController.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-15.
//

import Foundation
import GRDB

class UserInputQueries {

//TODO: evaluate, are the results better with dynamically updated ratios?
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
    
    
    /// Detailed tracking of mouse clicks.
    /// - Parameter clicks: a list of clicks tracked over a 1-min period
    static func saveMouseClicks(clicks: [MouseClickEvent]) {
        let dbController = DatabaseController.getDatabaseController()
        
        if clicks.count == 0 {
            return
        }
        
        do {
            let values = clicks.map({ c -> String in
                let nowStr = DateFormatConverter.dateToStr(date: Date())
                let tsStr = DateFormatConverter.dateToStr(date: c.timestamp)
                return "('\(nowStr)', '\(tsStr)', \(c.x), \(c.y), '\(c.button)')"
            })
            
            let q = """
                    INSERT INTO \(UserInputSettings.DbTableMouseClick_v1) (time, timestamp, x, y, button)
                    VALUES \(values.joined(separator: ","));
                    """
            
            try dbController.executeUpdate(query: q)
            
        } catch {
            print(error)
        }
    }
    
    
    /// Detailed tracking of keystrokes
    /// - Parameter keystrokes: a list of keystrokes over a 1 min period
    static func saveKeystrokes(keystrokes: [KeyStrokeEvent]) {
        let dbController = DatabaseController.getDatabaseController()
        
        if keystrokes.count == 0 {
            return
        }
        
        do {
            let values = keystrokes.map({ ks -> String in
                let nowStr = DateFormatConverter.dateToStr(date: Date())
                let tsStr = DateFormatConverter.dateToStr(date: ks.timestamp)
                return "('\(nowStr)', '\(tsStr)', '\(ks.type)')"
            })
            
            let q = """
                    INSERT INTO \(UserInputSettings.DbTableKeyboard_v1) (time, timestamp, keystrokeType)
                    VALUES \(values.joined(separator: ","));
                    """
            
            try dbController.executeUpdate(query: q)
            
        } catch {
            print(error)
        }
    }
    
    
    /// Saves detailed mouse scrolls with a per-second aggregate
    /// - Parameter scrolls: tracked mouse scrolls over a 1 min period
    static func saveMouseScrolls(scrolls: [MouseScrollSnapshot]) {
        
        if scrolls.count == 0 { return }
        
        var tsCurrent = scrolls.first!.timestamp
        let tsEnd = scrolls.last!.timestamp
        var scrollAggregatedPerSecond = [MouseScrollSnapshot]()

        while (tsCurrent <= tsEnd) {
            let tsCurrentEnd = tsCurrent.addingTimeInterval(1) // 1 sec
            let scrollsThisSec = scrolls.filter { $0.timestamp >= tsCurrent && $0.timestamp < tsCurrentEnd }
            let scrollDeltaThisSec = scrollsThisSec.reduce(0, { sum, scroll in sum + scroll.scrollDelta })
            
            tsCurrent = tsCurrentEnd
            
            if (scrollDeltaThisSec == 0) {
                continue // no data this second
            }
            
            // we use the logic from the Windows version:
            // take the last scroll snapshot of the second and
            // set scrollDelta to the total scrolled distance per sec
            var last = scrollsThisSec.last! // last element does exist here
            last.scrollDelta = scrollDeltaThisSec
            scrollAggregatedPerSecond.append(last)
        }

        if scrollAggregatedPerSecond.count == 0 {
            return
        }
        
        do {
            let dbController = DatabaseController.getDatabaseController()
            
            let values = scrollAggregatedPerSecond.map({ s -> String in
                let nowStr = DateFormatConverter.dateToStr(date: Date())
                let tsStr = DateFormatConverter.dateToStr(date: s.timestamp)
                return "('\(nowStr)', '\(tsStr)', \(s.x), \(s.y), '\(s.scrollDelta)')"
            })
                        
            let q = """
                    INSERT INTO \(UserInputSettings.DbTableMouseScrolling_v1) (time, timestamp, x, y, scrollDelta)
                    VALUES \(values.joined(separator: ","));
                    """
            
            try dbController.executeUpdate(query: q)
            
        } catch {
            print(error)
        }
    }
    
    
    /// Save detailed mouse movements with a per-second aggregate
    /// - Parameter movements: tracked mouse movements over a 1 min period
    static func saveMouseMovements(movements: [MouseMovementSnapshot]) {
        if movements.count == 0 { return }
        
        var tsCurrent = movements.first!.timestamp
        let tsEnd = movements.last!.timestamp
        var movementAggregatedPerSecond = [MouseMovementSnapshot]()

        while (tsCurrent <= tsEnd) {
            let tsCurrentEnd = tsCurrent.addingTimeInterval(1) // 1 sec
            let movementsThisSec = movements.filter { $0.timestamp >= tsCurrent && $0.timestamp < tsCurrentEnd }
            let movedDistanceThisSec = movementsThisSec.reduce(0, { sum, movement in sum + movement.movedDistance })
            
            tsCurrent = tsCurrentEnd
            
            if (movedDistanceThisSec == 0) {
                continue // no data this second
            }
            
            // we use the logic from the Windows version:
            // take the last mouse movement snapshot of the second
            // and set movedDistance to the total distance per sec
            var last = movementsThisSec.last! // last element does exist here
            last.movedDistance = movedDistanceThisSec
            movementAggregatedPerSecond.append(last)
        }

        if movementAggregatedPerSecond.count == 0 {
            return
        }
        
        do {
            let dbController = DatabaseController.getDatabaseController()
            
            let values = movementAggregatedPerSecond.map({ m -> String in
                let nowStr = DateFormatConverter.dateToStr(date: Date())
                let tsStr = DateFormatConverter.dateToStr(date: m.timestamp)
                return "('\(nowStr)', '\(tsStr)', \(m.x), \(m.y), '\(m.movedDistance)')"
            })
                        
            let q = """
                    INSERT INTO \(UserInputSettings.DbTableMouseMovement_v1) (time, timestamp, x, y, movedDistance)
                    VALUES \(values.joined(separator: ","));
                    """
            
            try dbController.executeUpdate(query: q)
            
        } catch {
            print(error)
        }
    }
    
    
    /// minute-aggregate of all user inputs
    /// - Parameter input: the user input counts
    static func saveUserInput(input: UserInputTracker) {
        let dbController = DatabaseController.getDatabaseController()
        
        let keyTotal = input.keyCount + input.deleteCount + input.navigateCount
        let clicksTotal = input.leftClickCount + input.rightClickCount
                
        do {
            let args:StatementArguments = [
                DateFormatConverter.dateToStr(date: Date()),
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
        //return dict.Aggregate("", (current, a) => current + (DateTimeHelper.JavascriptTimestampFromDateTime(a.Key) + ", ")).Trim().TrimEnd(',');
    }

    
    static func GetUserInputTimelineData(date: Date) -> Dictionary<TimeInterval,Int> {
        let dbController = DatabaseController.getDatabaseController()
        let start = date.getStartHour()
        let end = date.getEndHour()
        
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
