//
//  MouseInput.swift
//  PersonalAnalytics
//
//  Created by Roy Rutishauser on 16.12.19.
//

import Foundation


enum MouseButtonType {
    case Left
    case Right
    case Middle
    case XButton1
    case XButton2
}

struct MouseClickEvent {
    var timestamp: Date
    var button: MouseButtonType
    var x: Int
    var y: Int
    
    init(button btn: MouseButtonType, location: NSPoint) {
        timestamp = Date()
        button = btn
        x = Int(location.x)
        y = Int(location.y)
    }
}

struct MouseMovementSnapshot {
    var timestamp: Date
    var movedDistance: Int
    var x: Int
    var y: Int
    
    init(movedDistance d: Int, location: NSPoint) {
        timestamp = Date()
        movedDistance = d
        x = Int(location.x)
        y = Int(location.y)
    }
}

struct MouseScrollSnapshot {
    var timestamp: Date
    var scrollDelta: Int
    var x: Int
    var y: Int
    
    init(scrollDelta d: Int, location: NSPoint) {
        timestamp = Date()
        scrollDelta = d
        x = Int(location.x)
        y = Int(location.y)
    }
}
