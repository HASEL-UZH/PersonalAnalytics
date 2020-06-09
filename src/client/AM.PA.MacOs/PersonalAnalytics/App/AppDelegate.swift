//
//  AppDelegate.swift
//  PersonalAnalytics
//
//  Created by Jonathan Stiansen on 2015-09-20.
//


import Foundation
import Sparkle


@NSApplicationMain
class AppDelegate: NSObject, NSApplicationDelegate, NSUserNotificationCenterDelegate {
    
    // MARK: - App Support Directory
    lazy var applicationDocumentsDirectory: URL = {
        // The directory the application uses to store the sqlite .dat file. This code uses a directory named "PersonalAnalytics" in the user's Application Support directory.
        let urls = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)
        let appSupportURL = urls[urls.count - 1]
        return appSupportURL.appendingPathComponent(Environment.appSupportDir)
    }()
    
    
    // MARK: - Menu Bar & Controllers
    let menu = MenuBar()
    let preferencesController = PreferencesWindowController(windowNibName: NSNib.Name(rawValue: "PreferencesWindow"))
    let retrospectiveController = RetrospectiveWindowController(windowNibName:NSNib.Name(rawValue: "RetrospectiveWindow"))
    
    
    // MARK: - Variables
    var api : PAHttpServer? = nil
    var isPaused: Bool = false
    
    
    // MARK: - App Management
    @objc func quit(){
        NSApplication.shared.terminate(self)
    }
    
    @objc func togglePause(){
        if(isPaused){
            TrackerManager.shared.resume()
            menu.showMenuResumed()
            isPaused = false
        }
        else{
            TrackerManager.shared.pause()
            menu.showMenuPaused()
            isPaused = true
        }
    }
    
    
    // MARK: - Preferences Management
    func setUpPreferencesView(){
        preferencesController.window?.contentViewController = PreferencesViewController(nibName: NSNib.Name(rawValue: "PreferencesView"), bundle: nil)
    }
    
    func showPreferencesIfTrackerDisabled() {
        let viewController = PreferencesViewController(nibName: NSNib.Name(rawValue: "PreferencesView"), bundle: nil)
        
        if !viewController.areAllTrackersEnabled() {
            showPreferencesWindow()
        }
    }
    
    @objc func showPreferences(){
        showPreferencesWindow()
    }
    
    func showPreferencesWindow() {
        preferencesController.showWindow(nil)
        NSApp.activate(ignoringOtherApps: true)
        preferencesController.window?.makeKeyAndOrderFront(self)
        preferencesController.window?.level = NSWindow.Level(rawValue: Int(CGWindowLevelForKey(.floatingWindow)))
    }
    
    
    // MARK: - Retrospective & Data Folder Management
    func setUpRetrospective(){
        retrospectiveController.window?.contentViewController = RetrospectiveViewController(nibName: NSNib.Name(rawValue: "RetrospectiveView"), bundle: nil)
    }
    
    @objc func showRetrospective(){
        retrospectiveController.showWindow(nil)
        NSApp.activate(ignoringOtherApps: true)

        retrospectiveController.window?.makeKeyAndOrderFront(self)
    }
    
    @objc func openDataFolder() {
        NSWorkspace.shared.openFile(applicationDocumentsDirectory.path)
    }

    
    // MARK - Utility Functions
    func userNotificationCenter(_ center: NSUserNotificationCenter, didActivate notification: NSUserNotification) {
        TrackerManager.shared.handleTrackerUserNotifications(notification: notification)
    }

    //https://stackoverflow.com/questions/7271528/how-to-nslog-into-a-file
    func redirectLogToDocuments() {
        let pathForErrors = self.applicationDocumentsDirectory.path.appendingFormat(Environment.errorsLogFile)
        //freopen(pathForLog.cString(using: String.Encoding.ascii)!, "a+", stdout)
        freopen(pathForErrors.cString(using: String.Encoding.ascii)!, "a+", stderr)
    }
    
    func createApplicationDocumentsDirectoryIfMissing() {
        let fileManager = FileManager.default
        var failError: NSError? = nil
        var shouldFail = false
        var failureReason = "There was an error creating or loading the application's saved data."
        // Make sure the application files directory is there
        do {
            let properties = try (self.applicationDocumentsDirectory as NSURL).resourceValues(forKeys: [URLResourceKey.isDirectoryKey])
            if !(properties[URLResourceKey.isDirectoryKey]! as AnyObject).boolValue {
                failureReason = "Expected a folder to store application data, found a file \(self.applicationDocumentsDirectory.path)."
                shouldFail = true
            }
        } catch  {
            let nserror = error as NSError
            if nserror.code == NSFileReadNoSuchFileError {
                do {
                    try fileManager.createDirectory(atPath: self.applicationDocumentsDirectory.path, withIntermediateDirectories: true, attributes: nil)
                } catch {
                    failError = nserror
                }
            } else {
                failError = nserror
            }
        }

        if shouldFail || (failError != nil) {
            // Report any error we got.
            var dict = [String: AnyObject]()
            dict[NSLocalizedDescriptionKey] = "Failed to initialize the application's saved data" as AnyObject?
            dict[NSLocalizedFailureReasonErrorKey] = failureReason as AnyObject?
            if failError != nil {
                dict[NSUnderlyingErrorKey] = failError
            }
            let error = NSError(domain: "YOUR_ERROR_DOMAIN", code: 9999, userInfo: dict)
            NSApplication.shared.presentError(error)
            abort()
        }
    }
    
    
    // MARK: - Setup, teardown of application including timers
    func applicationDidFinishLaunching(_ aNotification: Notification) {
        
        // opening the preference window to show which trackers are working and which are missing permission
        showPreferencesIfTrackerDisabled()
        preferencesController.repositionWindow()
        
        createApplicationDocumentsDirectoryIfMissing()
        
        TrackerManager.shared.register(tracker: UserInputTracker())
        TrackerManager.shared.register(tracker: WindowsActivityTracker())
        TrackerManager.shared.register(tracker: ResourceActivityTracker())
        //TrackerManager.shared.register(tracker: UserEfficiencyTracker())
        //TrackerManager.shared.register(tracker: EmotionTracker())
                
        redirectLogToDocuments()
        setUpPreferencesView()
        setUpRetrospective()
        menu.setup()
        
        let center = NSUserNotificationCenter.default
        center.delegate = self
        
        // Start local server
        api = PAHttpServer()
        api!.startServer()
    }
    
    func applicationWillTerminate(_ aNotification: Notification) {
        // Remove listeners so notification center doesn't have dangling references
        print("Terminating app")
        // This cannot be set to defer above because it will ask for connection, but before you can click
        // it will see the server is not connected and close it.
        api!.stopServer()
    }
}
