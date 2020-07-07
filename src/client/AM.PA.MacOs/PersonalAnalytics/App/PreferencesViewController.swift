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
        let urls = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)
        return urls[urls.count-1].appendingPathComponent(Environment.appSupportDir)
    }()
    
    @IBOutlet weak var keyboardTrackingStatusLabel: NSTextField!
    @IBOutlet weak var resourceTrackingStatusLabel: NSTextField!
    @IBOutlet weak var safariURLTrackingStatusLabel: NSTextField!
    @IBOutlet weak var chromeURLTrackingStatusLabel: NSTextField!
    
    @IBOutlet weak var resourceTrackingButton: NSButton!
    
    override func viewDidLoad() {
        super.viewDidLoad()
        
        if isKeyboardTrackingEnabled() {
            keyboardTrackingStatusLabel.stringValue = "enabled"
        }
        
        if #available(macOS 10.15, *){
            resourceTrackingButton.isEnabled = true
        }
        
        if isWindowNameTrackingEnabled() {
            resourceTrackingStatusLabel.stringValue = "enabled"
        }
        
        if isURLTrackingEnabled(browser: "Safari") {
            safariURLTrackingStatusLabel.stringValue = "enabled"
        }
        
        if isURLTrackingEnabled(browser: "Google Chrome") {
            chromeURLTrackingStatusLabel.stringValue = "enabled"
        }
    }
    
    func areAllTrackersEnabled() -> Bool {
        return isWindowNameTrackingEnabled() && isKeyboardTrackingEnabled() && isURLTrackingEnabled(browser: "Safari") && isURLTrackingEnabled(browser: "Google Chrome")
    }
    
    func isKeyboardTrackingEnabled() -> Bool {
         return AXIsProcessTrusted()
    }
    
    func isWindowNameTrackingEnabled() -> Bool {
        // 22.4.2020 - the latest privacy measures in Catalina require screen capture permission in order to access the window name of an application. See: https://stackoverflow.com/questions/56597221/detecting-screen-recording-settings-on-macos-catalina
        if #available(macOS 10.15, *){
            guard let windows = CGWindowListCopyWindowInfo([.optionOnScreenOnly], kCGNullWindowID) as? [[String: AnyObject]] else { return false }
            return windows.allSatisfy({ window in
                let windowName = window[kCGWindowName as String] as? String
                return windowName != nil
            })
        }
        else{
            return true
        }
    }
    
    func isURLTrackingEnabled(browser: String) -> Bool {
        var script = ""
        if browser == "Google Chrome" {
            script = "tell application \"Google Chrome\" to return URL of active tab of front window"
        } else if browser == "Safari" {
            script = "tell application \"Safari\" to return URL of front document"
        } else {
            return false
        }
        
        let scriptObject = NSAppleScript(source: script)
        var error: NSDictionary?
        scriptObject?.executeAndReturnError(&error)
        if (error != nil) {
            return false
        }
        return true
    }
       
    @IBAction func openDataPressed(_ sender: Any) {
        print("Opening folder:", applicationDocumentsDirectory.path)
        NSWorkspace.shared.openFile(applicationDocumentsDirectory.path)
    }
    
    @IBAction func goToAccessibilityPane(_ sender: Any) {
           let script = """
                        tell application "System Preferences"
                        reveal anchor "Privacy_Accessibility" of pane id "com.apple.preference.security"
                        activate
                        end tell
                        """

           let scriptObject = NSAppleScript(source: script)
           scriptObject?.executeAndReturnError(nil)
       }
       
       @IBAction func goToScreenRecordingPane(_ sender: Any) {
           // 22.4.2020 this should trigger a system warning and lead the user
           // to the Security/Privacy --> Screen Recording List with PA in it.
           CGDisplayCreateImage(CGMainDisplayID())
           
           // 22.4.2020 I could not find a way to jump to the "Screen Recording" pane directly
           let script = """
                        tell application "System Preferences"
                        reveal anchor "Privacy" of pane id "com.apple.preference.security"
                        activate
                        end tell
                        """

           let scriptObject = NSAppleScript(source: script)
           scriptObject?.executeAndReturnError(nil)
       }
       
       @IBAction func goToAutomationPane(_ sender: Any) {
           // 22.4.2020 I could not find a way to jump to the "Automation" pane directly
          let script = """
                       tell application "System Preferences"
                       reveal anchor "Privacy" of pane id "com.apple.preference.security"
                       activate
                       end tell
                       """

          let scriptObject = NSAppleScript(source: script)
          scriptObject?.executeAndReturnError(nil)
      }
    
    override var representedObject: Any? {
        didSet{
            //Update the view, if already loaded.
        }
    }
}
