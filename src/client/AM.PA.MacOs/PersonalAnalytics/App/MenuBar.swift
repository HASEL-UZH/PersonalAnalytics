//
//  Menu.swift
//  PersonalAnalytics
//
//  Created by Roy Rutishauser on 08.06.20.
//

import Foundation
import Sparkle

class MenuBar {
    
    let defaults = UserDefaults.standard
    let statusItem = NSStatusBar.system.statusItem(withLength: -2)
    let menu = NSMenu()
    var pauseItem : NSMenuItem?
    
    func setup(){
        statusItem.menu = menu
        
        defaults.register(defaults: [
            AppConstants.summaryStateKey: 0,
            AppConstants.notificationsPersistKey: true])
        
        // Grabbed this from here: https://github.com/producthunt/producthunt-osx/blob/ab3a0c42cf680a5b0231b3c99a76445cce9abb94/Source/Actions/PHOpenSettingsMenuAction.swift
        let delegate = NSApplication.shared.delegate as! AppDelegate
        
        let retrospectiveItem = NSMenuItem(title: "Show Retrospective", action: #selector(delegate.showRetrospective), keyEquivalent: "R")
        pauseItem = NSMenuItem(title: "Pause Trackers", action: #selector(delegate.togglePause), keyEquivalent: "u")
        let preferencesItem = NSMenuItem(title: "Preferences...", action: #selector(delegate.showPreferences), keyEquivalent: "P")
        let openDataItem = NSMenuItem(title: "Open Data Folder...", action: #selector(delegate.openDataFolder), keyEquivalent: "O")
        let checkUpdatesItem = NSMenuItem(title: "Check for Updates...", action: #selector(SUUpdater.checkForUpdates(_:)), keyEquivalent: "U")
        
        menu.addItem(retrospectiveItem)
        menu.addItem(NSMenuItem.separator())
        menu.addItem(pauseItem!)
        menu.addItem(NSMenuItem.separator())
        menu.addItem(openDataItem)
        menu.addItem(preferencesItem)
        menu.addItem(NSMenuItem.separator())
        menu.addItem(checkUpdatesItem)
        menu.addItem(NSMenuItem.separator())
        menu.addItem(NSMenuItem(title: "Quit", action: #selector(delegate.quit), keyEquivalent: "q"))
        
        statusItem.image = NSImage(named: NSImage.Name(rawValue: Environment.statusBarIcon))
    }
    
    func showMenuPaused() {
        pauseItem!.title = "Resume Trackers"
    }
    
    func showMenuResumed() {
        pauseItem!.title = "Pause Trackers"
    }
}
