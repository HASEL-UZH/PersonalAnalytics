//
//  SQLController.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-15.
//

import Foundation
import GRDB

class InputSQLController: SQLController { 


    var MouseClickKeyboardRatio: Double = 3
    var MouseScrollingRatio: Double = 0.008
    var MouseMovementRatio: Double = 0.0028
    
    override init() throws{
        try super.init()
        
        do{
            try dbQueue.inDatabase{ db in
                let rows = try Row.fetchCursor(db, sql: "SELECT COUNT(*) FROM user_input")
                if let row = try rows.next(){
                    let count:Int = row["COUNT(*)"]
                    
                    if count > 100 {
                        let rows = try Row.fetchCursor(db, sql: "SELECT SUM(clickTotal), SUM(keyTotal), SUM(scrollDelta), SUM(movedDistance) FROM user_input")
                        if let row = try rows.next(){
                            let clicks:Double = row["SUM(clickTotal)"]
                            let keystrokes:Double = row["SUM(keyTotal)"]
                            let scrolls:Double = row["SUM(scrollDelta)"]
                            let distance:Double = row["SUM(movedDistance)"]
                            if(!(clicks == 0 || scrolls == 0 || distance == 0 || keystrokes == 0)){
                                MouseClickKeyboardRatio = (keystrokes)/(clicks)
                                MouseScrollingRatio = (keystrokes)/(scrolls)
                                MouseMovementRatio = (keystrokes)/(distance)
                            }
                        }
                    }
                    print(MouseClickKeyboardRatio, MouseScrollingRatio, MouseMovementRatio)
                }
            }
        }
        catch{
            print(error)
        }
    }

    
    func calculateInputLevel(row: Row) -> Int {
        var inputLevel: Double = 0
        inputLevel += row["clickTotal"] * MouseClickKeyboardRatio
        inputLevel += row["keyTotal"]
        inputLevel += row["scrollDelta"] * MouseScrollingRatio
        inputLevel += row["movedDistance"] * MouseMovementRatio
    
        inputLevel.round()
        return Int(inputLevel)
    }
    
 /*   func getAverageInputLevel() -> Int
    {
        var inputLevel = 0

        do{

            try dbQueue.inDatabase{ db in
                let rows = try Row.fetchCursor(db, "SELECT * FROM user_input")
                inputLevel = calculateInputLevel(row: rows)
            }
        }
        catch{
            print(error)
        }
        print(inputLevel)
        return inputLevel
        
    }*/
    
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
        //return dict.Aggregate("", (current, a) => current + (DateTimeHelper.JavascriptTimestampFromDateTime(a.Key) + ", ")).Trim().TrimEnd(',');
    }

    
    func GetUserInputTimelineData(date: Date) -> Dictionary<TimeInterval,Int>{
        let start = getStartHour(date:date)
        let startStr = DateFormatConverter.interval1970ToDateStr(interval: start)
        let end = getEndHour(date:date)
        let endStr = DateFormatConverter.interval1970ToDateStr(interval: end)
        
        let intervalSize: TimeInterval = 600 //10 minute intervals
        var current = start
        
        var results = [TimeInterval: Int]()
        
        do{
            let query = "SELECT * FROM user_input WHERE tsStart >= '\(startStr)' AND tsEnd < '\(endStr)'"
            let rows = try dbQueue.inDatabase{ db in
                try Row.fetchAll(db, sql: query)
            }
            
            while(current < end){
                let next = current + intervalSize
                var inputLevel: Int = 0
                //print(inputLevel)

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
