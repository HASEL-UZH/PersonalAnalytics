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
    
    
    fileprivate var globaEventlMonitor: AnyObject?
    fileprivate var deleteCount:Int
    fileprivate var navigateCount:Int
    fileprivate var keyCount:Int
   
    init(){
        
        if(!AXIsProcessTrusted()){
            print("KeystrokeController requires accessibility privileges to function properly")
        }

        self.deleteCount = 0
        self.navigateCount = 0
        self.keyCount = 0
        
        self.globaEventlMonitor = NSEvent.addGlobalMonitorForEvents(matching: [NSEvent.EventTypeMask.keyDown], handler:  self.recordKeyboardKeys) as AnyObject?
        
    }

    func recordKeyboardKeys(_ keyEvent:NSEvent){
        switch keyEvent.type{
            // All events to record
        case .keyDown:
            if(deleteKeyCodes.contains(keyEvent.keyCode)){
                deleteCount += 1
            }
            else if(navigationKeyCodes.contains(keyEvent.keyCode)){
                navigateCount += 1
            }
            else{
                keyCount += 1
            }

        default:
            print("\(keyEvent.modifierFlags)Somehow it made it here, event is: \(keyEvent)")
        }
    }

    func getValues() -> (Int, Int, Int){
        return(keyCount, navigateCount, deleteCount)
    }
    
    func reset(){
        self.keyCount = 0
        self.navigateCount = 0
        self.deleteCount = 0
    }

    
    deinit{
        if let monitor = self.globaEventlMonitor {
            NSEvent.removeMonitor(monitor)
        }
    }
}
