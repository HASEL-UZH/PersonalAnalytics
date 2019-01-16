//
//  EmotionTrackerHelpers.swift
//  PersonalAnalytics
//
//  Created by Luigi Quaranta on 2019-01-15.
//

import Foundation

struct EmotionTrackerHelpers {

    // Properties
    var context: NSManagedObjectContext?

    // MARK: Initializer
    init() {
        // COREDATA INITIALIZATION
        // Initialize a Persistent Container
        let container = NSPersistentContainer(name: "PersonalAnalytics")
        container.loadPersistentStores(completionHandler: { (description, error) in
            if let error = error {
                fatalError("Unable to load persistent stores: \(error)")
            }
        })

        // Get an NSManagedObjectContext
        self.context = container.newBackgroundContext()
    }


    // MARK: Persistence functions
    func storeQuestionnaire(questionnaire: Questionnaire) {

        let emotionalState = NSEntityDescription.insertNewObject(forEntityName: "EmotionalState", into: self.context!) as! EmotionalState

        emotionalState.timestamp = questionnaire.timestamp
        emotionalState.activity = questionnaire.activity
        emotionalState.valence = questionnaire.valence
        emotionalState.arousal = questionnaire.arousal

        do {
            try self.context?.save()
        } catch let error {
            print("It was not possible to save the last emotional state.")
            print("ERROR DETAILS: \(error)")
        }

        print("Emotional state saved:")
        print(emotionalState)

    }

    // MARK: Export functions
    // Export collected data to csv
    func exportToCsv(destinationPath: URL) {

        // csv string to be saved
        let csv = csv_string()

        do {
            try csv.write(to: destinationPath, atomically: true, encoding: String.Encoding.utf8)
        } catch {
            print("Something went wrong trying to save the file.")
            // Possible causes for the error: failed to write file – bad permissions, bad filename, missing permissions, or more likely it can't be converted to the encoding
        }

    }

    func csv_string() -> String {

        let dateFormatter = DateFormatter()
        dateFormatter.dateFormat = "yyyy-MM-dd HH:mm:ss"
        dateFormatter.locale = .current

        var csv = ""

        let emotionLogFetch = NSFetchRequest<NSFetchRequestResult>(entityName: "EmotionalState")
        do {
            let fetchedEmotionLogs = try context!.fetch(emotionLogFetch) as! [EmotionalState]
            for emotionLog in fetchedEmotionLogs {

                csv.append(dateFormatter.string(from: emotionLog.timestamp! as Date))
                csv.append(";")
                csv.append(emotionLog.activity ?? "nil")
                csv.append(";")
                csv.append(emotionLog.valence?.stringValue ?? "nil")
                csv.append(";")
                csv.append(emotionLog.arousal?.stringValue ?? "nil")
                csv.append("\n")

            }
        } catch {
            fatalError("Failed to fetch EmotionLogs: \(error)")
        }

        return csv
    }

}
