//
//  Activity.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-31.
//

import Foundation

class Activity{
    
    var startTime: TimeInterval
    var endTime: TimeInterval
    var duration: TimeInterval
    var title: String
    var name: String
    var activityType: String
    var URL: String?
    
    init(start: TimeInterval, end: TimeInterval, title: String, name: String, url: String? = nil){
        self.startTime = start
        self.endTime = end
        self.title = title
        self.name = name
        self.activityType = ""
        self.duration = endTime - startTime
        self.URL = url
        activityType = getActivityType()
    }
    
    
    func getActivityType() -> String{
        
        let name = self.name.lowercased()
        
        for category in ActivityCategory.all{
            if(category.contains(name)){
                return category.name
            }
        }
        
        return "Other"
    }
    
    
}
