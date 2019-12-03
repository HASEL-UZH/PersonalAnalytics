//
//  EmotionPopUpWindowController.swift
//  PersonalAnalytics
//
//  Created by Luigi Quaranta on 2019-01-03.
//

import Cocoa
import Foundation
import CoreData

class EmotionPopUpWindowController: NSWindowController {

    // MARK: Properties

    // Buttons
    var valence: NSButton? = nil
    var arousal: NSButton? = nil
    @IBOutlet weak var activityPopupButton: NSPopUpButton!

    // Validation labels
    @IBOutlet weak var activityValidationLabel: NSTextField!
    @IBOutlet weak var valenceValidationLabel: NSTextField!
    @IBOutlet weak var arousalValidationLabel: NSTextField!


    // MARK: Utility functions
    func resetForm() {
        // Reset buttons
        self.valence?.state = NSControl.StateValue.off
        self.valence = nil
        self.arousal?.state = NSControl.StateValue.off
        self.arousal = nil
        self.activityPopupButton.selectItem(at: -1)

        // Reset validation labels
        self.activityValidationLabel.isHidden = true
        self.valenceValidationLabel.isHidden = true
        self.arousalValidationLabel.isHidden = true

    }

    @objc func showEmotionPopUp(_ sender: AnyObject) {
        self.showWindow(nil)
        NSApp.activate(ignoringOtherApps: true)

        self.window?.makeKeyAndOrderFront(self)
    }

    // MARK: Lifecycle events
    override func windowDidLoad() {
        super.windowDidLoad()
        resetForm()
    }


    // MARK: UI behaviour

    // Valence/Arousal radio buttons
    @IBAction func valenceRadioButtonsClicked(_ sender: NSButton) {
        self.valence = sender
    }
    @IBAction func arousalRadioButtonsClicked(_ sender: NSButton) {
        self.arousal = sender
    }

    // Repeat button (now "Done" button)
    @IBAction func repeatButtonClicked(_ sender: Any) {

        if let valenceValue = self.valence?.identifier?.rawValue,
            let arousalValue = self.arousal?.identifier?.rawValue,
            let activityValue = self.activityPopupButton.titleOfSelectedItem {

            let emotionTracker = TrackerManager.shared.getTracker(tracker: "EmotionTracker") as! EmotionTracker

            let timestamp = Date()
            let activity = activityValue
            let valence = NSNumber(value: Int16(valenceValue)!)
            let arousal = NSNumber(value: Int16(arousalValue)!)

            let questionnaire = Questionnaire(timestamp: timestamp, activity: activity, valence: valence, arousal: arousal)

            // Save questionnaire data
            emotionTracker.save(questionnaire: questionnaire)

            // Reset buttons
            resetForm()

            // Request a new notification
            emotionTracker.scheduleNotification()

            // Close the window
            self.close()

        } else {

            // If the user didn't choose an option for the the activity...
            if self.activityPopupButton.titleOfSelectedItem == nil {
                activityValidationLabel.isHidden = false
            } else {
                activityValidationLabel.isHidden = true
            }

            // If the user didn't choose an option for the valence radios...
            if self.valence?.identifier?.rawValue == nil {
                valenceValidationLabel.isHidden = false
            } else {
                valenceValidationLabel.isHidden = true
            }

            // If the user didn't choose an option for the arousal radios...
            if self.arousal?.identifier?.rawValue == nil {
                arousalValidationLabel.isHidden = false
            } else {
                arousalValidationLabel.isHidden = true
            }
        }
    }
}
