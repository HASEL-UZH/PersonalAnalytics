//
//  TrackerManager.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-06-26.
//

import Foundation


class TrackerManager {
    
    //singleton instance
    static let shared: TrackerManager = TrackerManager()
    
    fileprivate var trackers: [String:ITracker]
    
    fileprivate init(){
        trackers = [String:ITracker]()
    }
    
    func getVisualizations(date: Date, type: String) -> String{
        var html = ""
        for (_, tracker) in trackers{
            var viz: [IVisualization]?
            if(type == VisConstants.Day){
                viz = tracker.getVisualizationsDay(date: date)
            }
            else if(type == VisConstants.Week){
                viz = tracker.getVisualizationsWeek(date: date)
            }
            for v in viz!{
                html += CreateDashboardItem(v, date: date, type: type)
            }
        }
        return html
    }
            
    func register(tracker: ITracker){
        if(trackers[tracker.name] == nil){
            tracker.createDatabaseTablesIfNotExist()
            trackers[tracker.name] = tracker
        }
        else{
            print("tracker already registered")
        }
    }
    
    func pause(){
        for tracker in trackers{
            tracker.value.stop()
        }
    }
    
    func resume(){
        for tracker in trackers{
            tracker.value.start()
        }
    }
    
    func deregister(type: String){
        if(trackers[type] != nil){
            trackers[type]?.stop()
            trackers[type] = nil
        }
    }
    
    func getTracker(tracker: String) -> ITracker? {
        return trackers[tracker]
    }
    
    
    /// Forwards a notification to the right tracker after the notification was acknowledged/seen by the user. To do so, it checks if the notification identifier matches with the name of a tracker. In order to handle the notification a tracker needs to implement the TrackerUserNotificationHandling protocol.
    /// - Parameter notification: a user notification most likely scheduled by a tracker and acknowledged by the user.
    func handleTrackerUserNotifications(notification: NSUserNotification) {
        // check if a notification identifier exists
        guard let id = notification.identifier else {
            // programming "error". Fail early. Should never get this far in production.
            fatalError("User notification is ignored. Notification needs an identifier.")
        }
        
        // check if the notification identifier matches a tracker
        guard let tracker = trackers[id] else {
            fatalError("User notification [\(id)] is ignored. Notification identifier must be set to the name of the tracker itself.")
        }
        
        // check if tracker implements the TrackerUserNotificationHandling protocol
        guard let notifTracker = tracker as? TrackerUserNotificationHandling else {
            fatalError("User notification [\(id)] is ignored. Tracker [\(id)] needs to implement the TrackerUserNotificationHandling protocol.")
            
        }
        
        // success!
        print("Forwarding notification to [\(id)]")
        notifTracker.handleUserNotification(notification: notification)
    }
    
    fileprivate func CreateDashboardItem(_ viz: IVisualization, date: Date, type: String) -> String{
        let html = viz.getHtml(date, type: type)
        if(html == ""){
            return ""
        }
        let feedbackButtons = ""
        let title = "<h3 style='text-align: center;'>" + viz.title + "</h3>"
        
        return "<div class='item \(viz.Size)'>\(feedbackButtons)\(title)\(html)</div>"
    }
}

