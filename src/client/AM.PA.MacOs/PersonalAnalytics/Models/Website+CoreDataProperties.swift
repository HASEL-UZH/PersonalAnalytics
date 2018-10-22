//
//  Website+CoreDataProperties.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-04.
//

import Foundation
import CoreData


extension Website {

    @nonobjc public class func fetchRequest() -> NSFetchRequest<Website> {
        return NSFetchRequest<Website>(entityName: "Website")
    }

    @NSManaged public var date: Double
    @NSManaged public var html: String?
    @NSManaged public var title: String?
    @NSManaged public var url: String?
    @NSManaged public var viewedBy: User?

}
