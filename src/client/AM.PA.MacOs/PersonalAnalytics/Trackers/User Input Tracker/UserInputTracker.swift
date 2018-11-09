//
//  UserInputTracker.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-03.
//

import Foundation

fileprivate enum Settings{
    static let IsDetailedCollectionEnabled = false
    
    static let DbTableUserInput_v2 = "user_input"; // aggregate of user inputs per minute (use this, not the *_v1 ones if possible!)
    static let DbTableKeyboard_v1 = "user_input_keyboard"; // for old deployments & in case a study needs more detailed data
    static let DbTableMouseClick_v1 = "user_input_mouse_click"; // for old deployments & in case a study needs more detailed data
    static let DbTableMouseScrolling_v1 = "user_input_mouse_scrolling"; // for old deployments & in case a study needs more detailed data
    static let DbTableMouseMovement_v1 = "user_input_mouse_movement"; // for old deployments & in case a study needs more detailed data
}


class UserInputTracker: ITracker{
    var name: String
    var isRunning: Bool
    let type: String = "UserInput"

    
    let dataController : DataObjectController
    var clickCount: Int
    var distance: Int
    var scrollDelta: Int
    var time: Date
    var keyCount: Int
    var navigateCount: Int
    var deleteCount: Int
    let mouseController: MouseActionController
    let keystrokeController: KeystrokeController
    var inputTimer: Timer?
    let inputInterval: TimeInterval = TimeInterval(60) // seconds
    var isPaused = false

    
    init(){
        self.dataController = DataObjectController.sharedInstance
        mouseController = MouseActionController()
        keystrokeController = KeystrokeController()
        self.time = Date()
        self.clickCount = 0
        self.distance = 0
        self.keyCount = 0
        self.navigateCount = 0
        self.deleteCount = 0
        self.scrollDelta = 0
        
        name = "User Input Tracker"
        if(Settings.IsDetailedCollectionEnabled){
            name += " (detailed)"
        }
        isRunning = true
        
        inputTimer = Timer.scheduledTimer(timeInterval: inputInterval, target: self,selector: #selector(save), userInfo: nil, repeats: true)
        inputTimer?.tolerance = 5

        
    }
    
    func createDatabaseTablesIfNotExist() {
        let dbController = DatabaseController.getDatabaseController()
        do{
            try dbController.executeUpdate(query: "CREATE TABLE IF NOT EXISTS " + Settings.DbTableKeyboard_v1 + " (id INTEGER PRIMARY KEY, time TEXT, timestamp TEXT, keystrokeType TEXT)");
            try dbController.executeUpdate(query: "CREATE TABLE IF NOT EXISTS " + Settings.DbTableMouseClick_v1 + " (id INTEGER PRIMARY KEY, time TEXT, timestamp TEXT, x INTEGER, y INTEGER, button TEXT)");
            try dbController.executeUpdate(query: "CREATE TABLE IF NOT EXISTS " + Settings.DbTableMouseScrolling_v1 + " (id INTEGER PRIMARY KEY, time TEXT, timestamp TEXT, x INTEGER, y INTEGER, scrollDelta INTEGER)");
            try dbController.executeUpdate(query: "CREATE TABLE IF NOT EXISTS " + Settings.DbTableMouseMovement_v1 + " (id INTEGER PRIMARY KEY, time TEXT, timestamp TEXT, x INTEGER, y INTEGER, movedDistance INTEGER)");
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
        (clickCount, scrollDelta, distance) = mouseController.getValues()
        mouseController.reset()
        (keyCount, navigateCount, deleteCount) = keystrokeController.getValues()
        keystrokeController.reset()
        time = Date()
        dataController.saveUserInput(aggregatedInput: self)
    }
    
    deinit{
        save()
    }
    


}
