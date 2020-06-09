//
//  MouseActionController.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-03.
//

import Foundation

class MouseActionController{

    fileprivate var globalEventMonitor: AnyObject?
    fileprivate var lastMouseLocation: NSPoint
    
    fileprivate var mouseClickList = [MouseClickEvent]()
    fileprivate var mouseMovementList = [MouseMovementSnapshot]()
    fileprivate var mouseScrollList = [MouseScrollSnapshot]()

    init(){
        self.lastMouseLocation = NSEvent.mouseLocation //add initial mouse location
        self.globalEventMonitor = NSEvent.addGlobalMonitorForEvents(matching: [NSEvent.EventTypeMask.leftMouseDown, NSEvent.EventTypeMask.rightMouseDown, NSEvent.EventTypeMask.otherMouseDown, NSEvent.EventTypeMask.mouseMoved, NSEvent.EventTypeMask.scrollWheel], handler: self.recordActions) as AnyObject?
    }
    
    func recordActions(mouseEvent:NSEvent){
        let currentLocation = NSEvent.mouseLocation

        switch mouseEvent.type {
        case .leftMouseDown:
            mouseClickList.append(MouseClickEvent(button: .Left, location: currentLocation))
        case .rightMouseDown:
            mouseClickList.append(MouseClickEvent(button: .Right, location: currentLocation))
        case .otherMouseDown:
            // middle button
            if (mouseEvent.buttonNumber == 2) {
                mouseClickList.append(MouseClickEvent(button: .Middle, location:  currentLocation))
            }
            // Xbutton1 (thumb button on the side)
            else if (mouseEvent.buttonNumber == 3) {
                mouseClickList.append(MouseClickEvent(button: .XButton1, location: currentLocation))
            }
            // Xbutton2 (thumb button on the side)
            else if (mouseEvent.buttonNumber == 4) {
                mouseClickList.append(MouseClickEvent(button: .XButton2, location: currentLocation))
            }
        case .mouseMoved:
            let distance = calculateDistance(a: currentLocation, b: lastMouseLocation)
            lastMouseLocation = currentLocation
            mouseMovementList.append(MouseMovementSnapshot(movedDistance: distance, location: currentLocation))
        case .scrollWheel:
            let scroll = Int(abs(mouseEvent.scrollingDeltaY))
            mouseScrollList.append(MouseScrollSnapshot(scrollDelta: scroll, location: currentLocation))
        default:
            print("Whoops! how did we get here")
        }
    }
    
    func calculateDistance(a: NSPoint, b: NSPoint) -> Int{
        return Int(sqrt(pow(b.x-a.x,2) + pow(b.y-a.y,2)))
    }
    
    func reset(){
        mouseClickList.removeAll()
        mouseScrollList.removeAll()
        mouseMovementList.removeAll()
    }
    
    func getValues() -> (Int, Int, Int, Int, Int){
        let leftClicks = mouseClickList.filter { $0.button == .Left }
        let rightClicks = mouseClickList.filter { $0.button == .Right }
        let otherClicks = mouseClickList.filter { $0.button == .Middle || $0.button == .XButton1 || $0.button == .XButton2 }
        
        let scrollDelta = mouseScrollList.reduce(0, { sum, scroll in
            sum + scroll.scrollDelta
        })
        
        let mouseMovement = mouseMovementList.reduce(0, { sum, move in
            sum + move.movedDistance
        })
        
        return (leftClicks.count, rightClicks.count, otherClicks.count, scrollDelta, mouseMovement)
    }
    
    func saveDetailedMouseInputs() {
        UserInputQueries.saveMouseClicks(clicks: mouseClickList)
        UserInputQueries.saveMouseScrolls(scrolls: mouseScrollList)
        UserInputQueries.saveMouseMovements(movements: mouseMovementList)
    }
    
    deinit{
        if let monitor = self.globalEventMonitor {
            NSEvent.removeMonitor(monitor)
        }
    }
}
