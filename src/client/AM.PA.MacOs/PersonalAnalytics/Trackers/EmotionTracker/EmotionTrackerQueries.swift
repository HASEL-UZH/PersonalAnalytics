//
//  EmotionTrackerQueries.swift
//  PersonalAnalytics
//
//  Created by Roy Rutishauser on 20.11.19.
//

import Foundation
import GRDB

class EmotionTrackerQueries {
    
    static func createDatabaseTablesIfNotExist() {

        let dbController = DatabaseController.getDatabaseController()

        do{
            try dbController.executeUpdate(query: "CREATE TABLE IF NOT EXISTS \(EmotionTrackerSettings.DbTable) (id INTEGER PRIMARY KEY, timestamp TEXT, activity TEXT, valence INTEGER, arousal INTEGER)");
        }
        catch{
            print(error)
        }
    }
    
    
    static func saveEmotionalState(questionnaire: Questionnaire) {
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
}
