//
//  SummaryViewController.swift
//  PersonalAnalytics
//
//  Created by Jonathan Stiansen on 2015-10-07.
//


//TODO: unsupress warnings when completed
import Cocoa

class SummaryViewController: NSViewController {
    
    // TODO: use a regular window
    let popover = NSPopover()
    
    var percievedProductivity: Int = 1
    var surveyStartTime: Date = Date()
    var surveyNotifyTime: Date = Date()
       
    @IBAction func submit(_ sender: NSButton) {
                
        UserEfficiencyQueries.saveUserEfficiency(userProductivity: percievedProductivity, surveyNotifyTime: surveyNotifyTime, surveyStartTime: surveyStartTime, surveyEndTime: Date())
        
        closeSummaryPopup()
        
        print("Submitted user efficiency update")
    }
    
    override func viewDidLoad() {
        super.viewDidLoad()
        surveyStartTime = Date()
    }
    
    func closeSummaryPopup(){
        if(popover.isShown){
            popover.performClose(nil)
        }
    }
    
    func showSummaryPopup() {
        popover.contentViewController = self
        popover.behavior = .transient
        // TODO: Not nice to use the delegate here, better options?
        let appDelegate = NSApplication.shared.delegate as! AppDelegate
        if let button = appDelegate.menu.statusItem.button {
            popover.show(relativeTo: button.bounds, of: button, preferredEdge: NSRectEdge.minY)
            NSApp.activate(ignoringOtherApps: true)
        }
    }
}
