//
//  KeyboardInput.swift
//  PersonalAnalytics
//
//  Created by Roy Rutishauser on 16.12.19.
//

import Foundation

enum KeystrokeType {
    case Key
    case Navigate
    case Backspace
}


struct KeyStrokeEvent {
    var timestamp: Date
    var type: KeystrokeType
    
    init(type t: KeystrokeType) {
        timestamp = Date()
        type = t
    }
}
