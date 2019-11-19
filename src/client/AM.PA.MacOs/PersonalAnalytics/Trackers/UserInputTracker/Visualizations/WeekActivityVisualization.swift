//
//  WeekActivityVisualization.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-07-26.
//

import Foundation

class WeekActivityVisualization: IVisualization{
    let Size: String
    let color = AppConstants.retrospectiveColor
    var title: String
    var _type = [VisConstants.Week]

    required init() throws {
        Size = "Square"
        title = "Activity Breakdown for this Week"
    }
    
    func getHtml(_ _date: Date, type: String) -> String {
        if(!_type.contains(type)){
            return ""
        }
        
        var html = ""
        
        /////////////////////
        // CSS
        /////////////////////
        html += "<style type='text/css'>";
        html += ".c3-line { stroke-width: 2px; }";
        html += ".c3-grid text, c3.grid line { fill: gray; }";
        html += "</style>";

        /////////////////////
        // HTML
        /////////////////////
        
        html += "<div id='" + VisHelper.CreateChartHtmlTitle(title: title) + "' style='height:75%;' align='center'></div>"
        html += "<p style='text-align: center; font-size: 0.66em;'>Hint: placeholder.</p>" //TODO: replace placeholder
        
        /////////////////////
        // JS
        /////////////////////
        
        let dates = "'Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'"
        //TODO: let yTicks = ......
        
        //get the user data
        //time spent working each day
        //breakdown of time spent each day (consuming vs creating information)
        let data = "columns: [['Creating', 4.5, 7, 8, 2, 0, 1,0], ['Consuming', 2,1,0,3,2,2,1]], type:'bar', groups:[['Creating','Consuming']]"
        //let data: String = "x: 'timeAxis', columns: [['timeAxis', " + timeAxis + "], ['User_Input_Level', " + userInputFormattedData + " ] ], type: 'area', colors: { " + colors + " }, axis: { 'PerceivedProductivity': 'y' }"
        let grid: String = "y: { lines: [ { value: 0 } ] } "
        let axis: String = "x: { type: 'category', categories: [" + dates + "]  }, y: {tick:{values:[1,2,3,4,5,6,7,8]}}"//TODO:
        let parameters: String = " bindto: '#" + VisHelper.CreateChartHtmlTitle(title: title) + "', data: { " + data + " }, padding: { left: 0, right: 0, bottom: -10, top: 0}, legend: { show: true }, axis: { " + axis + " }, grid: { " + grid + " }, tooltip: { show: true }, point: { show: false }"
        // padding: { left: 0, right: 0 },
        
        html += "<script type='text/javascript'>";
        html += "var " + VisHelper.CreateChartHtmlTitle(title: title) + " = c3.generate({ " + parameters + " });";
        html += "</script>";
        
        return html;

        

    }
    
}
