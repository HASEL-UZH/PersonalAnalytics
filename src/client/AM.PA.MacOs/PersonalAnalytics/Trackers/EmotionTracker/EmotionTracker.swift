//
//  EmotionTracker.swift
//  PersonalAnalytics
//
//  Created by Luigi Quaranta on 2019-01-02.
//

import Cocoa
import Foundation
import CoreData

struct Questionnaire {
    var timestamp: NSDate
    var activity: String
    var valence: NSNumber
    var arousal: NSNumber
}

class EmotionTracker: ITracker {

    // MARK: Properties
    let emotionPopUpController = EmotionPopUpWindowController(windowNibName: NSNib.Name(rawValue: "EmotionPopUp"))
    let notificationCenter = NSUserNotificationCenter.default
    var notificationSet = Set<NSUserNotification>()

    // Properties for protocol conformity
    var name: String = "EmotionTracker"
    var isRunning: Bool = false


    // MARK: Initializer
    init() {
        
        // Set default time interval between notificaitons
        var minutes = 60
        minutes *= 60
        UserDefaults.standard.set(minutes, forKey: "timeInterval")

        // Start the tracker
        start()
    }


    // MARK: ITracker Protocol Conformity Functions
    func start() {
        // Schedule first notification
        scheduleNotification()
        isRunning = true
    }

    func stop() {
        // Cancel scheduled notification
        for notification in notificationSet {
            notificationCenter.removeScheduledNotification(notification)
        }
        isRunning = false
    }

    // Not yet implemented
    func createDatabaseTablesIfNotExist() {}
    func updateDatabaseTables(version: Int) {}


    // MARK: Notification scheduling and management
    func scheduleNotification(minutesSinceNow: Int? = nil) {

        let notification = NSUserNotification()

        // Notification properties
        notification.identifier = String(NSDate().timeIntervalSince1970)
        notification.title = "How are you feeling?"
        notification.subtitle = "Click here to open the pop-up!"
        notification.soundName = NSUserNotificationDefaultSoundName
        let timeIntervalSinceNow: Int = (minutesSinceNow ?? (UserDefaults.standard.value(forKey: "timeInterval") as! Int))
        notification.deliveryDate = Date(timeIntervalSinceNow: TimeInterval(exactly: timeIntervalSinceNow)!)

        // Notification buttons
        notification.hasActionButton = true
        notification.otherButtonTitle = "Dismiss"
        notification.actionButtonTitle = "Postpone"
        var actions = [NSUserNotificationAction]()
        let action1 = NSUserNotificationAction(identifier: "1h", title: "1 hour")
        let action2 = NSUserNotificationAction(identifier: "2h", title: "2 hours")
        actions.append(action1)
        actions.append(action2)
        notification.setValue(true, forKey: "_alwaysShowAlternateActionMenu") // WARNING, private API
        notification.additionalActions = actions

        // Actual notification scheduling
        notificationCenter.scheduleNotification(notification)
        notificationSet.insert(notification)

        // Debug prints
        print("Time to wait for next notification:", TimeInterval(exactly: timeIntervalSinceNow)!)
        print("NotificationID: " + (notification.identifier ?? "noID"))

    }

    func manageNotification(notification: NSUserNotification) {

        if notificationSet.contains(notification) {

            if let choosen = notification.additionalActivationAction, let actionIdentifier = choosen.identifier {

                // If the notification is postponed...
                switch actionIdentifier {
                case "1h":
                    scheduleNotification(minutesSinceNow: 60*60)
                    print("Notification postponed. It will display 1 hour from now!")
                case "2h":
                    scheduleNotification(minutesSinceNow: 120*60)
                    print("Notification postponed. It will display 2 hours from now!")
                default:
                    print("Something went wrong: UserNotification additionalActivationAction not recognized")
                }

            } else {
                // Show EmotionPopUp and remove the notification
                emotionPopUpController.showEmotionPopUp(self)
                notificationCenter.removeDeliveredNotification(notification)
            }
        
        }
    }

}
