//
//  DateTimeHelper.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-06-06.
//

import Foundation

extension Date {
    struct Gregorian {
        static let calendar = Calendar(identifier: .gregorian)
    }
    var startOfWeek: Date? {
        return Gregorian.calendar.date(from: Gregorian.calendar.dateComponents([.yearForWeekOfYear, .weekOfYear], from: self))
    }
    var endOfWeek: Date? {
        return startOfWeek?.addingTimeInterval(24*60*60*7)
    }

    func getStartHour() -> TimeInterval{
        let dbController = DatabaseController.getDatabaseController()

        let start = NSCalendar.current.startOfDay(for: self)
        let end = NSCalendar.current.date(byAdding: .day, value: 1, to: start)
        
        let startStr = DateFormatConverter.dateToStr(date: start)
        let endStr = DateFormatConverter.dateToStr(date: end!)
       
        do{
            let query = """
                        SELECT * FROM \(WindowsActivitySettings.DbTable)
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

    func getEndHour() -> TimeInterval{
        let dbController = DatabaseController.getDatabaseController()

        let start = NSCalendar.current.startOfDay(for: self)
        let end = NSCalendar.current.date(byAdding: .day, value: 1, to: start)
        
        let startStr = DateFormatConverter.dateToStr(date: start)
        let endStr = DateFormatConverter.dateToStr(date: end!)
        
        do{
            let query = """
                        SELECT * FROM \(WindowsActivitySettings.DbTable)
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
}
