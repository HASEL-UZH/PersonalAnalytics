//
//  EmotionPopUpViewController.swift
//  PersonalAnalytics
//
//  Created by Luigi Quaranta on 02/01/2019.
//

import Cocoa
import Foundation
import UserNotifications
import CoreData

class EmotionPopUpViewController: NSViewController {

    // ===== //
    // STATE //
    // ===== //

    @IBOutlet weak var window: NSWindow!
    let center = UNUserNotificationCenter.current()
    var preferencesController: NSWindowController?
    var context: NSManagedObjectContext?

    // Variables to be saved with CoreData
    var valence: NSButton? = nil
    var arousal: NSButton? = nil
    var timestamp = NSDate()
    @IBOutlet weak var activityPopupButton: NSPopUpButton!

    // ========= //
    // BEHAVIOUR //
    // ========= //

    override func viewWillAppear() {

        // Initialize a Persistent Container
        let container = NSPersistentContainer(name: "Model")
        container.loadPersistentStores(completionHandler: { (description, error) in
            if let error = error {
                fatalError("Unable to load persistent stores: \(error)")
            }
        })

        // Get an NSManagedObjectContext
        self.context = container.newBackgroundContext()

        // SET DEFAULTS
        // Set default time interval between notificaitons
        UserDefaults.standard.set(10, forKey: "timeInterval")
        // Set default activity
        self.activityPopupButton.selectItem(at: -1)

    }


    func applicationWillTerminate(_ aNotification: Notification) {

    }


    // BUTTONS BEHAVIOUR

    // Valence/Arousal radio buttons
    @IBAction func valenceRadioButtonsClicked(_ sender: NSButton) {
        self.valence = sender
    }

    @IBAction func arousalRadioButtonsClicked(_ sender: NSButton) {
        self.arousal = sender
    }

    // Repeat button
    @IBAction func repeatButtonClicked(_ sender: Any) {

        // Prepare the data for storing
        //let questionnaire = NSEntityDescription.insertNewObject(forEntityName: "EmotionLog", into: self.context!) as! EmotionLog
        self.timestamp = NSDate()
        if let valenceValue = self.valence?.identifier?.rawValue,
            let arousalValue = self.arousal?.identifier?.rawValue {

//            questionnaire.timestamp = self.timestamp as Date
//            questionnaire.activity = self.activityPopupButton.titleOfSelectedItem
//            questionnaire.valence = Int16(valenceValue)!
//            questionnaire.arousal = Int16(arousalValue)!
//            print(questionnaire)

        }

        // Reset the choices
        self.valence?.state = NSControl.StateValue.off
        self.arousal?.state = NSControl.StateValue.off
        self.activityPopupButton.selectItem(at: -1)

        // Request a new notification schedule
        scheduleMainNotification()

        // Hide the application
        NSRunningApplication.current.hide()
    }

    @IBAction func exportToCsvClicked(_ sender: Any) {

        // csv string to be saved
        let csv = csv_string()

        // Open SavePanel
        let savePanel = NSSavePanel()
        savePanel.title = "Export data to csv"
        savePanel.message = NSLocalizedString("Insert a name", tableName: "MainMenu", comment:"How to call the output file when exporting to .csv")

        if (savePanel.runModal() == NSApplication.ModalResponse.OK) {
            let result = savePanel.url

            if (result != nil) {

                print("Saving to: ", result!.path)

                do {
                    try csv.write(to: result!, atomically: true, encoding: String.Encoding.utf8)
                } catch {
                    // failed to write file – bad permissions, bad filename, missing permissions, or more likely it can't be converted to the encoding
                    print("Something went wrong trying to save the file.")
                }
            }
        } else {
            return // User clicked cancel
        }

        print("Saved.")

    }

    // NOTIFICATION SCHEDULING

    // Schedule the notification
    func scheduleMainNotification() {

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
        center.add(request) { (error : Error?) in
            if error != nil {
                print("The main notification could not be added to the user's Notification Center")
            }
        }
    }


    // OTHER AUXILIARY METHODS

    func csv_string() -> String {

        let dateFormatter = DateFormatter()
        dateFormatter.dateStyle = .short
        dateFormatter.timeStyle = .short
        dateFormatter.locale = .current

        var csv = ""

//        let emotionLogFetch = NSFetchRequest<NSFetchRequestResult>(entityName: "EmotionLog")
//        do {
//            let fetchedEmotionLogs = try context!.fetch(emotionLogFetch) as! [EmotionLog]
//            for emotionLog in fetchedEmotionLogs {
//
//                csv.append(dateFormatter.string(from: emotionLog.timestamp!))
//                csv.append(";")
//                csv.append(emotionLog.activity ?? "nil")
//                csv.append(";")
//                csv.append(String(emotionLog.valence))
//                csv.append(";")
//                csv.append(String(emotionLog.arousal))
//                csv.append("\n")
//
//            }
//        } catch {
//            fatalError("Failed to fetch EmotionLogs: \(error)")
//        }

        return csv
    }

}
