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

    
    let dataController : DataObjectController
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
        self.dataController = DataObjectController.sharedInstance
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
        let dbController = DatabaseController.getDatabaseController()
        do{
            try dbController.executeUpdate(query: "CREATE TABLE IF NOT EXISTS \(UserInputSettings.DbTableUserInput_v2) (id INTEGER PRIMARY KEY, time TEXT, tsStart TEXT, tsEnd TEXT, keyTotal INTEGER, keyOther INTEGER, keyBackspace INTEGER, keyNavigate INTEGER, clickTotal INTEGER, clickOther INTEGER, clickLeft INTEGER, clickRight INTEGER, scrollDelta INTEGER, movedDistance INTEGER)")
            try dbController.executeUpdate(query: "CREATE TABLE IF NOT EXISTS \(UserInputSettings.DbTableKeyboard_v1) (id INTEGER PRIMARY KEY, time TEXT, timestamp TEXT, keystrokeType TEXT)")
            try dbController.executeUpdate(query: "CREATE TABLE IF NOT EXISTS \(UserInputSettings.DbTableMouseClick_v1) (id INTEGER PRIMARY KEY, time TEXT, timestamp TEXT, x INTEGER, y INTEGER, button TEXT)")
            try dbController.executeUpdate(query: "CREATE TABLE IF NOT EXISTS \(UserInputSettings.DbTableMouseScrolling_v1) (id INTEGER PRIMARY KEY, time TEXT, timestamp TEXT, x INTEGER, y INTEGER, scrollDelta INTEGER)")
            try dbController.executeUpdate(query: "CREATE TABLE IF NOT EXISTS \(UserInputSettings.DbTableMouseMovement_v1) (id INTEGER PRIMARY KEY, time TEXT, timestamp TEXT, x INTEGER, y INTEGER, movedDistance INTEGER)")
        }
        catch{
            print(error)
        }
    }
    
    func updateDatabaseTables(version: Int) {
    }
    
    func getVisualizationsDay(date: Date) -> [IVisualization] {
        var viz: [IVisualization] = []
        do{
            viz.append(try ActivityVisualization())
        }
        catch{
            print(error)
        }
        return viz
    }
    
    func getVisualizationsWeek(date: Date) -> [IVisualization] {
        var viz: [IVisualization] = []
        do{
            viz.append(try WeekActivityVisualization())
        }
        catch{
            print(error)
        }
        return viz
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
        dataController.saveUserInput(aggregatedInput: self)
        tsStart = Date() // reset for next aggregate
    }
    
    deinit{
        save()
    }
    


}
