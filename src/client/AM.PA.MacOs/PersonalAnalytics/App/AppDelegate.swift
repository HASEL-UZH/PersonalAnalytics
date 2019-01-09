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

import Cocoa
import Foundation
import Quartz


@NSApplicationMain
class AppDelegate: NSObject, NSApplicationDelegate, NSUserNotificationCenterDelegate {
    
    
    // MARK: - Constants
    let summaryEnabled = "Toggle Survey"

    let maxAppCount = 100
    var notificationTimer: Timer?
    var pauseItem : NSMenuItem?
    var isPaused: Bool = false
    
    // MARK: - Core Data stack
    
    lazy var applicationDocumentsDirectory: URL = {
        // The directory the application uses to store the Core Data store file. This code uses a directory named "PersonalAnalytics" in the user's Application Support directory.
        let urls = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)
        let appSupportURL = urls[urls.count - 1]
        return appSupportURL.appendingPathComponent("PersonalAnalytics")
    }()
    
    lazy var managedObjectModel: NSManagedObjectModel = {
        // The managed object model for the application. This property is not optional. It is a fatal error for the application not to be able to find and load its model.
        let modelURL = Bundle.main.url(forResource: "PersonalAnalytics", withExtension: "momd")!
        return NSManagedObjectModel(contentsOf: modelURL)!
    }()
    
    lazy var persistentStoreCoordinator: NSPersistentStoreCoordinator = {
        // The persistent store coordinator for the application. This implementation creates and returns a coordinator, having added the store for the application to it. (The directory for the store is created, if necessary.) This property is optional since there are legitimate error conditions that could cause the creation of the store to fail.
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
        
        // Create the coordinator and store
        var coordinator: NSPersistentStoreCoordinator? = nil
        if failError == nil {
            coordinator = NSPersistentStoreCoordinator(managedObjectModel: self.managedObjectModel)
            print(self.applicationDocumentsDirectory)
            let url = self.applicationDocumentsDirectory.appendingPathComponent("PersonalAnalytics.dat")
            do {
                let mOptions = [NSMigratePersistentStoresAutomaticallyOption: true,
                                NSInferMappingModelAutomaticallyOption: true]
                try coordinator!.addPersistentStore(ofType: NSSQLiteStoreType, configurationName: nil, at: url, options: mOptions)
            } catch {
                failError = error as NSError
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
        } else {
            return coordinator!
        }
    }()
    
    lazy var managedObjectContext: NSManagedObjectContext = {
        // Returns the managed object context for the application (which is already bound to the persistent store coordinator for the application.) This property is optional since there are legitimate error conditions that could cause the creation of the context to fail.
        let coordinator = self.persistentStoreCoordinator
        var managedObjectContext = NSManagedObjectContext(concurrencyType: .privateQueueConcurrencyType)
        managedObjectContext.persistentStoreCoordinator = coordinator
        return managedObjectContext
    }()
    
    
    // MARK: -- Menu Bar Info
    let popover = NSPopover()
    let statusItem = NSStatusBar.system.statusItem(withLength: -2)
    let menu = NSMenu()
    let defaults = UserDefaults.standard
    let defaultsController = NSUserDefaultsController.shared
    let preferencesController = PreferencesWindowController(windowNibName: NSNib.Name(rawValue: "PreferencesWindow"))
    let retrospectiveController = RetrospectiveWindowController(windowNibName:NSNib.Name(rawValue: "RetrospectiveWindow"))
    let emotionPopUpController = EmotionPopUpWindowController(windowNibName: NSNib.Name(rawValue: "EmotionPopUp"))

    // MARK: - Variables
    var eventMonitor: AnyObject? = nil
    var menuClickMonitor: AnyObject?
    var notificationListenersToRemove = [AnyObject]()
    var alertWaitsUntilClicked = false
    var api : PAHttpServer? = nil
    var keyboard: KeystrokeController?
    var mouse: MouseActionController?

    
    
    // MARK: - App Functions
    @objc func quit(){
        NSApplication.shared.terminate(self)
    }

    // MARK: - Setup
    func setUpMenuBar(){
        statusItem.menu = menu
        defaults.register(defaults: [
            AppConstants.summaryStateKey: 0,
            AppConstants.notificationsPersistKey: true,

            // ACTIVATE/DEACTIVATE EMOTION POP-UP USING THIS SETTING
            AppConstants.emotionPopUpActivateKey: true])
        
        let preferencesItem = NSMenuItem(title: "Preferences...", action: #selector(AppDelegate.showPreferences(_:)), keyEquivalent: "P")
        
        let retrospectiveItem = NSMenuItem(title: "Show Retrospective", action: #selector(AppDelegate.showRetrospective(_:)), keyEquivalent: "R")
        
        let toggleSummaryItem = NSMenuItem(title: summaryEnabled, action: #selector(AppDelegate.toggleSummary(_:)), keyEquivalent: "A")
        toggleSummaryItem.bind(NSBindingName(rawValue: "state"), to: defaultsController , withKeyPath: "values.\(AppConstants.summaryStateKey)", options: nil)
        // Grabbed this from here: https://github.com/producthunt/producthunt-osx/blob/ab3a0c42cf680a5b0231b3c99a76445cce9abb94/Source/Actions/PHOpenSettingsMenuAction.swift
        let delegate = NSApplication.shared.delegate as! AppDelegate
    //    let leaveReminderNotificationUntilClickedItem = NSMenuItem(title: "Notification Stays", action: Selector("togglePersistantUserNotifications:"), keyEquivalent: "P")
     //   leaveReminderNotificationUntilClickedItem.bind("state", to: defaultsController, withKeyPath: "values.\(AppConstants.notificationsPersistKey)", options: nil)
        
        pauseItem = NSMenuItem(title: "Pause Trackers", action: #selector(delegate.togglePause), keyEquivalent: "u")
        
        //menu.addItem(toggleSummaryItem)
        menu.addItem(retrospectiveItem)
        
        menu.addItem(NSMenuItem.separator())
        menu.addItem(pauseItem!)
        
        
        
        menu.addItem(NSMenuItem.separator())
        menu.addItem(preferencesItem)

        //menu.addItem(flowlightItem)
        //menu.addItem(leaveReminderNotificationUntilClickedItem)
        menu.addItem(NSMenuItem.separator())
        
        // It doesn't seem to work if I change the selector, so I'm leaving it for now (working is better for now than slowing down)
        menu.addItem(NSMenuItem(title: "Quit", action: #selector(delegate.quit), keyEquivalent: "q"))

        menu.addItem(NSMenuItem.separator())

        // If the Emotion pop-up function is activated, adds the Emotion Pop-up menu-item to the menu
        if UserDefaults.standard.value(forKey: AppConstants.emotionPopUpActivateKey) as! Bool {
            let emotionPopUpItem = NSMenuItem(title: "Emotion Pop-up", action: #selector(AppDelegate.showEmotionPopUp(_:)), keyEquivalent: "E")
            menu.addItem(emotionPopUpItem)
        }
        
        // Setting up the summary popup
        statusItem.image = NSImage(named: NSImage.Name(rawValue: "StatusBarButtonImage"))
        setUpSummaryView()
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
    
    let notificationQueue = OperationQueue.main // TODO:  this to a side queue?
    
    func addNotificationListeners(){
        let notificationCenter = NotificationCenter.default
        let closeSummaryObserver = notificationCenter.addObserver(forName: NSNotification.Name(rawValue: AppConstants.summarySubmittedNotification), object: nil, queue: notificationQueue) {
            (summaryClosedNotification) -> Void in
            self.resetSummaryView()
        }
        notificationListenersToRemove.append(closeSummaryObserver)
        
        //        let notificationObserver =
        //        notificationCenter.addObserverForName(nil, object: nil, queue: notificationQueue) { print($0) }
        //        notificationListenersToRemove.append(notificationObserver)
    }
    
    // MARK: - Preferences Management
    func setUpPreferencesView(){
        preferencesController.window?.contentViewController = PreferencesViewController(nibName: NSNib.Name(rawValue: "PreferencesView"), bundle: nil)
    }
    
    func setUpRetrospective(){
        retrospectiveController.window?.contentViewController = RetrospectiveViewController(nibName: NSNib.Name(rawValue: "RetrospectiveView"), bundle: nil)
    }
    
    var firstTime = true
    
    @objc func showPreferences(_ sender:AnyObject){
        preferencesController.showWindow(nil)
        NSApp.activate(ignoringOtherApps: true)
        preferencesController.window?.makeKeyAndOrderFront(self)
        
        if(firstTime){
            preferencesController.repositionWindow()
            firstTime = false
        }
        //flowlightController?.setGreen()
        preferencesController.window?.level = NSWindow.Level(rawValue: Int(CGWindowLevelForKey(.floatingWindow)))
    }
    
    @objc func showRetrospective(_ sender: AnyObject){
        DataObjectController.sharedInstance.saveContext()
        retrospectiveController.showWindow(nil)
        NSApp.activate(ignoringOtherApps: true)

        retrospectiveController.window?.makeKeyAndOrderFront(self)
        
    }

    @objc func showEmotionPopUp(_ sender: AnyObject){
        DataObjectController.sharedInstance.saveContext()
        emotionPopUpController.showWindow(nil)
        NSApp.activate(ignoringOtherApps: true)

        emotionPopUpController.window?.makeKeyAndOrderFront(self)

    }
    

    // MARK: - Popover Management
    func setUpSummaryView(){
        popover.contentViewController = SummaryViewController(nibName: NSNib.Name(rawValue: "SummaryView"), bundle: nil)
        popover.behavior = .transient
    }
    
    func resetSummaryView(){
        if(popover.isShown){
            popover.performClose(nil)
        }
        setUpSummaryView()
    }

    
    @objc func toggleSummary(_ sender:AnyObject){
            if let button = statusItem.button {
                popover.show(relativeTo: button.bounds, of: button, preferredEdge: NSRectEdge.minY)
                NSApp.activate(ignoringOtherApps: true)
            }
    }

    
    func userNotificationCenter(_ center: NSUserNotificationCenter, didActivate notification: NSUserNotification) {

        if notification.title == "What are you feeling?" {
            // EMOTION POP-UP ACTIVATION
            // If the notification is postponed...
            if let choosen = notification.additionalActivationAction, let actionIdentifier = choosen.identifier {
                switch actionIdentifier {
                case "5m":
                    emotionPopUpController.emotionTracker.scheduleNotification(minutesSinceNow: 5*60)
                    print("Notification postponed. It will display 5 minutes from now!")
                case "30m":
                    emotionPopUpController.emotionTracker.scheduleNotification(minutesSinceNow: 30*60)
                    print("Notification postponed. It will display 30 minutes from now!")
                case "1h":
                    emotionPopUpController.emotionTracker.scheduleNotification(minutesSinceNow: 60*60)
                    print("Notification postponed. It will display 60 minutes from now!")
                default:
                    print("Something went wrong: UserNotification additionalActivationAction not recognized")
                }
            } else {
                // If the notification is accepted...

                // Shows EmotionPopUp
                self.showEmotionPopUp(self)
                // Removes the notification from the NotificationCenter
                center.removeDeliveredNotification(notification)
            }
        } else {
            // SUMMARY ACTIVATION
            //print("Using delelgate for NSUsernotification")
            self.toggleSummary(notification.self)
        }




    }

    func userNotificationCenter(_ center: NSUserNotificationCenter, shouldPresent notification: NSUserNotification) -> Bool {
        return true
    }

    //https://stackoverflow.com/questions/7271528/how-to-nslog-into-a-file
    func redirectLogToDocuments() {
        let pathForErrors = self.applicationDocumentsDirectory.path.appendingFormat("/errors.txt")
        //freopen(pathForLog.cString(using: String.Encoding.ascii)!, "a+", stdout)
        freopen(pathForErrors.cString(using: String.Encoding.ascii)!, "a+", stderr)
    }
    
    
    

    
    // MARK: - Setup, teardown of application including timers
    func applicationDidFinishLaunching(_ aNotification: Notification) {

        if !AXIsProcessTrusted(){
            launchPermissionPanel()
            launchPermissionExplanationAlert()
        }
        
        TrackerManager.shared.register(tracker: UserInputTracker())
        TrackerManager.shared.register(tracker: ActiveApplicationTracker())
        TrackerManager.shared.register(tracker: TaskProductivityTracker())
        
        
        redirectLogToDocuments()
        setUpMenuBar()
        
        
        let center = NSUserNotificationCenter.default
        center.delegate = self
        
        
        addNotificationListeners()
        // Start local server so chrome extension can send data to it
        api = PAHttpServer(coreDataController: DataObjectController.sharedInstance)
        api!.startServer()

    }
    
    func launchPermissionExplanationAlert(){
        let alert = NSAlert()
        alert.showsHelp = true
        alert.helpAnchor = NSHelpManager.AnchorName(rawValue: "https://support.apple.com/en-us/HT201642")
        alert.messageText = "The accessibility window just opened, to keep track of events this app needs to access background events.\n" +
        "Please\n1. Click the add button at the bottom of the accessibility page. \n2. Acessibility's app list, add PersonalAnalytics"
        alert.runModal()
    }
    
    func launchPermissionPanel(){
        var script: String
        let version = ProcessInfo().operatingSystemVersionString as NSString
        if  version.substring(to: 12) == "Version 10.7" || version.substring(to: 12) == "Version 10.8" {
            script = "tell application \"System Preferences\" \n set the current pane to pane id \"com.apple.preference.universalaccess\" \n activate \n end tell"
        } else {
            script = "tell application \"System Preferences\" \n reveal anchor \"Privacy_Accessibility\" of pane id \"com.apple.preference.security\" \n activate \n end tell"
        }
        let scriptObject = NSAppleScript(source: script)
        scriptObject?.executeAndReturnError(nil)
    }
    
    

    
    func applicationWillTerminate(_ aNotification: Notification) {
        // Remove listeners so notification center doesn't have dangling references
        print("Terminating app")
        NotificationCenter.default.removeObserver(self)
        //TODO: Remove tracker observers
        if let monitor = eventMonitor{
            NSEvent.removeMonitor(monitor)
        }
        DataObjectController.sharedInstance.saveContext()
        // This cannot be set to defer above because it will ask for connection, but before you can click
        // it will see the server is not connected and close it.
        api!.stopServer()
    }

    
    func windowWillReturnUndoManager(_ window: NSWindow) -> UndoManager? {
        // Returns the NSUndoManager for the application. In this case, the manager returned is that of the managed object context for the application.
        return managedObjectContext.undoManager
    }
    
    func applicationShouldTerminate(_ sender: NSApplication) -> NSApplication.TerminateReply {
        // Save changes in the application's managed object context before the application terminates.
        
        if !managedObjectContext.commitEditing() {
            NSLog("\(NSStringFromClass(type(of: self))) unable to commit editing to terminate")
            return .terminateCancel
        }
        
        if !managedObjectContext.hasChanges {
            return .terminateNow
        }
        
        do {
            try managedObjectContext.save()
        } catch {
            let nserror = error as NSError
            // Customize this code block to include application-specific recovery steps.
            let result = sender.presentError(nserror)
            if (result) {
                return .terminateCancel
            }
            
            let question = NSLocalizedString("Could not save changes while quitting. Quit anyway?", comment: "Quit without saves error question message")
            let info = NSLocalizedString("Quitting now will lose any changes you have made since the last successful save", comment: "Quit without saves error question info");
            let quitButton = NSLocalizedString("Quit anyway", comment: "Quit anyway button title")
            let cancelButton = NSLocalizedString("Cancel", comment: "Cancel button title")
            let alert = NSAlert()
            alert.messageText = question
            alert.informativeText = info
            alert.addButton(withTitle: quitButton)
            alert.addButton(withTitle: cancelButton)
            
            let answer = alert.runModal()
            if answer == NSApplication.ModalResponse.alertFirstButtonReturn {
                return .terminateCancel
            }
        }
        // If we got here, it is time to quit.
        return .terminateNow
    }
    
    
}
