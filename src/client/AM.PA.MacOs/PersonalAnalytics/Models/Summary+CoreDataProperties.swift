//
//  Summary+CoreDataProperties.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-04.
//

import Foundation
import CoreData


extension Summary {

    @nonobjc public class func fetchRequest() -> NSFetchRequest<Summary> {
        return NSFetchRequest<Summary>(entityName: "Summary")
    }

    @NSManaged public var createdByUser: NSNumber?
    @NSManaged public var percentageDone: NSNumber?
    @NSManaged public var percentSimilarToPrevious: NSNumber?
    @NSManaged public var submissionTime: Double
    @NSManaged public var taskName: String?
    @NSManaged public var totalEstimatedTime: NSNumber?
    @NSManaged public var createdBy: User?
    @NSManaged public var percievedProductivity: NSNumber?

}
