//
//  AppDelegate.swift
//  PersonalAnalytics
//
//  Created by Jonathan Stiansen on 2015-09-20.
//

/*** Wish List
Finish default schema
Finish schema fetching
Log all keys
- filter alphabet keys out
Save active application
Delete live applications periodically
Save all collected schemes to core data
- Save all data to csv
- Add menu item for if notification should stay in the corner till dismissed
- Finish finished core data implementation and models
Set popup to appear once every 90 minutes
Change picture on notification
- Add action to notification click: http://stackoverflow.com/questions/11676017/nsusernotification-not-showing-action-button/12012934#12012934
- Add persistentNotifcation and uncomment item
- Make it change depending on the users productivity slider (to manual instertion of tasks?)
- Finish correct notifications
- Consider integrating with Dropbox through the dropbox SDK
- optional - Don't ask for summary untill the person stops typing for 20 seconds
*/

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
    
    
    // MARK: - Menu Bar Info
    let statusItem = NSStatusBar.system.statusItem(withLength: -2)
    let menu = NSMenu()
    let defaults = UserDefaults.standard
    let defaultsController = NSUserDefaultsController.shared
    let preferencesController = PreferencesWindowController(windowNibName: NSNib.Name(rawValue: "PreferencesWindow"))
    let retrospectiveController = RetrospectiveWindowController(windowNibName:NSNib.Name(rawValue: "RetrospectiveWindow"))

    
    // MARK: - Variables
    var api : PAHttpServer? = nil
    var pauseItem : NSMenuItem?
    var isPaused: Bool = false
    
    
    // MARK: - App Functions
    @objc func quit(){
        NSApplication.shared.terminate(self)
    }

    // MARK: - Setup
    func setUpMenuBar(){
        statusItem.menu = menu
        defaults.register(defaults: [
            AppConstants.summaryStateKey: 0,
            AppConstants.notificationsPersistKey: true])
        
        // Grabbed this from here: https://github.com/producthunt/producthunt-osx/blob/ab3a0c42cf680a5b0231b3c99a76445cce9abb94/Source/Actions/PHOpenSettingsMenuAction.swift
        let delegate = NSApplication.shared.delegate as! AppDelegate
        
        let retrospectiveItem = NSMenuItem(title: "Show Retrospective", action: #selector(AppDelegate.showRetrospective(_:)), keyEquivalent: "R")
        pauseItem = NSMenuItem(title: "Pause Trackers", action: #selector(delegate.togglePause), keyEquivalent: "u")
        let preferencesItem = NSMenuItem(title: "Preferences...", action: #selector(AppDelegate.showPreferences(_:)), keyEquivalent: "P")
        let openDataItem = NSMenuItem(title: "Open Data Folder...", action: #selector(AppDelegate.openDataFolder(_:)), keyEquivalent: "O")
        let checkUpdatesItem = NSMenuItem(title: "Check for Updates...", action: #selector(SUUpdater.checkForUpdates(_:)), keyEquivalent: "U")
        // let toggleSummaryItem = NSMenuItem(title: "Toggle Survey", action: #selector(toggleSummary), keyEquivalent: "A")
        // toggleSummaryItem.bind(NSBindingName(rawValue: "state"), to: defaultsController , withKeyPath: "values.\(AppConstants.summaryStateKey)", options: nil)
        // let leaveReminderNotificationUntilClickedItem = NSMenuItem(title: "Notification Stays", action: Selector("togglePersistantUserNotifications:"), keyEquivalent: "P")
        // leaveReminderNotificationUntilClickedItem.bind("state", to: defaultsController, withKeyPath: "values.\(AppConstants.notificationsPersistKey)", options: nil)
        
        menu.addItem(retrospectiveItem)
        menu.addItem(NSMenuItem.separator())
        menu.addItem(pauseItem!)
        menu.addItem(NSMenuItem.separator())
        menu.addItem(openDataItem)
        menu.addItem(preferencesItem)
        menu.addItem(NSMenuItem.separator())
        menu.addItem(checkUpdatesItem)
        menu.addItem(NSMenuItem.separator())
        // It doesn't seem to work if I change the selector, so I'm leaving it for now (working is better for now than slowing down)
        menu.addItem(NSMenuItem(title: "Quit", action: #selector(delegate.quit), keyEquivalent: "q"))

        // Setting up the summary popup
        statusItem.image = NSImage(named: NSImage.Name(rawValue: Environment.statusBarIcon))
        setUpPreferencesView()
        setUpRetrospective()
    }

    @objc func togglePause(){
        if(isPaused){
            TrackerManager.shared.resume()
            pauseItem!.title = "Pause Trackers"
            isPaused = false
        }
        else{
            TrackerManager.shared.pause()
            pauseItem!.title = "Resume Trackers"
            isPaused = true
        }
    }
        
    
    // MARK: - Preferences Management
    func setUpPreferencesView(){
        preferencesController.window?.contentViewController = PreferencesViewController(nibName: NSNib.Name(rawValue: "PreferencesView"), bundle: nil)
    }
    
    func setUpRetrospective(){
        retrospectiveController.window?.contentViewController = RetrospectiveViewController(nibName: NSNib.Name(rawValue: "RetrospectiveView"), bundle: nil)
    }
    
    func showPreferences() {
        preferencesController.showWindow(nil)
        NSApp.activate(ignoringOtherApps: true)
        preferencesController.window?.makeKeyAndOrderFront(self)
        preferencesController.window?.level = NSWindow.Level(rawValue: Int(CGWindowLevelForKey(.floatingWindow)))
    }
        
    @objc func showPreferences(_ sender:AnyObject){
        showPreferences()
    }
    
    @objc func showRetrospective(_ sender: AnyObject){
        retrospectiveController.showWindow(nil)
        NSApp.activate(ignoringOtherApps: true)

        retrospectiveController.window?.makeKeyAndOrderFront(self)
    }
    
    @objc func openDataFolder(_ sender: AnyObject) {
        NSWorkspace.shared.openFile(applicationDocumentsDirectory.path)
    }
    
    func toggleSummary(){
        let viewController = SummaryViewController(nibName: NSNib.Name(rawValue: "SummaryView"), bundle: nil)
        viewController.showSummaryPopup()
    }

    
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
        showPreferences()
        preferencesController.repositionWindow()
        
        createApplicationDocumentsDirectoryIfMissing()
        
        TrackerManager.shared.register(tracker: UserInputTracker())
        TrackerManager.shared.register(tracker: WindowsActivityTracker())
        TrackerManager.shared.register(tracker: ResourceActivityTracker())
        //TrackerManager.shared.register(tracker: UserEfficiencyTracker())
        //TrackerManager.shared.register(tracker: EmotionTracker())
        
        
        redirectLogToDocuments()
        setUpMenuBar()
        
        
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
