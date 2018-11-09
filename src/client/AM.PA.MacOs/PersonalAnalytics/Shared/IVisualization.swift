//
//  Visualization.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-27.
//

import Foundation

enum VisConstants{
    static let Week: String = "week"
    static let Day: String = "day"
}

protocol IVisualization{
    
    //size of visualization to be rendered
    //Square...
    var Size:String { get }
    var color:String { get }
    var title: String { get }
    var _type: [String] { get }
    
    init() throws
    
    func getHtml(_ _date: Date, type: String) -> String
    
}
