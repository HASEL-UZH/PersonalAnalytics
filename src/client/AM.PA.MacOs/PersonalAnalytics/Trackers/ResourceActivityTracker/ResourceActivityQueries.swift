//
//  ResourceActivityQueries.swift
//  PersonalAnalytics
//
//  Created by Roy Rutishauser on 04.02.20.
//

import GRDB

class ResourceActivityQueries {
    
    static func createDatabaseTablesIfNotExist() {
        let dbController = DatabaseController.getDatabaseController()
        
        do {
            try dbController.executeUpdate(query: "CREATE TABLE IF NOT EXISTS \(ResourceActivitySettings.DbTableActivity) (id INTEGER PRIMARY KEY, time TEXT, path TEXT, flags TEXT);")
            try dbController.executeUpdate(query: "CREATE TABLE IF NOT EXISTS \(ResourceActivitySettings.DbTableApplicationResource) (id INTEGER PRIMARY KEY, time TEXT, path TEXT, process TEXT);")
        }
        catch{
            print(error)
        }
    }
    
    
    static func saveResourceActivity(date: Date, path: String, flags: EonilFSEventsEventFlags) {
        let dbController = DatabaseController.getDatabaseController()
        
        do {
            let args:StatementArguments = [
                date,
                path,
                flags.description
            ]
            
            let q = """
                    INSERT INTO \(ResourceActivitySettings.DbTableActivity) (time, path, flags)
                    VALUES (?, ?, ?)
                    """
            
            try dbController.executeUpdate(query: q, arguments:args)
            
        } catch {
            print(error)
        }
    }
    
    
    static func saveResourceOfApplication(date: Date, path: String, process: String) {
        let dbController = DatabaseController.getDatabaseController()
        
        do {
            let args:StatementArguments = [
                date,
                path,
                process
            ]
            
            let q = """
                    INSERT INTO \(ResourceActivitySettings.DbTableApplicationResource) (time, path, process)
                    VALUES (?, ?, ?)
                    """
            
            try dbController.executeUpdate(query: q, arguments:args)
            
        } catch {
            print(error)
        }
    }
}


