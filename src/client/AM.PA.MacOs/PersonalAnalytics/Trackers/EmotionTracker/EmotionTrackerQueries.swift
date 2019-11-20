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
    
    
    //TODO: refactor
    struct EmotionalStateEntry {
        var timestamp: Double
        var activity: String
        var valence: Int
        var arousal: Int
    }

    static func fetchEmotionalStateSince(time: Double) -> [EmotionalStateEntry] {
        let dbController = DatabaseController.getDatabaseController()
        var results: [EmotionalStateEntry] = []
        let timeStr = DateFormatConverter.interval1970ToDateStr(interval: time)
        
        do {
            let query = """
                        SELECT * FROM emotional_state
                        WHERE timestamp >= '\(timeStr)'
                        ORDER BY timestamp
                        """
            
            let rows = try dbController.executeFetchAll(query: query)
            
            for row in rows {

                let timestamp: Double = DateFormatConverter.dateStrToInterval1970(str: row["timestamp"])
                let activity: String = row["activity"]
                let valence: Int = row["valence"]
                let arousal: Int = row["arousal"]

                results.append(EmotionalStateEntry(timestamp: timestamp, activity: activity, valence: valence, arousal: arousal))
            }

        } catch {
            print(error)
        }

        return results
    }
}
