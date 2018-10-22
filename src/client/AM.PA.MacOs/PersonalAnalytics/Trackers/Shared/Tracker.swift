//
//  ITracker.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-27.
//

import Foundation

enum TrackerConstants{
    static let KeyboardEventNotification = Notification.Name("keyEvent")
    static let MouseEventNotification = Notification.Name("mouseEvent")
    static let MouseEvent = "mouseEvent"
    static let KeyEvent = "keyEvent"
}

protocol Tracker {
    
    //list of visualizations to present for the tracker
    var viz: [Visualization] {get}
    var type: String {get}
    
    func pause()
    func resume()
    
}
