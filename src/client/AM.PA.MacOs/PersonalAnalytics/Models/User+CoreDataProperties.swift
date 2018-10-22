//
//  User+CoreDataProperties.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-06-26.
//

import Foundation
import CoreData


extension User {

    @nonobjc public class func fetchRequest() -> NSFetchRequest<User> {
        return NSFetchRequest<User>(entityName: "User")
    }

    @NSManaged public var id: NSNumber?
    @NSManaged public var created: NSOrderedSet?
    @NSManaged public var made: NSOrderedSet?
    @NSManaged public var used: NSOrderedSet?
    @NSManaged public var viewed: NSOrderedSet?

}

// MARK: Generated accessors for created
extension User {

    @objc(insertObject:inCreatedAtIndex:)
    @NSManaged public func insertIntoCreated(_ value: Summary, at idx: Int)

    @objc(removeObjectFromCreatedAtIndex:)
    @NSManaged public func removeFromCreated(at idx: Int)

    @objc(insertCreated:atIndexes:)
    @NSManaged public func insertIntoCreated(_ values: [Summary], at indexes: NSIndexSet)

    @objc(removeCreatedAtIndexes:)
    @NSManaged public func removeFromCreated(at indexes: NSIndexSet)

    @objc(replaceObjectInCreatedAtIndex:withObject:)
    @NSManaged public func replaceCreated(at idx: Int, with value: Summary)

    @objc(replaceCreatedAtIndexes:withCreated:)
    @NSManaged public func replaceCreated(at indexes: NSIndexSet, with values: [Summary])

    @objc(addCreatedObject:)
    @NSManaged public func addToCreated(_ value: Summary)

    @objc(removeCreatedObject:)
    @NSManaged public func removeFromCreated(_ value: Summary)

    @objc(addCreated:)
    @NSManaged public func addToCreated(_ values: NSOrderedSet)

    @objc(removeCreated:)
    @NSManaged public func removeFromCreated(_ values: NSOrderedSet)

}

// MARK: Generated accessors for made
extension User {

    @objc(insertObject:inMadeAtIndex:)
    @NSManaged public func insertIntoMade(_ value: AggregatedInput, at idx: Int)

    @objc(removeObjectFromMadeAtIndex:)
    @NSManaged public func removeFromMade(at idx: Int)

    @objc(insertMade:atIndexes:)
    @NSManaged public func insertIntoMade(_ values: [AggregatedInput], at indexes: NSIndexSet)

    @objc(removeMadeAtIndexes:)
    @NSManaged public func removeFromMade(at indexes: NSIndexSet)

    @objc(replaceObjectInMadeAtIndex:withObject:)
    @NSManaged public func replaceMade(at idx: Int, with value: AggregatedInput)

    @objc(replaceMadeAtIndexes:withMade:)
    @NSManaged public func replaceMade(at indexes: NSIndexSet, with values: [AggregatedInput])

    @objc(addMadeObject:)
    @NSManaged public func addToMade(_ value: AggregatedInput)

    @objc(removeMadeObject:)
    @NSManaged public func removeFromMade(_ value: AggregatedInput)

    @objc(addMade:)
    @NSManaged public func addToMade(_ values: NSOrderedSet)

    @objc(removeMade:)
    @NSManaged public func removeFromMade(_ values: NSOrderedSet)

}

// MARK: Generated accessors for used
extension User {

    @objc(insertObject:inUsedAtIndex:)
    @NSManaged public func insertIntoUsed(_ value: ActiveApplication, at idx: Int)

    @objc(removeObjectFromUsedAtIndex:)
    @NSManaged public func removeFromUsed(at idx: Int)

    @objc(insertUsed:atIndexes:)
    @NSManaged public func insertIntoUsed(_ values: [ActiveApplication], at indexes: NSIndexSet)

    @objc(removeUsedAtIndexes:)
    @NSManaged public func removeFromUsed(at indexes: NSIndexSet)

    @objc(replaceObjectInUsedAtIndex:withObject:)
    @NSManaged public func replaceUsed(at idx: Int, with value: ActiveApplication)

    @objc(replaceUsedAtIndexes:withUsed:)
    @NSManaged public func replaceUsed(at indexes: NSIndexSet, with values: [ActiveApplication])

    @objc(addUsedObject:)
    @NSManaged public func addToUsed(_ value: ActiveApplication)

    @objc(removeUsedObject:)
    @NSManaged public func removeFromUsed(_ value: ActiveApplication)

    @objc(addUsed:)
    @NSManaged public func addToUsed(_ values: NSOrderedSet)

    @objc(removeUsed:)
    @NSManaged public func removeFromUsed(_ values: NSOrderedSet)

}

// MARK: Generated accessors for viewed
extension User {

    @objc(insertObject:inViewedAtIndex:)
    @NSManaged public func insertIntoViewed(_ value: Website, at idx: Int)

    @objc(removeObjectFromViewedAtIndex:)
    @NSManaged public func removeFromViewed(at idx: Int)

    @objc(insertViewed:atIndexes:)
    @NSManaged public func insertIntoViewed(_ values: [Website], at indexes: NSIndexSet)

    @objc(removeViewedAtIndexes:)
    @NSManaged public func removeFromViewed(at indexes: NSIndexSet)

    @objc(replaceObjectInViewedAtIndex:withObject:)
    @NSManaged public func replaceViewed(at idx: Int, with value: Website)

    @objc(replaceViewedAtIndexes:withViewed:)
    @NSManaged public func replaceViewed(at indexes: NSIndexSet, with values: [Website])

    @objc(addViewedObject:)
    @NSManaged public func addToViewed(_ value: Website)

    @objc(removeViewedObject:)
    @NSManaged public func removeFromViewed(_ value: Website)

    @objc(addViewed:)
    @NSManaged public func addToViewed(_ values: NSOrderedSet)

    @objc(removeViewed:)
    @NSManaged public func removeFromViewed(_ values: NSOrderedSet)

}
