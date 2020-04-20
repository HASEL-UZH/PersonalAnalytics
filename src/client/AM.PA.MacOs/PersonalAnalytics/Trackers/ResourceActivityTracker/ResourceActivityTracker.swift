//
//  ResourceActivityTracker.swift
//  PersonalAnalytics
//
//  Created by Roy Rutishauser on 04.02.20.
//

import Foundation
import CoreGraphics
import Quartz

class ResourceActivityTracker: ITracker{
    
    var name = ResourceActivitySettings.Name
    var isRunning = true
    
    
    init() {
        
        NotificationCenter.default.addObserver(self, selector: #selector(self.onActiveApplicationChange(_:)), name: NSNotification.Name(rawValue: "activeApplicationChange"), object: nil)

        try? EonilFSEvents.startWatching(
            paths: [NSHomeDirectory()],
            for: ObjectIdentifier(self),
            with: { event in
                
                let flags = event.flag!
                
                // not interested in caches, logs and other system related stuff
                if event.path.contains("Library/") {
                    return
                }
                
                // only interested in files, not in symlinks or directories
                if !flags.contains(EonilFSEventsEventFlags.itemIsFile) {
                    return
                }
                
                if flags.contains(EonilFSEventsEventFlags.itemChangeOwner) {
//                    print("changed owner", event.path)
                    return
                }
                
                //  if event.path.contains("roy/Desktop") {
                //                    let attr = try? FileManager.default.attributesOfItem(atPath: event.path)
                //                    print(event.path)
                //                    print(attr)
                //                    print(flags)
                //                    print("###")
                //                }
                //                return
                                
                
                do {
                    // this throws if the file still no longer exists at this point.
                    // It might have already been deleted by the system...
                    let attr = try FileManager.default.attributesOfItem(atPath: event.path)
                    //                        print("created", event.path)
                    ResourceActivityQueries.saveResourceActivity(date: attr[FileAttributeKey.modificationDate] as! Date, path: event.path, flags: flags)

                } catch {
                    //print(error)
                }
                
                return
 
                
                // MARK: - Created
                if flags.contains(EonilFSEventsEventFlags.itemCreated) {
                    do {
                        // this throws if the file still no longer exists at this point.
                        // It might have already been deleted by the system...
                        let attr = try FileManager.default.attributesOfItem(atPath: event.path)
//                        print("created", event.path)
                    } catch {
                        //print(error)
                    }
                }

                // MARK: - Removed
                else if flags.contains(EonilFSEventsEventFlags.itemRemoved) {
//                    print("removed", event.path)
                }
                    
                // MARK: - Modified
                //TODO: why is itemRenamed in the condition and not only itemXattrMod?
                else if flags.contains(EonilFSEventsEventFlags.itemXattrMod) && flags.contains(EonilFSEventsEventFlags.itemRenamed) {
                    do {
                        let attr = try FileManager.default.attributesOfItem(atPath: event.path)
                        if Date().addingTimeInterval(-5.0 * 60.0) < (attr[FileAttributeKey.modificationDate] as! Date) || (attr[FileAttributeKey.busy] as? Int) != nil {
                            print("modified", event.path)
                        }
                    } catch {
                        print("error getting file date")
                    }
                }
                    
                // MARK: - InodeMeta
                else if flags.contains(EonilFSEventsEventFlags.itemInodeMetaMod) {
                    print("InodeMeta", event.path)
                }
                    
                // MARK: - Renamed
                else if flags.contains(EonilFSEventsEventFlags.itemRenamed) {
                    do {
                        // this checks if the file still exists at this point, might have already
                        // been deleted by the system...
                        let attr = try FileManager.default.attributesOfItem(atPath: event.path)
                        print("renamed (new name)", event.path)
                    } catch {
                        print("renamed (old name)", event.path)
                    }
                }
        })
    }
    
    func stop() {
        isRunning = false
    }
    
    func start() {
        isRunning = true
    }
    
    func createDatabaseTablesIfNotExist() {
        ResourceActivityQueries.createDatabaseTablesIfNotExist()
    }
    
    func updateDatabaseTables(version: Int) {}
    
    @objc func onActiveApplicationChange(_ notification: NSNotification) {
        if let activeApp = notification.userInfo?["activeApplication"] as? NSRunningApplication {
            let appName = activeApp.localizedName ?? ""
            var resourcePath: String
            if appName == "Google Chrome" || appName == "Safari" {
                resourcePath = getWebsiteOfActiveBrowser(appName)
            } else {
                resourcePath = getResourceOfActiveApplication(activeApp: activeApp)
            }
            
            ResourceActivityQueries.saveResourceOfApplication(date: Date(), path: resourcePath, process: appName)
        }
    }
    
    func getResourceOfActiveApplication(activeApp: NSRunningApplication) -> String {
        
        // get resouce associated with active application
        var filePath: String?
        var result = [AXUIElement]()
        var windowList: AnyObject? // [AXUIElement]
        let appRef = AXUIElementCreateApplication(activeApp.processIdentifier)
        if AXUIElementCopyAttributeValue(appRef, "AXWindows" as CFString, &windowList) == .success {
            result = windowList as! [AXUIElement]
        }

        if !result.isEmpty {
            var docRef: AnyObject?
            if AXUIElementCopyAttributeValue(result.first!, "AXDocument" as CFString, &docRef) == .success {
                filePath = docRef as? String
            }
        }
        
        return filePath ?? ""
    }
    
    // works with "Google Chrome" and "Safari"
    func getWebsiteOfActiveBrowser(_ browser: String) -> String {
        
        // helper function
        func runApplescript(_ script: String) -> String?{
            var error: NSDictionary?
            if let scriptObject = NSAppleScript(source: script) {
                if let output: NSAppleEventDescriptor = scriptObject.executeAndReturnError(
                    &error) {
                        if let URL = output.stringValue {
                            return URL // This is the important outcome, the rest don't matter
                        }
                } else if (error != nil) {
                    print("error: \(error)")
                }
            }
            return nil
        }
        
        switch browser {
            case "Google Chrome":
                // let titleReturn = runApplescript("tell application \"Google Chrome\" to return title of active tab of front window")
                let url = runApplescript("tell application \"Google Chrome\" to return URL of active tab of front window")
                return url ?? ""
                
            case "Safari":
                //  let titleReturn = runApplescript("tell application \"Safari\" to return name of front document")
                let url = runApplescript("tell application \"Safari\" to return URL of front document")
                return url ?? ""
            default:
                break
        }
        return ""
    }
}
