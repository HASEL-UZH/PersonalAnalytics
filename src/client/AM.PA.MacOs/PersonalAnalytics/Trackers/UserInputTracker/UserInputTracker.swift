//
//  UserInputTracker.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-03.
//

import Foundation


class UserInputTracker: ITracker{
    var name: String = UserInputSettings.Name
    var isRunning: Bool

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
        inputTimer = Timer.scheduledTimer(timeInterval: inputInterval, target: self, selector: #selector(save), userInfo: nil, repeats: true)
        inputTimer?.tolerance = 5
        isPaused = false
    }
    
    @objc func save(){
        // detailed aggregation (per-second)
        if (UserInputSettings.IsDetailedCollectionEnabled) {
            mouseController.saveDetailedMouseInputs()
            keystrokeController.saveDetailedKeyStrokes()
        }
        
        // default aggregation (per-minute)
        let mouseValues = mouseController.getValues()
        (leftClickCount, rightClickCount, scrollDelta, distance) = mouseValues
                
        let keystrokeValues = keystrokeController.getValues()
        (keyCount, navigateCount, deleteCount) = keystrokeValues
        
        tsEnd = Date()
        UserInputQueries.saveUserInput(input: self)
        
        // prepare/reset for next aggregate
        tsStart = tsEnd
        mouseController.reset()
        keystrokeController.reset()
    }
}
