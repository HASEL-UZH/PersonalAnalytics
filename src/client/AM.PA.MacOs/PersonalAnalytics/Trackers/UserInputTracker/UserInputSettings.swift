//
//  UserInputSettings.swift
//  PersonalAnalytics
//
//  Created by Roy Rutishauser on 19.11.19.
//

import Foundation


enum UserInputSettings {
    
    static let isEnabledByDefault = true // not used yet
    static let IsDetailedCollectionEnabled = false
    
    // MARK: - timer intervals
    static let UserInputAggregationInterval = TimeInterval(60) // 1 min
    static let UserInputVisInterval = TimeInterval(600) // 10 mins
    
    
    // MARK: - database tables
    static let DbTableUserInput_v2 = "user_input"; // aggregate of user inputs per minute (use this, not the *_v1 ones if possible!)
    static let DbTableKeyboard_v1 = "user_input_keyboard"; // for old deployments & in case a study needs more detailed data
    static let DbTableMouseClick_v1 = "user_input_mouse_click"; // for old deployments & in case a study needs more detailed data
    static let DbTableMouseScrolling_v1 = "user_input_mouse_scrolling"; // for old deployments & in case a study needs more detailed data
    static let DbTableMouseMovement_v1 = "user_input_mouse_movement"; // for old deployments & in case a study needs more detailed data
    
    
    // MARK: - user input level weighting
    static let MouseClickKeyboardRatio: Double = 3
    static let MouseMovementKeyboardRatio: Double = 0.0028
    static let MouseScrollingKeyboardRatio: Double = 1.55
}
