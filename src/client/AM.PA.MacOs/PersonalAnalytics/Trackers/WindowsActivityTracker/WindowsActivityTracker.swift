//
//  WindowsActivityTracker.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-29.
//

import Foundation
import CoreGraphics
import Quartz

struct ActiveApplication {
    var time: Date
    var tsStart: Date
    var tsEnd: Date
    var window: String
    var process: String
}

class WindowsActivityTracker: ITracker{

    var name = WindowsActivitySettings.Name
    var isRunning: Bool
    
    var lastApplication: ActiveApplication?
    let defaults = UserDefaults.standard
    var applicationTimer: Timer?
    var idleTime: CFTimeInterval = 0
    var idleTimer: Timer?
    var isIdle: Bool
    var unsafeChars : CharacterSet
    let ignorelist = ["loginwindow", "com.apple.WebKit.WebContent", "CoreServicesUIAgent", "System Events","SecurityAgent", "PersonalAnalytics Web Content", "ScreenSaverEngine"]
    var isPaused = false

  
    init(){
        isIdle = false
        isRunning = true
        
        unsafeChars = NSCharacterSet.alphanumerics
        unsafeChars.insert(charactersIn: "<>?';:\",.][{}\\|+=-_)(*&^%$#@!~`")
        unsafeChars = unsafeChars.inverted
        
        applicationTimer = Timer.scheduledTimer(timeInterval: 1, target: self, selector: #selector(saveCurrentApplicationToMemory), userInfo: nil, repeats: true)
        idleTimer = Timer.scheduledTimer(timeInterval: 10, target: self, selector: #selector(checkForIdle), userInfo: nil, repeats: true)
        
        applicationTimer!.tolerance = 10
        idleTimer!.tolerance = 5
        
        // screen recording is required to read process window titles in catalina (10.15).
        // https://developer.apple.com/videos/play/wwdc2019/701/
        if !canRecordScreen() {
            triggerScreenRecordingPrivacyDialog()
        }
        
        NSWorkspace.shared.notificationCenter.addObserver(self,
                                                            selector: #selector(saveCurrentApplicationToMemory),
                                                            name: NSWorkspace.didActivateApplicationNotification,
                                                            object: nil)
        
        NSWorkspace.shared.notificationCenter.addObserver(self,
                                                            selector: #selector(onSleepReset),
                                                            name: NSWorkspace.willSleepNotification,
                                                            object: nil)

    }
    
    private func triggerScreenRecordingPrivacyDialog() {
        // this should trigger a system warning and lead the user
        // to the Security/Privacy --> Screen Recording List with PA in it.
        CGDisplayCreateImage(CGMainDisplayID())
    }
    
    // https://stackoverflow.com/questions/56597221/detecting-screen-recording-settings-on-macos-catalina
    private func canRecordScreen() -> Bool {
        guard let windows = CGWindowListCopyWindowInfo([.optionOnScreenOnly], kCGNullWindowID) as? [[String: AnyObject]] else { return false }
        return windows.allSatisfy({ window in
            let windowName = window[kCGWindowName as String] as? String
            return windowName != nil
        })
    }
    
    func createDatabaseTablesIfNotExist() {
        WindowsActivityQueries.createDatabaseTablesIfNotExist()
    }
    
    func updateDatabaseTables(version: Int) {
    }
    
    func getVisualizationsDay(date: Date) -> [IVisualization] {
        return [
            DayProgamsUsedPieChart(),
            DayMostFocusedProgram(),
            DayFragmentationTimeline(),
            DayTimeSpentVisualization()
        ]
    }
    
    func getVisualizationsWeek(date: Date) -> [IVisualization] {
        return [WeekProgramsUsedTable()]
    }
    
    func stop(){
        applicationTimer?.invalidate()
        idleTimer?.invalidate()
        isPaused = true
    }
    
    func start(){
        if(isPaused == false){
            return
        }
        
        isIdle = false
        isPaused = false

        applicationTimer = Timer.scheduledTimer(timeInterval: 120, target: self, selector: #selector(saveCurrentApplicationToMemory), userInfo: nil, repeats: true)
        idleTimer = Timer.scheduledTimer(timeInterval: 10, target: self, selector: #selector(checkForIdle), userInfo: nil, repeats: true)
        
        applicationTimer!.tolerance = 20
        idleTimer!.tolerance = 10
    }
    
    @objc func onSleepReset(){
        lastApplication = nil
        isIdle = true
        NotificationCenter.default.post(name: NSNotification.Name(rawValue: "isIdle"), object: nil, userInfo: ["isidle":isIdle])
    }
    
    @objc func checkForIdle(){
        
        //https://stackoverflow.com/questions/31943951/swift-and-my-idle-timer-implementation-missing-cgeventtype
        let anyInputEventType = CGEventType(rawValue: ~0)!
        
        self.idleTime = CGEventSource.secondsSinceLastEventType(.combinedSessionState, eventType: anyInputEventType)
        
        if(idleTime > 2 * 60){
            print("idle -  ----------")
            if(!isIdle){
                isIdle = true
                saveCurrentApplicationToMemory()
            }
        }
        else{
            if(isIdle){
                isIdle = false
                NotificationCenter.default.post(name: NSNotification.Name(rawValue: "isIdle"), object: nil, userInfo: ["isidle":isIdle])
                saveCurrentApplicationToMemory()
            }
        }
    }
    
    
    @objc func saveCurrentApplicationToMemory(){
        if(isPaused){
            return
        }
        
        if let activeApp = NSWorkspace.shared.frontmostApplication {
        // I've had a problem with a thread being created which makes no apps active, so it crashed on activeApps.first!
        // Now I'm confirming it has a name
            // Get first/only element
            let activeAppName: String
            var title = ""
            if(isIdle){
                activeAppName = "Idle"
            }
            else{
                //https://stackoverflow.com/questions/5292204/macosx-get-foremost-window-title
                activeAppName = activeApp.localizedName!
                let options = CGWindowListOption(arrayLiteral: CGWindowListOption.excludeDesktopElements, CGWindowListOption.optionOnScreenOnly)
                // Starting from Catalina 10.15 CGWindowListCopyWindowInfo does not return meta data
                // kCGWindowName unless the user approves screen recording.
                // more infos here: https://developer.apple.com/videos/play/wwdc2019/701/
                let windowListInfo = CGWindowListCopyWindowInfo(options, kCGNullWindowID)
                let infoList = windowListInfo as NSArray? as? [[String: AnyObject]]
                let flattenedInfoList = infoList.flatMap { $0 }
                
                let activeWindows = flattenedInfoList?.filter { $0["kCGWindowOwnerPID"] as! pid_t == activeApp.processIdentifier }
                
                for window in activeWindows ?? [] {
                    if window["kCGWindowName"] != nil {
                        if window["kCGWindowName"] as! String != "" {
                            title = window["kCGWindowName"] as! String
                            break
                        }
                    }
                }
            }
            
            if(ignorelist.contains(activeAppName)){
                return
            }
            
            if (lastApplication == nil) {
                lastApplication = ActiveApplication(time: Date(), tsStart: Date(), tsEnd: Date(), window: title, process: activeAppName)
            } else {
                // the last app is still running since we last checked. Therefore, we have to extend the active lifetime by increasing .tsEnd
                lastApplication!.tsEnd = Date()
                // I have noticed that there are consecutive db records of the same process - the first record without a window title and the second records a few ms later with a window title (in Chrome, Xcode). The following if clause prevents this.
                if (lastApplication!.process == activeAppName && lastApplication!.window == "") {
                    lastApplication!.window = title
                }
                else if (lastApplication!.process != activeAppName || (lastApplication!.window != title)){
                    // at this point, the last app is no longer active and we can persist it
                    WindowsActivityQueries.saveActiveApplication(app: lastApplication!)
                    // new application which is currently running
                    lastApplication = ActiveApplication(time: Date(), tsStart: Date(), tsEnd: Date(), window: title, process: activeAppName)
                    
                    // other trackers might be intersted when the active application changes
                    NotificationCenter.default.post(name: NSNotification.Name(rawValue: "activeApplicationChange"), object: nil, userInfo: ["activeApplication":activeApp])
                }
            }
        }
    }
    
    // MARK: - Current Application
    //var lastWebsiteCaptureTime: Date = Date()
    //let browserURLandTitleInterval = TimeInterval( 20 ) // seconds
    
    //    typealias currentTab = (name:String, url:String)?
    
    //    func saveTabURLAndTitle(_ activeApplication: String)->currentTab{
    //        //Helper function
    //        func runApplescript(_ applescriptString: String) -> String?{
    //            var error: NSDictionary?
    //            if let scriptObject = NSAppleScript(source: applescriptString) {
    //                if let output: NSAppleEventDescriptor = scriptObject.executeAndReturnError(
    //                    &error) {
    //                        if let URL = output.stringValue {
    //                            return URL // This is the important outcome, the rest don't matter
    //                        }
    //                } else if (error != nil) {
    //                    print("error: \(error)")
    //                }
    //            }
    //            return nil
    //        }
    //        // Only works with Safari or Chrome
    //        switch activeApplication{
    //            // TODO: Took this out since the extension now gets the below data and more!
    ////
    ////        case "Google Chrome":
    ////            let urlReturn = runApplescript("tell application \"Google Chrome\" to return URL of active tab of front window")
    ////            let titleReturn = runApplescript("tell application \"Google Chrome\" to return title of active tab of front window")
    ////
    ////            guard let url = urlReturn else { return nil }
    ////            guard let title = titleReturn else { return nil }
    //////            DataObjectController.sharedInstance.saveCurrentWebsite(title, url: url)
    ////            return (name: title, url:url)
    ////        case "Safari":
    ////            let urlReturn = runApplescript("tell application \"Safari\" to return URL of front document")
    ////            let titleReturn = runApplescript("tell application \"Safari\" to return name of front document")
    ////            guard let url = urlReturn else { return nil }
    ////            guard let title = titleReturn else { return nil }
    ////  //          DataObjectController.sharedInstance.saveCurrentWebsite(title, url: url)
    ////            return (name: title, url:url)
    //        default:
    //            break
    //        }
    //        return nil
    //    }
    
}
