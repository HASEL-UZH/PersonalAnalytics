//
//  TrackerManager.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-06-26.
//

import Foundation


class TrackerManager {
    
    enum TrackerType {
        static let UserInput: String = "UserInput"
        static let ActiveApplication: String = "ActiveApplication"
        static let TaskProductivity: String = "TaskProductivity"
    }
    
    //singleton instance
    static let shared: TrackerManager = TrackerManager()
    
    fileprivate var trackers: [String:Tracker]
    fileprivate let order: [String] = [TrackerType.UserInput, TrackerType.ActiveApplication, TrackerType.TaskProductivity]
    
    fileprivate init(){
        trackers = [String:Tracker]()
    }
    
    func getVisualizations(date: Date, type: String) -> String{
        var html = ""
        for tracker in order{
            if let visualizations = trackers[tracker]?.viz{
                for viz in visualizations{
                    html += CreateDashboardItem(viz, date: date, type: type)
                }
            }
        }
        return html
    }
            
    func register(tracker: Tracker){
        if(trackers[tracker.type] == nil){
            trackers[tracker.type] = tracker
        }
        else{
            print("tracker already registered")
        }
    }
    
    func pause(){
        for tracker in trackers{
            tracker.value.pause()
        }
    }
    
    func resume(){
        for tracker in trackers{
            tracker.value.resume()
        }
    }
    
    func deregister(type: String){
        if(trackers[type] != nil){
            trackers[type]?.pause()
            trackers[type] = nil
        }
    }
    
    func getTracker(tracker: String) -> Tracker {
        return trackers[tracker]!
    }
    
    fileprivate func CreateDashboardItem(_ viz: Visualization, date: Date, type: String) -> String{
        do{
            let html = viz.getHtml(date, type: type)
            if(html == ""){
                return ""
            }
            let feedbackButtons = ""
            let title = "<h3 style='text-align: center;'>" + viz.title + "</h3>"
            
            return "<div class='item \(viz.Size)'>\(feedbackButtons)\(title)\(html)</div>"
        }
        catch{
            return "error"
        }
    }
}

