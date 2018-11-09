//
//  TaskProductivityTracker.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-06-26.
//

import Foundation

fileprivate enum Settings{
    static let DbTableIntervalPopup = "user_efficiency_survey"
    static let DbTableDailyPopup = "user_efficiency_survey_day"
}

class TaskProductivityTracker: ITracker{
    
    var name: String
    var isRunning: Bool
    var notificationTimer: Timer?
    var isIdle = false
    var isPaused = false
    let summaryIntervalMinutes: Double = 90
    //var notificationsDisabled: Boolean = false


    init(){
        name = "User Efficiency Survey"
        isRunning = true
        //notificationTimer = Timer.scheduledTimer(timeInterval: summaryIntervalMinutes * 60.0, target: self, selector: #selector(showNotificationThatLinksToSummary), userInfo: nil, repeats: true)
        //notificationTimer?.tolerance = 120
        
        NotificationCenter.default.addObserver(self, selector: #selector(self.handleIdle(_:)), name: NSNotification.Name(rawValue: "isIdle"), object: nil)
    }
    
    func createDatabaseTablesIfNotExist() {
        let dbController = DatabaseController.getDatabaseController()
        do{
            try dbController.executeUpdate(query: "CREATE TABLE IF NOT EXISTS " + Settings.DbTableIntervalPopup + " (id INTEGER PRIMARY KEY, time TEXT, surveyNotifyTime TEXT, surveyStartTime TEXT, surveyEndTime TEXT, userProductivity NUMBER, column1 TEXT, column2 TEXT, column3 TEXT, column4 TEXT, column5 TEXT, column6 TEXT, column7 TEXT, column8 TEXT )");
            try dbController.executeUpdate(query: "CREATE TABLE IF NOT EXISTS " + Settings.DbTableDailyPopup + " (id INTEGER PRIMARY KEY, time TEXT, workDay TEXT, surveyNotifyTime TEXT, surveyStartTime TEXT, surveyEndTime TEXT, userProductivity NUMBER, column1 TEXT, column2 TEXT, column3 TEXT, column4 TEXT, column5 TEXT, column6 TEXT, column7 TEXT, column8 TEXT )");
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
            viz.append(try DayProductivityTimeline())
        }
        catch{
            print(error)
        }
        do{
            viz.append(try DayWeekProductivityTimeline())
        }
        catch{
            print(error)
        }
        return viz
    }
    
    @objc
    func handleIdle(_ notification: NSNotification){
        if(notification.userInfo?["isidle"] as! Bool == true){
            isIdle = true
            //notificationTimer?.invalidate()
        }
        else{
            if(isIdle == true){
                //self.notificationTimer = Timer.scheduledTimer(timeInterval: summaryIntervalMinutes * 60.0, target: self, selector: #selector(TaskProductivityTracker.showNotificationThatLinksToSummary), userInfo: nil, repeats: true)
                isIdle = false
            }
        }
    }
 /*
    func disableNotifications(){
        notificationsDisabled = true
    }
    
    func enableNotifications(){
        notificationsDisabled = false
    }
    */
    func stop(){
        //TODO: do something
    }
    
    func start(){
        //TODO: do something
    }
    
    @objc func showNotificationThatLinksToSummary(){
        /*if(notificationsDisabled){
            return
        }*/
        let note = NSUserNotification()
        note.title = "Task Summarizing"
        note.informativeText = "\(summaryIntervalMinutes) minutes is up, please take 15 seconds to fill out the survey."
        //note.contentImage = NSImage(byReferencingURL: NSURL(string: "http://assets.brand.ubc.ca/signatures/2015/ubc_brand_assets_blue/4_logo/rgb/s4b282c2015.png")!)
        note.hasReplyButton = false
        note.responsePlaceholder = "Thanks! Please enter what you're working on"
        note.actionButtonTitle = "Answer Questions"
        note.hasActionButton = true
        NSUserNotificationCenter.default.deliver(note)
    }
    
    deinit{
        NotificationCenter.default.removeObserver(self, name: Notification.Name("isIdle"), object: nil)
    }
}
