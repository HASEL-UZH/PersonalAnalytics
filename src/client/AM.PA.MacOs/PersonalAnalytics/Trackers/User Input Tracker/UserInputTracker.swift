//
//  UserInputTracker.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-03.
//

import Foundation
class UserInputTracker: Tracker{
    
    var viz: [Visualization] = []
    let type: String = "UserInput"


    //singleton instance (WHY????)
    //static let sharedInstance: UserInputTracker = UserInputTracker()
    
    let dataController : DataObjectController
    var clickCount: Int
    var distance: Int
    var scrollDelta: Int
    var time: Date
    var keyCount: Int
    var navigateCount: Int
    var deleteCount: Int
    let mouseController: MouseActionController
    let keystrokeController: KeystrokeController
    var inputTimer: Timer?
    let inputInterval: TimeInterval = TimeInterval(60) // seconds
    var isPaused = false

    
    required init(){
        self.dataController = DataObjectController.sharedInstance
        mouseController = MouseActionController()
        keystrokeController = KeystrokeController()
        self.time = Date()
        self.clickCount = 0
        self.distance = 0
        self.keyCount = 0
        self.navigateCount = 0
        self.deleteCount = 0
        self.scrollDelta = 0
        
        do{
            viz.append(try ActivityVisualization())
        }
        catch{
            print(error)
        }
        
        inputTimer = Timer.scheduledTimer(timeInterval: inputInterval, target: self,selector: #selector(save), userInfo: nil, repeats: true)
        inputTimer?.tolerance = 5

        
    }
    
    func pause(){
        inputTimer?.invalidate()
        keystrokeController.reset()
        mouseController.reset()
        isPaused = true
    }
    
    func resume(){
        if(isPaused == false){
            return
        }
        inputTimer = Timer.scheduledTimer(timeInterval: inputInterval, target: self,
                                          selector: #selector(save), userInfo: nil, repeats: true)
        inputTimer?.tolerance = 5
        isPaused = false
    }

    @objc func save(){
        (clickCount, scrollDelta, distance) = mouseController.getValues()
        mouseController.reset()
        (keyCount, navigateCount, deleteCount) = keystrokeController.getValues()
        keystrokeController.reset()
        time = Date()
        dataController.saveUserInput(aggregatedInput: self)
    }
    
    deinit{
        save()
    }
    


}
