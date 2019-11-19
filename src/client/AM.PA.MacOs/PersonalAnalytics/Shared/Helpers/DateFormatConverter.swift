//
//  DateFormatConverter.swift
//  PersonalAnalytics
//
//  Created by Roy Rutishauser on 11.11.19.
//

import Foundation

class DateFormatConverter {
    
    static func interval1970ToDateStr(interval: Double) -> String {
        let formatter = DateFormatter()
        formatter.dateFormat = "yyyy-MM-dd HH:mm:ss.SSS"
        return formatter.string(from: Date(timeIntervalSince1970: interval))
    }
    
    static func dateStrToInterval1970(str: String) -> TimeInterval {
        let formatter = DateFormatter()
        formatter.dateFormat = "yyyy-MM-dd HH:mm:ss.SSS"
        return formatter.date(from: str)!.timeIntervalSince1970
    }
    
    static func dateToStr(date: Date) -> String {
        let formatter = DateFormatter()
        formatter.dateFormat = "yyyy-MM-dd HH:mm:ss.SSS"
        return formatter.string(from: date)
    }
}
