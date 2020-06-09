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
        // The directory the application uses to store the sqlite database file. This code uses a directory named "PersonalAnalytics" in the user's Application Support directory.
        let urls = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)
        let appSupportURL = urls[urls.count - 1]
        return appSupportURL.appendingPathComponent(Environment.appSupportDir)
    }()
    
    fileprivate init(){
        do{
            dbQueue = try DatabaseQueue(path: applicationDocumentsDirectory.appendingPathComponent(Environment.sqliteDbName).absoluteString)
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
