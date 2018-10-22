//
//  ActiveApplication+CoreDataProperties.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-04.
//

import Foundation
import CoreData


extension ActiveApplication {

    @nonobjc public class func fetchRequest() -> NSFetchRequest<ActiveApplication> {
        return NSFetchRequest<ActiveApplication>(entityName: "ActiveApplication")
    }

    @NSManaged public var endTime: Double
    @NSManaged public var name: String?
    @NSManaged public var startTime: Double
    @NSManaged public var usedBy: User?
    @NSManaged public var title: String?

}
