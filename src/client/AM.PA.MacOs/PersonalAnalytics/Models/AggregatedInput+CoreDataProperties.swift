//
//  AggregatedInput+CoreDataProperties.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-05.
//

import Foundation
import CoreData


extension AggregatedInput {

    @nonobjc public class func fetchRequest() -> NSFetchRequest<AggregatedInput> {
        return NSFetchRequest<AggregatedInput>(entityName: "AggregatedInput")
    }

    @NSManaged public var clickCount: NSNumber?
    @NSManaged public var distance: NSNumber?
    @NSManaged public var scrollDelta: NSNumber?
    @NSManaged public var time: Double
    @NSManaged public var keyOther: NSNumber?
    @NSManaged public var keyNavigate: NSNumber?
    @NSManaged public var keyDelete: NSNumber?
    @NSManaged public var keyTotal: NSNumber?

}
