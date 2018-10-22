//
//  SummaryViewController.swift
//  PersonalAnalytics
//
//  Created by Jonathan Stiansen on 2015-10-07.
//

import Cocoa
import CoreData
class SummaryViewController: NSViewController {

    let previousTask: String = DataObjectController.sharedInstance.previousTask

    var taskName: String?
    var percievedProductivity: Int = 1
    var estimatedTotalTime: Double = 0.5
    var estimatedTaskCompletionPercent: Int = 25
    var similarityToPreviousTask: Int = 0
    var hidePreviousTaskElements: Bool {
        return previousTask == ""
    }

    
    @IBAction func submit(_ sender: NSButton) {
        if let taskName = taskName{
            DataObjectController.sharedInstance.saveSummary(taskName, totalEsimatedTime: estimatedTotalTime, percentageDone: estimatedTaskCompletionPercent, percentSimilarToBefore: similarityToPreviousTask, createdByUser: true, percievedProductivity: percievedProductivity)
                NotificationCenter.default.post(name: Notification.Name(rawValue: AppConstants.summarySubmittedNotification), object: nil)
                print("Submitted \(taskName): \(estimatedTotalTime), \(estimatedTaskCompletionPercent), \(similarityToPreviousTask)")
        } else {
            let noTaskNotification = NSUserNotification()
            noTaskNotification.title = "No task entered"
            noTaskNotification.informativeText = "Please enter task into taskbar"
            noTaskNotification.hasActionButton = false
            NSUserNotificationCenter.default.deliver(noTaskNotification)
            // Send NSUserNotification saying thanks!
        }

    }
    
    override func viewDidLoad() {
        super.viewDidLoad()
        print("****** summarycontroller being allocated!!!! *****")
    }
    deinit{
        print("***** summarycontroller being deallocated *****")
    }
    
}
