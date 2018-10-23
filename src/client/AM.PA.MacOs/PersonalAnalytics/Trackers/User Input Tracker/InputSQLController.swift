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
                let rows = try Row.fetchCursor(db, "SELECT COUNT(*) FROM ZAGGREGATEDINPUT")
                if let row = try rows.next(){
                    let count:Int = row["COUNT(*)"]
                    
                    if count > 100 {
                        let rows = try Row.fetchCursor(db, "SELECT SUM(ZCLICKCOUNT), SUM(ZKEYTOTAL), SUM(ZSCROLLDELTA), SUM(ZDISTANCE) FROM ZAGGREGATEDINPUT")
                        if let row = try rows.next(){
                            let clicks:Double = row["SUM(ZCLICKCOUNT)"]
                            let keystrokes:Double = row["SUM(ZKEYTOTAL)"]
                            let scrolls:Double = row["SUM(ZSCROLLDELTA)"]
                            let distance:Double = row["SUM(ZDISTANCE)"]
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
        
        inputLevel += row["ZCLICKCOUNT"] * MouseClickKeyboardRatio
        inputLevel += row["ZKEYTOTAL"]
        inputLevel += row["ZSCROLLDELTA"] * MouseScrollingRatio
        inputLevel += row["ZDISTANCE"] * MouseMovementRatio
    
        inputLevel.round()
        return Int(inputLevel)
    }
    
 /*   func getAverageInputLevel() -> Int
    {
        var inputLevel = 0

        do{

            try dbQueue.inDatabase{ db in
                let rows = try Row.fetchCursor(db, "SELECT * FROM ZAGGREGATEDINPUT")
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
        let end = getEndHour(date:date)
                
        let intervalSize: TimeInterval = 600 //10 minute intervals
        var current = start
        
        var results = [TimeInterval: Int]()
        
        
        do{
            var query = "SELECT * FROM ZAGGREGATEDINPUT WHERE ZTIME>=" + String(start)
            query += " AND ZTIME <" + String(end)
            let rows = try dbQueue.inDatabase{ db in
                try Row.fetchAll(db, query)
            }
            
            while(current < end){
                let next = current + intervalSize
                var inputLevel: Int = 0
                //print(inputLevel)

                for row in rows{
                    if(row["ZTIME"] < current){
                        continue
                    }
                    else if(row["ZTIME"] >= next){
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
