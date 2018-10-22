//
//  DateTimeHelper.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-06-06.
//

import Foundation

extension Date {
    struct Gregorian {
        static let calendar = Calendar(identifier: .gregorian)
    }
    var startOfWeek: Date? {
        return Gregorian.calendar.date(from: Gregorian.calendar.dateComponents([.yearForWeekOfYear, .weekOfYear], from: self))
    }
    var endOfWeek: Date? {
        return startOfWeek?.addingTimeInterval(24*60*60*7)
    }
}
