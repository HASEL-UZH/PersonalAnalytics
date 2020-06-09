//
//  KeystrokeController.swift
//  PersonalAnalytics
//
//  Created by Jonathan Stiansen on 2015-10-19.
//



import Cocoa

//NOTE: This class requires accessibility privileges to function properly.
class KeystrokeController {
    
    let navigationKeyCodes: Set<UInt16> = [123,124,125,126, 0x77, 0x74, 0x79] //left, right, up, down, pagedown, pageup, home, end, next
    let deleteKeyCodes: Set<UInt16> = [51,0x75]
    
    fileprivate var globaEventMonitor: AnyObject?
    fileprivate var keystrokeList = [KeyStrokeEvent]()

    init(){
        
        if(!AXIsProcessTrusted()){
            print("KeystrokeController requires accessibility privileges to function properly")
        }
        
        self.globaEventMonitor = NSEvent.addGlobalMonitorForEvents(matching: [NSEvent.EventTypeMask.keyDown], handler:  self.recordKeyboardKeys) as AnyObject?
    }

    func recordKeyboardKeys(_ keyEvent:NSEvent){
        switch keyEvent.type {
            // All events to record
        case .keyDown:
            if(deleteKeyCodes.contains(keyEvent.keyCode)){
                keystrokeList.append(KeyStrokeEvent(type: .Backspace))
            }
            else if(navigationKeyCodes.contains(keyEvent.keyCode)){
                keystrokeList.append(KeyStrokeEvent(type: .Navigate))
            }
            else{
                keystrokeList.append(KeyStrokeEvent(type: .Key))
            }

        default:
            print("\(keyEvent.modifierFlags)Somehow it made it here, event is: \(keyEvent)")
        }
    }

    
    func getValues() -> (Int, Int, Int){
        let keyList = keystrokeList.filter { $0.type == .Key }
        let navigateList = keystrokeList.filter { $0.type == .Navigate }
        let deleteList = keystrokeList.filter { $0.type == .Backspace }
        
        return (keyList.count, navigateList.count, deleteList.count)
    }
    
    func reset(){
        keystrokeList.removeAll()
    }
    
    func saveDetailedKeyStrokes() {
        UserInputQueries.saveKeystrokes(keystrokes: keystrokeList)
    }
    
    deinit{
        if let monitor = self.globaEventMonitor {
            NSEvent.removeMonitor(monitor)
        }
    }
}
