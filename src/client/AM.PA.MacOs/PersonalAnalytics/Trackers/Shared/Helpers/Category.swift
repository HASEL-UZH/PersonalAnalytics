//
//  Category.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-31.
//

import Foundation

class Category{
    let name: String
    let contents: Set<String>
    
    init(name: String, contents: [String]){
        self.name = name
        self.contents = Set<String>(contents)
    }
    
    init(name: String, contents: Set<String>){
        self.name = name
        self.contents = contents
    }

    
    func contains(_ pattern: String) -> Bool{
        return contents.contains(pattern)
    }
}
