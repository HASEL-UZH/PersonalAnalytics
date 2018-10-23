//
//  Stats.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-24.
//

import Foundation

class Stats{
    
    static func getVisualizations() -> String {
        return getVisualizations(date: Date(), type: VisConstants.Day)
    }
    
    
    static func getVisualizations(date: Date, type: String) -> String {
        return TrackerManager.shared.getVisualizations(date: date, type: type)
    }
    
    //TODO: do we really need this?
    static func getVisualizationsWeekly(date: Date, type: String) -> String {
        return getVisualizations(date: date, type: type)
    }
    
    
}
