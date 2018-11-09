//
//  ActivityCategory.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-31.
//

import Foundation

class ActivityCategory{
    
    
    static var Browsing: Category = Category(name: "Browsing", contents: ["google chrome", "safari", "firefox", "mozilla firefox"])
    static var Other: Category = Category(name: "Other", contents: [])
    static var Editor: Category = Category(name: "Reading/Editing Documents", contents: ["preview", "microsoft word", "microsoft excel", "microsoft powerpoint", "adobe acrobat"])
    static var Development: Category = Category(name:"Coding", contents: ["xcode", "visual studio community", "webstorm", "eclipse", "coda", "textmate", "atom", "sublime text", "pycharm", "intellij"])
    static var Finder: Category = Category(name: "Finder", contents: ["finder"])
    static var InstantMessaging: Category = Category(name: "Instant Messaging", contents: ["skype", "slack", "lync", "wechat"])
    static var Idle: Category = Category(name: "Idle", contents: ["idle"])
    
    static var all: [Category] = [Browsing, Other, Editor, Development, Finder, InstantMessaging, Idle]

}

    
