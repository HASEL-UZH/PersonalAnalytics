//
//  UserInputTracker.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-03.
//

import Foundation


class UserInputTracker: ITracker{
    var name: String
    var isRunning: Bool
    let type: String = "UserInput"

    
    var leftClickCount: Int
    var rightClickCount: Int
    var distance: Int
    var scrollDelta: Int
    var tsStart: Date
    var tsEnd: Date
    var keyCount: Int
    var navigateCount: Int
    var deleteCount: Int
    let mouseController: MouseActionController
    let keystrokeController: KeystrokeController
    var inputTimer: Timer?
    let inputInterval: TimeInterval = UserInputSettings.UserInputAggregationInterval
    var isPaused = false

    
    init(){
        mouseController = MouseActionController()
        keystrokeController = KeystrokeController()
        self.tsStart = Date()
        self.tsEnd = Date()
        self.leftClickCount = 0
        self.rightClickCount = 0
        self.distance = 0
        self.keyCount = 0
        self.navigateCount = 0
        self.deleteCount = 0
        self.scrollDelta = 0
        
        name = "User Input Tracker"
        if(UserInputSettings.IsDetailedCollectionEnabled){
            name += " (detailed)"
        }
        isRunning = true
        
        inputTimer = Timer.scheduledTimer(timeInterval: inputInterval, target: self,selector: #selector(save), userInfo: nil, repeats: true)
        inputTimer?.tolerance = 5
    }
    
    func createDatabaseTablesIfNotExist() {
        UserInputQueries.createDatabaseTablesIfNotExist()
    }
    
    func updateDatabaseTables(version: Int) {
    }
    
    func getVisualizationsDay(date: Date) -> [IVisualization] {
        return [ ActivityVisualization() ]
    }
    
    func getVisualizationsWeek(date: Date) -> [IVisualization] {
        return [ WeekActivityVisualization() ]
    }
    
    func stop(){
        inputTimer?.invalidate()
        keystrokeController.reset()
        mouseController.reset()
        isPaused = true
    }
    
    func start(){
        if(isPaused == false){
            return
        }
        inputTimer = Timer.scheduledTimer(timeInterval: inputInterval, target: self,
                                          selector: #selector(save), userInfo: nil, repeats: true)
        inputTimer?.tolerance = 5
        isPaused = false
    }

    @objc func save(){
        (leftClickCount, rightClickCount, scrollDelta, distance) = mouseController.getValues()
        mouseController.reset()
        (keyCount, navigateCount, deleteCount) = keystrokeController.getValues()
        keystrokeController.reset()
        tsEnd = Date()
        UserInputQueries.saveUserInput(input: self)
        tsStart = Date() // reset for next aggregate
    }
    
    deinit{
        save()
    }
}
