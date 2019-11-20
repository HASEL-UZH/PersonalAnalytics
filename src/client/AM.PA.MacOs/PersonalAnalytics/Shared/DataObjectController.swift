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
            try db.execute(sql: query)
        }
    }
    
    /**
    * Executes SQL statements that do not return a database row
    **/
    func executeUpdate(query: String, arguments args: StatementArguments) throws {
        try dbQueue.write{ db in
            try db.execute(sql: query, arguments:args)
        }
    }
    
    /**
     * Executes SQL statements that fetches database rows
     **/
    func executeFetchAll(query: String) throws -> [Row]{
        let rows = try dbQueue.read{ db in
            try Row.fetchAll(db, sql: query)
        }
        return rows
    }
    
    func executeFetchOne(query: String) throws -> Row {
        let row = try dbQueue.read{ db in
            try Row.fetchOne(db, sql: query)
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
    
    static let sharedInstance : DataObjectController = DataObjectController()

    fileprivate override init(){
        super.init()
    }
    
    // (Percent)
    typealias Percent = Int
    
    // MARK: - Save tracker data to sqlite
   
    func saveEmotionalState(questionnaire: Questionnaire) {
        let dbController = DatabaseController.getDatabaseController()
        
        do {
            let args:StatementArguments = [
                questionnaire.timestamp,
                questionnaire.activity,
                questionnaire.valence,
                questionnaire.arousal
            ]

            let q = """
                    INSERT INTO emotional_state (timestamp, activity, valence, arousal)
                    VALUES (?, ?, ?, ?)
                    """
                   
            try dbController.executeUpdate(query: q, arguments:args)
                   
        } catch {
            print(error)
        }
    }
    
    
    func buildCSVString(input: [UserInputQueries.AggregatedInputEntry]) -> String{
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
    
    func buildCSVString(input: [WindowsActivityQueries.ActiveApplicationEntry]) -> String{
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
            result += String(row.timestamp) + ","
            result += String(row.activity) + ","
            result += String(row.valence) + ","
            result += String(row.arousal) + "\n"
        }
        return result
    }
    
    func exportStudyData(startTime: Double){
        do{
            let sql = try SQLController()
    
            let emotionalStates = sql.fetchEmotionalStateSince(time: startTime)
            
            // TODO: this will need refactoring. We should not user UserInputQueries here
            let aggregatedInput = UserInputQueries.fetchAggregatedInputSince(time: startTime)
            let activeApplications = WindowsActivityQueries.fetchActiveApplicationsSince(time: startTime)
            
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
