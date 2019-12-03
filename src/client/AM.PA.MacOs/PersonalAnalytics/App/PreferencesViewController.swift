//
//  PreferencesViewController.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-11.
//

import Foundation

class PreferencesViewController: NSViewController{
    
    let defaults = UserDefaults.standard
    let defaultsController = NSUserDefaultsController.shared
    let appDelegate = NSApplication.shared.delegate as! AppDelegate

    lazy var applicationDocumentsDirectory: URL = {
        // The directory the application uses to store the Core Data store file. This code uses a directory named "PersonalAnalytics" in the user's Application Support directory.
        let urls = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)
        let appSupportURL = urls[urls.count - 1]
        return appSupportURL.appendingPathComponent(Environment.appSupportDir)
    }()
    
    @IBOutlet weak var toggleTimer: NSButton!
    @IBOutlet weak var toggleSwitching: NSButton!
    @IBOutlet weak var resetData: NSButton!
    
    //http://stackoverflow.com/questions/12161654/restrict-nstextfield-to-only-allow-numbers
    fileprivate class OnlyIntegerValueFormatter: NumberFormatter {
        
        override func isPartialStringValid(_ partialString: String, newEditingString newString: AutoreleasingUnsafeMutablePointer<NSString?>?, errorDescription error: AutoreleasingUnsafeMutablePointer<NSString?>?) -> Bool {
            
            // Ability to reset your field (otherwise you can't delete the content)
            // You can check if the field is empty later
            if partialString.isEmpty {
                return true
            }
            
            // Optional: limit input length
            /*
             if partialString.characters.count>3 {
             return false
             }
             */
            
            // Actual check
            return Int(partialString) != nil
        }
    }
    
    
    @IBAction func openDataPressed(_ sender: Any) {
        print("Opening folder:", applicationDocumentsDirectory.path)
        NSWorkspace.shared.openFile(applicationDocumentsDirectory.path)
    }
    
    
    override var representedObject: Any? {
        didSet{
            //Update the view, if already loaded.
        }
    }
    
    
}
