//
//  SummaryViewController.swift
//  PersonalAnalytics
//
//  Created by Jonathan Stiansen on 2015-10-07.
//


//TODO: unsupress warnings when completed
import Cocoa
import CoreData
class SummaryViewController: NSViewController {

    var percievedProductivity: Int = 1
    var surveyStartTime: Date = Date()
    var surveyNotifyTime: Date = Date()
       
    @IBAction func submit(_ sender: NSButton) {
                
        DataObjectController.sharedInstance.saveUserEfficiency(userProductivity: percievedProductivity, surveyNotifyTime: surveyNotifyTime, surveyStartTime: surveyStartTime, surveyEndTime: Date())
        
        NotificationCenter.default.post(name: Notification.Name(rawValue: AppConstants.summarySubmittedNotification), object: nil)
        
        print("Submitted user efficiency update")
    }
    
    override func viewDidLoad() {
        super.viewDidLoad()
        surveyStartTime = Date()
        print("****** summarycontroller being allocated!!!! *****")
    }
    deinit{
        print("***** summarycontroller being deallocated *****")
    }
    
}
