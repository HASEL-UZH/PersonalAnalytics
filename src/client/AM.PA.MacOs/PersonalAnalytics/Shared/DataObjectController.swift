//
//  DataObjectController.swift
//  PersonalAnalytics
//
//  Created by Jonathan Stiansen on 2015-10-16.
//

import Cocoa
import CoreData
import GRDB

enum DatabaseError: Error{
    case fetchError(String)
}

class DatabaseController{
    
    fileprivate static let _dbController: DatabaseController = DatabaseController()
    let dbQueue: DatabaseQueue
    let applicationDocumentsDirectory: URL = {
        // The directory the application uses to store the Core Data store file. This code uses a directory named "PersonalAnalytics" in the user's Application Support directory.
        let urls = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)
        let appSupportURL = urls[urls.count - 1]
        return appSupportURL.appendingPathComponent("PersonalAnalytics")
    }()
    
    fileprivate init(){
        do{
            dbQueue = try DatabaseQueue(path: applicationDocumentsDirectory.appendingPathComponent("PersonalAnalytics.dat").absoluteString)
        }
        catch{
            fatalError("Could not initialize Database: \(error)")
        }
    }
    
    static func getDatabaseController() -> DatabaseController{
        return ._dbController
    }
    
    /**
    * Executes SQL statements that do not return a database row
    **/
    func executeUpdate(query: String) throws {
        try dbQueue.write{ db in
            try db.execute(query)
        }
    }
    
    /**
     * Executes SQL statements that fetches database rows
     **/
    func executeFetchAll(query: String) throws -> [Row]{
        let rows = try dbQueue.read{ db in
            try Row.fetchAll(db, query)
        }
        return rows
    }
    
    func executeFetchOne(query: String) throws -> Row {
        let row = try dbQueue.read{ db in
            try Row.fetchOne(db, query)
        }
        if((row) != nil){
            return row!
        }
        else{
            throw DatabaseError.fetchError("fetchOne failed")
        }
    }
}

/**
* Responsible for managing saving, and management of coredata objects
**/
class DataObjectController: NSObject{
    
    fileprivate weak var managedContext: NSManagedObjectContext?
    fileprivate var lastSummary: NSManagedObject?
    
    var acceptingWebsites = true

    static let sharedInstance : DataObjectController = DataObjectController()
    
    internal var previousTask: String {
        if let summary = lastSummary{
            return summary.value(forKey: taskNameKey) as! String
        } else {
            return ""
        }
    }
    let lockQueue = DispatchQueue(label: "saveQueue")

    
    // MARK: Constants
    let taskNameKey = "taskName"

    fileprivate override init(){
        
        let appDelegate = NSApplication.shared.delegate as? AppDelegate
        self.managedContext = appDelegate!.managedObjectContext
        super.init()
    
        //save before power off/switch user
        NSWorkspace.shared.notificationCenter.addObserver(self,
                                                            selector: #selector(saveContext),
                                                            name: NSWorkspace.willPowerOffNotification,
                                                            object: nil)
        NSWorkspace.shared.notificationCenter.addObserver(self,
                                                            selector: #selector(saveContext),
                                                            name: NSWorkspace.willSleepNotification,
                                                            object: nil)
        NSWorkspace.shared.notificationCenter.addObserver(self,
                                                            selector: #selector(saveContext),
                                                            name: NSWorkspace.sessionDidResignActiveNotification,
                                                            object: nil)
    }
    
    @objc func saveContext(){
        lockQueue.sync {
            print("saving context of data objects")
            do {
                try managedContext?.save()
                
                print("saved")
            } catch {
                print(error)
            }
        }
    }
    // (Percent)
    typealias Percent = Int
    
    // MARK: - Save current object models
    
    // Gets current user from context, and automatically adds it
    func saveSummary(_ taskName: String, totalEsimatedTime: Double, percentageDone: Percent, percentSimilarToBefore: Percent, createdByUser userFlag: Bool, percievedProductivity: Int){
        lockQueue.sync {
            let entity = NSEntityDescription.entity(forEntityName: "Summary", in: managedContext!)
            if let entity = entity{
                let summary = NSManagedObject(entity: entity, insertInto: managedContext)
                summary.setValue(taskName, forKey: taskNameKey)
                summary.setValue(totalEsimatedTime, forKey: "totalEstimatedTime")
                summary.setValue(percentageDone, forKey: "percentageDone")
                summary.setValue(percentSimilarToBefore, forKey: "percentSimilarToPrevious")
                summary.setValue(Date().timeIntervalSince1970, forKey: "submissionTime")
                summary.setValue(userFlag, forKey: "createdByUser")
                summary.setValue(percievedProductivity, forKey: "percievedProductivity")
                // This is so we can retrieve information again and tell if there was a task before hand.
                lastSummary = summary;
            }
        }
        print("saving summary")

        self.saveContext()
    }
    
    func saveCurrentWebsite(_ title:String, url:String, html:String, datetime: Date){
        if(!acceptingWebsites){
            return
        }
        lockQueue.sync {
            let entity = NSEntityDescription.entity(forEntityName: "Website", in: managedContext!)
            if let entity = entity {
                let currentWebsite = NSManagedObject(entity: entity, insertInto: managedContext!)
                currentWebsite.setValue(title, forKey: "title")
                currentWebsite.setValue(url, forKey: "url")
                currentWebsite.setValue(html, forKey: "html")
                currentWebsite.setValue(datetime.timeIntervalSince1970, forKey: "time")
                
            }
            print("saving website")
        }

        self.saveContext()
    }
    
    func saveActiveApplication(_ name: String, startTime: Date, endTime: Date?){
        lockQueue.sync {
            let entity = NSEntityDescription.entity(forEntityName: "ActiveApplication", in: managedContext!)
            if let entity = entity{
                let activeApp = NSManagedObject(entity: entity, insertInto: managedContext)
                activeApp.setValue(name, forKey: "name")
                activeApp.setValue(startTime.timeIntervalSince1970, forKey: "startTime")
                activeApp.setValue(endTime?.timeIntervalSince1970, forKey: "endTime")
                print(activeApp)
            }
        }
        print("saving active")
        self.saveContext()
    }
    
    func saveKeystrokes(_ keys: String, time: Date){
        lockQueue.sync {
            let entity = NSEntityDescription.entity(forEntityName: "KeyStrokes", in: managedContext!)
            if let entity = entity{
                let keystokes = NSManagedObject(entity: entity, insertInto: managedContext)
                keystokes.setValue(time, forKey: "time")
                keystokes.setValue(keys, forKey: "typing")
            }
        }
        print("saving keystrokes")
        self.saveContext()
    }
    
    func saveMouseAction(clickCount: Int, distance: Int, scrollDelta: Int, time:Date){
        lockQueue.sync {
            let entity = NSEntityDescription.entity(forEntityName: "MouseAction", in: managedContext!)
            if let entity = entity{
                let mouseAction = NSManagedObject(entity: entity, insertInto: managedContext)
                mouseAction.setValue(time, forKey: "time")
                mouseAction.setValue(clickCount, forKey: "clickCount")
                mouseAction.setValue(distance, forKey: "distance")
                mouseAction.setValue(scrollDelta, forKey: "scrollDelta")
            }
        }
        self.saveContext()
    }

    func saveEmotionalState(questionnaire: Questionnaire) {
        lockQueue.sync {
            let entity = NSEntityDescription.entity(forEntityName: "EmotionalState", in: managedContext!)
            if let entity = entity {
                let emotionalState = NSManagedObject(entity: entity, insertInto: managedContext)
                emotionalState.setValue(questionnaire.timestamp.timeIntervalSince1970, forKey: "date")
                emotionalState.setValue(questionnaire.activity, forKey: "activity")
                emotionalState.setValue(questionnaire.valence, forKey: "valence")
                emotionalState.setValue(questionnaire.arousal, forKey: "arousal")
            } else {
                print("It was impossible to save the last emotional state.")
            }
        }
        self.saveContext()
    }
    
    func newActiveApplication(_ name: String, title: String) -> ActiveApplication {
        
        return lockQueue.sync{
            let appDelegate = NSApplication.shared.delegate as! AppDelegate
        
            let activeApp = NSEntityDescription.insertNewObject(forEntityName: "ActiveApplication", into: appDelegate.managedObjectContext) as! ActiveApplication
            activeApp.name = name
            activeApp.startTime = Date().timeIntervalSince1970
        
            activeApp.title = title
            activeApp.endTime = Date().timeIntervalSince1970
        
            return activeApp
        }
    }
    
    func saveUserInput(aggregatedInput:UserInputTracker){
        lockQueue.sync {
            let entity = NSEntityDescription.entity(forEntityName: "AggregatedInput", in: managedContext!)
            let keyTotal = aggregatedInput.keyCount + aggregatedInput.deleteCount + aggregatedInput.navigateCount
            if let entity = entity{
                let aggregate = NSManagedObject(entity: entity, insertInto: managedContext)
                aggregate.setValue(aggregatedInput.time.timeIntervalSince1970, forKey: "time")
                aggregate.setValue(aggregatedInput.clickCount, forKey: "clickCount")
                aggregate.setValue(aggregatedInput.distance, forKey: "distance")
                aggregate.setValue(aggregatedInput.scrollDelta, forKey: "scrollDelta")
                aggregate.setValue(aggregatedInput.keyCount, forKey: "keyOther")
                aggregate.setValue(aggregatedInput.deleteCount, forKey:"keyDelete")
                aggregate.setValue(aggregatedInput.navigateCount, forKey:"keyNavigate")
                aggregate.setValue(keyTotal, forKey:"keyTotal")
            }
        }
        
        self.saveContext()
    }
    
    func getRecentObject(type: String, sortOn: String) throws -> NSManagedObject {
        var managedObject: [Any]?
        lockQueue.sync {
            let request = NSFetchRequest<NSFetchRequestResult>(entityName: type)
            request.sortDescriptors = [NSSortDescriptor(key: sortOn, ascending: false)]
            request.fetchLimit = 1
            
            do{
                managedObject = try managedContext?.fetch(request)
            }
            catch{
                print("couldnt fetch recent object")
            }
        }
        return managedObject![0] as! NSManagedObject

    }

    
    func saveFocusState(userInputLevel: Double, focusState: String, smoothedFocusState: String){
        lockQueue.sync {
            let entity = NSEntityDescription.entity(forEntityName: "FocusState", in: managedContext!)
            if let entity = entity{
                let focus = NSManagedObject(entity: entity, insertInto: managedContext)
                focus.setValue(Date().timeIntervalSince1970, forKey: "time")
                focus.setValue(userInputLevel, forKey: "userinputlevel")
                focus.setValue(focusState, forKey: "focusstate")
                focus.setValue(smoothedFocusState, forKey: "smoothedfocusstate")
            }
        }
        self.saveContext()
    }
    
    deinit{
        saveContext()
    }
    
    func buildCSVString(input: [SQLController.AggregatedInputEntry]) -> String{
        var result = "Time,KeyTotal,ClickCount,Distance,ScrollDelta\n"
        for row in input {
            result += String(row.time) + ","
            result += String(row.keyTotal) + ","
            result += String(row.clickCount) + ","
            result += String(row.distance) + ","
            result += String(row.scrollDelta) + "\n"
        }
        return result
    }
    
    func buildCSVString(input: [SQLController.ActiveApplicationEntry]) -> String{
        var result = "StartTime,EndTime,AppName,WindowTitle\n"
        for row in input {
            result += String(row.startTime) + ","
            result += String(row.endTime) + ","
            result += String(row.appName) + ","
            result += String(row.windowTitle) + "\n"
        }
        return result
    }

    func buildCSVString(input: [SQLController.EmotionalStateEntry]) -> String {

        let dateFormatter = DateFormatter()
        dateFormatter.dateFormat =  "yyyy-MM-dd HH:mm:ss"
        dateFormatter.locale = .current

        var result = "Timestamp,Activity,Valence,Arousal\n"


        for row in input {
            let date = Date(timeIntervalSince1970: row.timestamp)
            result += String(dateFormatter.string(from: date)) + ","
            result += String(row.activity) + ","
            result += String(row.valence) + ","
            result += String(row.arousal) + "\n"
        }
        return result
    }
    
    func exportStudyData(startTime: Double){
        do{
            let sql = try SQLController()
            let aggregatedInput = sql.fetchAggregatedInputSince(time: startTime)
            let activeApplications = sql.fetchActiveApplicationsSince(time: startTime)
            let emotionalStates = sql.fetchEmotionalStateSince(time: startTime)

            let inputString = buildCSVString(input: aggregatedInput)
            let appString = buildCSVString(input: activeApplications)
            let emotionString = buildCSVString(input: emotionalStates)

            let dir = URL(fileURLWithPath: NSHomeDirectory()).appendingPathComponent("Study Data")
            let inputData = inputString.data(using: String.Encoding.utf8)!
            try inputData.write(to: dir.appendingPathComponent("input.csv"))
            
            let appData = appString.data(using: String.Encoding.utf8)!
            try appData.write(to: dir.appendingPathComponent("appdata.csv"))

            let emotionData = emotionString.data(using: String.Encoding.utf8)!
            try emotionData.write(to: dir.appendingPathComponent("emotionData.csv"))
        }
        catch{
            print(error)
        }
    }
}
