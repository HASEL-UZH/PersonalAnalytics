//
//  PreferencesWindowController.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-11.
//

import Foundation

class PreferencesWindowController: NSWindowController {
    
    override func windowDidLoad() {
        super.windowDidLoad()
        repositionWindow()
    }
    
    
    func repositionWindow(){
        if let window = window, let screen = window.screen {
            let screenRect = screen.visibleFrame
            let offsetFromLeftOfScreen: CGFloat = screenRect.maxX * 0.5 - 240
            let offsetFromTopOfScreen: CGFloat = screenRect.maxY * 0.1
            let newOriginY = screenRect.maxY - window.frame.height - offsetFromTopOfScreen
            window.setFrameOrigin(NSPoint(x: offsetFromLeftOfScreen, y: newOriginY))
        }
    }
    
    override func close(){
        
    }
}
