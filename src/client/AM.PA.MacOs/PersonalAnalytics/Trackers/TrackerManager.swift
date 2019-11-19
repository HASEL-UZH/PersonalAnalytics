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
    
    func getTracker(tracker: String) -> ITracker {
        return trackers[tracker]!
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

