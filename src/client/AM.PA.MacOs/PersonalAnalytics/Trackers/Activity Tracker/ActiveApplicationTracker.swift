//
//  ActiveApplicationTracker.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-29.
//

import Foundation
import CoreGraphics

class ActiveApplicationTracker: Tracker{
    let type: String = "ActiveApplication"

    
    var applications: [ActiveApplication] = []
    let maxAppCount = 100
    var viz: [Visualization]
    let defaults = UserDefaults.standard
    var applicationTimer: Timer?
    var idleTime: CFTimeInterval = 0
    var idleTimer: Timer?
    var isIdle: Bool
    var unsafeChars : CharacterSet
    let ignorelist = ["loginwindow", "com.apple.WebKit.WebContent", "CoreServicesUIAgent", "System Events","SecurityAgent", "PersonalAnalytics Web Content", "ScreenSaverEngine"]
    var isPaused = false

  
    required init(){
        viz = []
        isIdle = false
        
        unsafeChars = NSCharacterSet.alphanumerics
        unsafeChars.insert(charactersIn: "<>?';:\",.][{}\\|+=-_)(*&^%$#@!~`")
        unsafeChars = unsafeChars.inverted
        
        applicationTimer = Timer.scheduledTimer(timeInterval: 60, target: self, selector: #selector(saveCurrentApplicationToMemory), userInfo: nil, repeats: true)
        idleTimer = Timer.scheduledTimer(timeInterval: 10, target: self, selector: #selector(checkForIdle), userInfo: nil, repeats: true)
        
        applicationTimer!.tolerance = 10
        idleTimer!.tolerance = 5
        
        NSWorkspace.shared.notificationCenter.addObserver(self,
                                                            selector: #selector(saveCurrentApplicationToMemory),
                                                            name: NSWorkspace.didActivateApplicationNotification,
                                                            object: nil)
        
        NSWorkspace.shared.notificationCenter.addObserver(self,
                                                            selector: #selector(onSleepReset),
                                                            name: NSWorkspace.willSleepNotification,
                                                            object: nil)
        
        
        do{
            viz.append(try DayProgamsUsedPieChart())
        }
        catch{
            print(error)
        }
        
        do{
            viz.append(try DayMostFocusedProgram())
        }
        catch{
            print(error)
        }
        
        do{
            viz.append(try DayFragmentationTimeline())
        }
        catch{
            print(error)
        }
        
        do{
            viz.append(try WeekProgramsUsedTable())
        }
        catch{
            print(error)
        }
        
        do{
            viz.append(try DayTimeSpentVisualization())
        }
        catch{
            print(error)
        }

    }
    
    func pause(){
        applicationTimer?.invalidate()
        idleTimer?.invalidate()
        isPaused = true
        DataObjectController.sharedInstance.acceptingWebsites = false
    }
    
    func resume(){
        if(isPaused == false){
            return
        }
        
        isIdle = false
        isPaused = false
        DataObjectController.sharedInstance.acceptingWebsites = true


        applicationTimer = Timer.scheduledTimer(timeInterval: 120, target: self, selector: #selector(saveCurrentApplicationToMemory), userInfo: nil, repeats: true)
        idleTimer = Timer.scheduledTimer(timeInterval: 10, target: self, selector: #selector(checkForIdle), userInfo: nil, repeats: true)
        
        applicationTimer!.tolerance = 20
        idleTimer!.tolerance = 10
    }
    
    @objc func onSleepReset(){
        applications = []
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
        else if(idleTime > 30 * 60){
            NotificationCenter.default.post(name: NSNotification.Name(rawValue: "isIdle"), object: nil, userInfo: ["isidle":isIdle])
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
        

        
        func resetApplicationList(){
            if(!applications.isEmpty){
                let previousApp = applications.popLast()!
                applications = [previousApp]
            }
        }
        
        func runApplescript(_ applescriptString: String) -> String{
             var error: NSDictionary?
             if let scriptObject = NSAppleScript(source: applescriptString) {
                 if let output: NSAppleEventDescriptor = scriptObject.executeAndReturnError(&error) {
                     if let URL = output.stringValue {
                        return URL // This is the important outcome, the rest don't matter
                    }
                 }
             }
            print(error!)
            return ""
         }
         
        let thisComputer = NSWorkspace.shared
        let activeApps = thisComputer.runningApplications.filter { $0.isActive }
        // I've had a problem with a thread being created which makes no apps active, so it crashed on activeApps.first!
        // Now I'm confirming it has a name
        if let activeApp = activeApps.first {
            // Get first/only element
            let activeAppName: String
            var title = ""
            if(isIdle){
                activeAppName = "Idle"
            }
            else{
                //https://stackoverflow.com/questions/5292204/macosx-get-foremost-window-title
                activeAppName = activeApp.localizedName! //runtime error if nil
                title = "global frontApp, frontAppName, windowTitle"
                title += "\nset windowTitle to \"\""
                title += "\ntell application \"System Events\""
                title += "\n    set frontApp to first application process whose frontmost is true"
                title += "\n    set frontAppName to name of frontApp"
                title += "\n    set windowTitle to \"no window\""
                title += "\n    tell process frontAppName"
                title += "\n        if exists (1st window whose value of attribute \"AXMain\" is true) then"
                title += "\n            tell (1st window whose value of attribute \"AXMain\" is true)"
                title += "\n                set windowTitle to value of attribute \"AXTitle\""
                title += "\n            end tell"
                title += "\n        end if"
                title += "\n    end tell"
                title += "\nend tell"
                title += "\nreturn windowTitle"
                
                title = runApplescript(title)
                
                title = title.trimmingCharacters(in: unsafeChars)
                if(title == "no window"){
                    title = ""
                }
                if(activeAppName == "PersonalAnalytics"){
                    title = "PersonalAnalytics"
                }
            }
            
            if(ignorelist.contains(activeAppName)){
                return
            }
            
            if applications.isEmpty {
                applications.append(DataObjectController.sharedInstance.newActiveApplication(activeAppName, title: title))
            } else {
                let previousApp = applications.popLast()! // only gets here when it's not empty
                //updated enddate
                
                previousApp.endTime = Date().timeIntervalSince1970
                
                applications.append(previousApp)
                if (previousApp.name != activeAppName || previousApp.title != title){ // if no longer active add new app
                    if applications.count > maxAppCount {
                        resetApplicationList()
                    }
                    applications.append(DataObjectController.sharedInstance.newActiveApplication(activeAppName, title: title))
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
