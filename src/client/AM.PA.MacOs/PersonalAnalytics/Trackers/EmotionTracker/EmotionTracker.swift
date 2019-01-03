//
//  EmotionTracker.swift
//  PersonalAnalytics
//
//  Created by Luigi Quaranta on 02/01/2019.
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

class EmotionTracker {

    // MARK: Properties
    var context: NSManagedObjectContext?

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

        let notification = NSUserNotification()
        let notificationCenter = NSUserNotificationCenter.default
        notification.title = "How are you feeling?"
        notification.subtitle = "REMEMBER: push the button on your smartband!"
        notification.soundName = NSUserNotificationDefaultSoundName
        notification.deliveryDate = Date(timeIntervalSinceNow: UserDefaults.standard.value(forKey: "timeInterval") as! TimeInterval)

        // More optional notification settings

        // Identifier: DANGEROUS, the notification didn't work
        // notification.identifier = "Emotion Pop-up notification"

        // Informative text
        // notification.informativeText = "And remember to push the button on your smartband!"

        // Actual notification scheduling
        notificationCenter.scheduleNotification(notification)
    }

}
