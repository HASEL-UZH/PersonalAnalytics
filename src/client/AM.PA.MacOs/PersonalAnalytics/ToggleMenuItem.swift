//
//  ToggleMenuItem.swift
//  Collector
//
//  Created by Jonathan Stiansen on 2015-09-28.
//  Copyright © 2015 Jonathan Stiansen. All rights reserved.
//

import Cocoa
/**
* A simple toggleable menuitem
*/
class ToggleMenuItem: NSMenuItem {
    
    var toggled: Bool
    let selector1, selector2: Selector
    let key1, key2: String 
    let title1, title2: String
    
    convenience init(firstTitle: String, firstAction: Selector, keyEquivalent1: String,
                     secondTitle: String, secondAction: Selector, keyEquivalent2: String){
            self.init(title: firstTitle, action: firstAction, keyEquivalent: keyEquivalent1)
            self.toggled = false
    }
    
    override init(title aString: String, action aSelector: Selector, keyEquivalent charCode: String) {
        super.init(title: aString, action: aSelector, keyEquivalent: charCode)
        
    }

    required init?(coder aDecoder: NSCoder) {
        super.init(coder: aDecoder)
    }
}
