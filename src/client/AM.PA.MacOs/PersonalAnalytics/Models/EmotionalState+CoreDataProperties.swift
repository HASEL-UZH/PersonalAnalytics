//
//  EmotionalState+CoreDataProperties.swift
//
//
//  Created by Luigi Quaranta on 2019-01-02.
//
//

import Foundation
import CoreData


extension EmotionalState {

    @nonobjc public class func fetchRequest() -> NSFetchRequest<EmotionalState> {
        return NSFetchRequest<EmotionalState>(entityName: "EmotionalState")
    }

    @NSManaged public var activity: String?
    @NSManaged public var arousal: NSNumber?
    @NSManaged public var valence: NSNumber?
    @NSManaged public var date: NSNumber?

}
