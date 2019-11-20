//
//  SQLController.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-29.
//

import Foundation


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
