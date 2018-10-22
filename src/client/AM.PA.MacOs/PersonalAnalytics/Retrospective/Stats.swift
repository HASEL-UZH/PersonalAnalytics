//
//  Stats.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-24.
//

import Foundation

class Stats{
    
    static func getVisualizations() -> String {
        do{
            return getVisualizations(date: Date(), type: VisConstants.Day)
        }
        catch{
            print("error")
            return ""
        }
    }
    
    
    static func getVisualizations(date: Date, type: String) -> String {
        do{
            return TrackerManager.shared.getVisualizations(date: date, type: type)
        }
        catch{
            print("error")
            return ""
        }
    }
    
    // NO LONGER NEEDED
    static func getVisualizationsWeekly(date: Date, type: String) -> String {
        return getVisualizations(date: date, type: type)
    }
    
    
}
