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
        var minutes = 60
        minutes *= 60
        UserDefaults.standard.set(minutes, forKey: "timeInterval")

    }

    // MARK: Persistence utilities
    func storeQuestionnaire(questionnaire: Questionnaire) {

        let emotionalState = NSEntityDescription.insertNewObject(forEntityName: "EmotionalState", into: self.context!) as! EmotionalState

        emotionalState.timestamp = questionnaire.timestamp
        emotionalState.activity = questionnaire.activity
        emotionalState.valence = questionnaire.valence
        emotionalState.arousal = questionnaire.arousal

        do {
            try self.context?.save()
        } catch let error {
            print("It was not possible to save the last emotional state.")
            print("ERROR DETAILS: \(error)")
        }

        print("Emotional state saved:")
        print(emotionalState)

    }

    // MARK: Notification scheduling
    func scheduleNotification(minutesSinceNow: Int? = nil) {

        let notification = NSUserNotification()
        let notificationCenter = NSUserNotificationCenter.default
        let timeIntervalSinceNow: Int = (minutesSinceNow ?? (UserDefaults.standard.value(forKey: "timeInterval") as! Int))

        notification.title = "What are you feeling?"
        notification.subtitle = "REMEMBER: push the button on your smartband!"
        notification.soundName = NSUserNotificationDefaultSoundName
        notification.deliveryDate = Date(timeIntervalSinceNow: TimeInterval(exactly: timeIntervalSinceNow)!)

        notification.hasActionButton = true
        notification.otherButtonTitle = "Dismiss"
        notification.actionButtonTitle = "Postpone"
        var actions = [NSUserNotificationAction]()
        let action1 = NSUserNotificationAction(identifier: "5m", title: "5 minutes")
        let action2 = NSUserNotificationAction(identifier: "30m", title: "30 minutes")
        let action3 = NSUserNotificationAction(identifier: "1h", title: "1 hour")
        actions.append(action1)
        actions.append(action2)
        actions.append(action3)
        // WARNING, private API
        notification.setValue(true, forKey: "_alwaysShowAlternateActionMenu")
        notification.additionalActions = actions



        print("Time to wait for next notification:", TimeInterval(exactly: timeIntervalSinceNow)!)

        // More optional notification settings

        // Identifier: DANGEROUS, the notification didn't work
        // notification.identifier = "Emotion Pop-up notification"

        // Informative text
        // notification.informativeText = "And remember to push the button on your smartband!"

        // Actual notification scheduling
        notificationCenter.scheduleNotification(notification)
    }

    // MARK: Export collected data to csv
    func exportToCsv(destinationPath: URL) {

        // csv string to be saved
        let csv = csv_string()

        do {
            try csv.write(to: destinationPath, atomically: true, encoding: String.Encoding.utf8)
        } catch {
            print("Something went wrong trying to save the file.")
            // Possible problems: failed to write file – bad permissions, bad filename, missing permissions, or more likely it can't be converted to the encoding
        }

    }

    // TODO: refactoring needed
    func csv_string() -> String {

        let dateFormatter = DateFormatter()
        dateFormatter.dateFormat = "dd/MM/yyyy-HH:mm:ss"
        dateFormatter.locale = .current

        var csv = ""

        let emotionLogFetch = NSFetchRequest<NSFetchRequestResult>(entityName: "EmotionalState")
        do {
            let fetchedEmotionLogs = try context!.fetch(emotionLogFetch) as! [EmotionalState]
            for emotionLog in fetchedEmotionLogs {

                csv.append(dateFormatter.string(from: emotionLog.timestamp! as Date))
                csv.append(";")
                csv.append(emotionLog.activity ?? "nil")
                csv.append(";")
                csv.append(emotionLog.valence?.stringValue ?? "nil")
                csv.append(";")
                csv.append(emotionLog.arousal?.stringValue ?? "nil")
                csv.append("\n")

            }
        } catch {
            fatalError("Failed to fetch EmotionLogs: \(error)")
        }

        return csv
    }

}