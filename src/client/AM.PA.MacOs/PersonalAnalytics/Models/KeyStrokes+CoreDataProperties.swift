//
//  KeyStrokes+CoreDataProperties.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-04.
//

import Foundation
import CoreData


extension KeyStrokes {

    @nonobjc public class func fetchRequest() -> NSFetchRequest<KeyStrokes> {
        return NSFetchRequest<KeyStrokes>(entityName: "KeyStrokes")
    }

    @NSManaged public var time: NSDate?
    @NSManaged public var typing: String?
    @NSManaged public var madeBy: User?

}
