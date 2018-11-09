//
//  ITracker.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-27.
//

import Foundation

enum TrackerConstants{
    static let KeyboardEventNotification = Notification.Name("keyEvent")
    static let MouseEventNotification = Notification.Name("mouseEvent")
    static let MouseEvent = "mouseEvent"
    static let KeyEvent = "keyEvent"
}

protocol ITracker {
    
    //list of visualizations to present for the tracker
    var name: String {get set}
    var isRunning: Bool {get set}
    //var isFirstStart{get set}
    
    //TODO: change this to match windows functionalitiy
    func stop()
    func start()
    func getStatus() -> String
    func getVersion() -> String
    func isEnabled() -> Bool
    
    func createDatabaseTablesIfNotExist()
    func updateDatabaseTables(version: Int)
    
    func getVisualizationsDay(date: Date) -> [IVisualization]
    func getVisualizationsWeek(date: Date) -> [IVisualization]
    //TODO: what does this do?
    func getStartScreens()
}

extension ITracker{
    //TODO: override these methods until ready to handle
    func getStartScreens(){
    }
    func getVersion() -> String{
        return ""
    }
    func isEnabled() -> Bool{
        return true
    }
    
    //MARK: Define default behaviour if not overwritten
    func getStatus() -> String{
        if(isRunning){
            return "\(name) is running."
        }
        else{
            return "\(name) is NOT running."
        }
    }
    
    func getVisualizationsDay(date: Date) -> [IVisualization]{
        return []
    }
    
    func getVisualizationsWeek(date: Date) -> [IVisualization]{
        return []
    }
}
