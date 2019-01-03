//
//  EmotionPopUpWindowController.swift
//  PersonalAnalytics
//
//  Created by Luigi Quaranta on 03/01/2019.
//

import Cocoa
import Foundation
import UserNotifications
import CoreData

class EmotionPopUpWindowController: NSWindowController {

    // MARK: Properties

    // EmotionTracker instance
    var emotionTracker = EmotionTracker()

    // Buttons
    var valence: NSButton? = nil
    var arousal: NSButton? = nil
    @IBOutlet weak var activityPopupButton: NSPopUpButton!


    //------------------------------------------------------


    // MARK: Utility functions
    func resetButtons() {
        self.valence?.state = NSControl.StateValue.off
        self.arousal?.state = NSControl.StateValue.off
        self.activityPopupButton.selectItem(at: -1)
    }

    // MARK: Lifecycle events
    override func windowDidLoad() {
        super.windowDidLoad()
        resetButtons()
    }

    //------------------------------------------------------

    // MARK: UI behaviour

    // Valence/Arousal radio buttons
    @IBAction func valenceRadioButtonsClicked(_ sender: NSButton) {
        self.valence = sender
    }
    @IBAction func arousalRadioButtonsClicked(_ sender: NSButton) {
        self.arousal = sender
    }

    // Repeat button
    @IBAction func repeatButtonClicked(_ sender: Any) {

        if let valenceValue = self.valence?.identifier?.rawValue,
            let arousalValue = self.arousal?.identifier?.rawValue {

            let timestamp = NSDate()
            let activity = self.activityPopupButton.titleOfSelectedItem
            let valence = NSNumber(value: Int16(valenceValue)!)
            let arousal = NSNumber(value: Int16(arousalValue)!)

            let questionnaire = Questionnaire(timestamp: timestamp, activity: activity ?? "nil", valence: valence, arousal: arousal)

            emotionTracker.storeQuestionnaire(questionnaire: questionnaire)
        }

        // Reset button state
        resetButtons()

        // Request a new notification
        emotionTracker.scheduleNotification()

        // Close the window
        self.close()
    }



//    @IBAction func exportToCsvClicked(_ sender: Any) {
//
//        // csv string to be saved
//        let csv = csv_string()
//
//        // Open SavePanel
//        let savePanel = NSSavePanel()
//        savePanel.title = "Export data to csv"
//        savePanel.message = NSLocalizedString("Insert a name", tableName: "MainMenu", comment:"How to call the output file when exporting to .csv")
//
//        if (savePanel.runModal() == NSApplication.ModalResponse.OK) {
//            let result = savePanel.url
//
//            if (result != nil) {
//
//                print("Saving to: ", result!.path)
//
//                do {
//                    try csv.write(to: result!, atomically: true, encoding: String.Encoding.utf8)
//                } catch {
//                    // failed to write file – bad permissions, bad filename, missing permissions, or more likely it can't be converted to the encoding
//                    print("Something went wrong trying to save the file.")
//                }
//            }
//        } else {
//            return // User clicked cancel
//        }
//
//        print("Saved.")
//
//    }


//    // MARK: Export utilities
//
//    func csv_string() -> String {
//
//        let dateFormatter = DateFormatter()
//        dateFormatter.dateStyle = .short
//        dateFormatter.timeStyle = .short
//        dateFormatter.locale = .current
//
//        var csv = ""
//
//        let emotionLogFetch = NSFetchRequest<NSFetchRequestResult>(entityName: "EmotionalState")
//        do {
//            let fetchedEmotionLogs = try context!.fetch(emotionLogFetch) as! [EmotionalState]
//            for emotionLog in fetchedEmotionLogs {
//
//                csv.append(dateFormatter.string(from: emotionLog.timestamp! as Date))
//                csv.append(";")
//                csv.append(emotionLog.activity ?? "nil")
//                csv.append(";")
//                csv.append(emotionLog.valence?.stringValue ?? "nil")
//                csv.append(";")
//                csv.append(emotionLog.arousal?.stringValue ?? "nil")
//                csv.append("\n")
//
//            }
//        } catch {
//            fatalError("Failed to fetch EmotionLogs: \(error)")
//        }
//
//        return csv
//    }

}
