//
//  EmotionTracker.swift
//  PersonalAnalytics
//
//  Created by Luigi Quaranta on 02/01/2019.
//

import Cocoa
import Foundation
import UserNotifications
import CoreData

struct Questionnaire {
    var timestamp: NSDate
    var activity: String
    var valence: NSNumber
    var arousal: NSNumber
}

class EmotionTracker {

    // MARK: Properties
    var context: NSManagedObjectContext?
    let notificationCenter = UNUserNotificationCenter.current()

    // MARK: Class initializer
    init() {

        // CoreData initialization

        // Initialize a Persistent Container
        let container = NSPersistentContainer(name: "PersonalAnalytics")
        container.loadPersistentStores(completionHandler: { (description, error) in
            if let error = error {
                fatalError("Unable to load persistent stores: \(error)")
            }
        })

        // Get an NSManagedObjectContext
        self.context = container.newBackgroundContext()

        // Set default settings
        // Default time interval between notificaitons
        UserDefaults.standard.set(10, forKey: "timeInterval")

    }

    // MARK: Persistence utilities
    func storeQuestionnaire(questionnaire: Questionnaire) {

        let emotionalState = NSEntityDescription.insertNewObject(forEntityName: "EmotionalState", into: self.context!) as! EmotionalState

        emotionalState.timestamp = questionnaire.timestamp
        emotionalState.activity = questionnaire.activity
        emotionalState.valence = questionnaire.valence
        emotionalState.arousal = questionnaire.arousal

        print(emotionalState)

    }

    // MARK: Notification scheduling
    func scheduleNotification() {

        // Create the notification's content
        let notificationContent = UNMutableNotificationContent()
        notificationContent.title = "How are you feeling?"
        notificationContent.body = "Tell us something about your emotions!"
        notificationContent.sound = UNNotificationSound.defaultCriticalSound(withAudioVolume: 0.9)

        // Create the time interval trigger for the notification
        let trigger = UNTimeIntervalNotificationTrigger(timeInterval: TimeInterval(UserDefaults.standard.integer(forKey: "timeInterval")), repeats: false)

        // Schedule the notification
        let request = UNNotificationRequest(identifier: "Main notification", content: notificationContent, trigger: trigger)

        // Add the notification to the current User Notification Center
        notificationCenter.add(request) { (error : Error?) in
            if error != nil {
                print("The main notification could not be added to the user's Notification Center")
                print(error.debugDescription)
            }
        }

    }

}
