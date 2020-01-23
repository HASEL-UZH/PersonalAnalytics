//
//  DayWeekProductivivityTimeline.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-06-08.
//
//  Adapted from Windows version created by André Meyer


import Foundation

class DayWeekProductivityTimeline: IVisualization {
    
    
    class MyPair
    {
        var Productive: Int
        var Unproductive: Int

        init(prod: Int, unprod: Int)
        {
            Productive = prod;
            Unproductive = unprod;
        }
        
    }

    required init() {
        Size = "Square"
        title = "Top Programs Used during (Un-)Productive Times"
        _type = [VisConstants.Day, VisConstants.Week]
        color = AppConstants.retrospectiveColor
    }

    var title: String
    var _type: [String]
    var color: String
    var Size: String
    let maxNumberOfTopPrograms = 7;
    let _notEnoughDataMsg = "It is not possible to give you insights into your productivity as you didn't fill out the pop-up often enough. Try to fill it out at least 3 times per day."


    
    func getHtml(_ _date: Date, type: String) -> String {
        
        if(!_type.contains(type)){
            return ""
        }
        
        var html = ""
        
        /////////////////////
        // fetch data sets
        /////////////////////
        var prodResponses = UserEfficiencyQueries.GetUserProductivityTimelineData(date: _date, type: type);
        var programsUsed = UserEfficiencyQueries.GetTopProgramsUsedWithTimes(date: _date, type: type, max: maxNumberOfTopPrograms);
        
        if (prodResponses.count < 3 || programsUsed.count < 1)
        {
            html += VisHelper.NotEnoughData(_notEnoughDataMsg);
            return html;
        }
        
        /////////////////////
        // prepare data sets
        /////////////////////
        
        // create & initialize dictionary
        var dict = [String:MyPair]()
        
        for activity in programsUsed{
            if(!dict.keys.contains(activity.name)){
                dict[activity.name] = MyPair(prod: 0, unprod: 0)
            }
        }

        var start: TimeInterval = 0
        
        // populate dictionary
        for response in prodResponses
        {
            // only count if PerceivedProductivity was either unproductive (1-3) or productive (5-7)
            var perceivedProductivity = response.value
            if (perceivedProductivity == 4){
                
            }

            
            var end = response.key
            
            for program in programsUsed{
                if(program.startTime > start && program.endTime < end)
                {
                    if(perceivedProductivity >= 5 && perceivedProductivity <= 7) {
                        dict[program.name]?.Productive += Int(program.duration/60.0)
                    }
                    else if(perceivedProductivity >= 1 && perceivedProductivity <= 3) {
                        dict[program.name]?.Unproductive += Int(program.duration/60.0)
                    }
                }
                else if(program.startTime > start && program.startTime < end){
                    let duration = end - program.startTime
                    if(perceivedProductivity >= 5 && perceivedProductivity <= 7) {
                        dict[program.name]?.Productive += Int(duration/60.0)
                    }
                    else if(perceivedProductivity >= 1 && perceivedProductivity <= 3) {
                        dict[program.name]?.Unproductive += Int(duration/60.0)
                    }
                }
                else if(program.endTime < end && program.endTime > start){
                    let duration = program.endTime - start
                    if(perceivedProductivity >= 5 && perceivedProductivity <= 7) {
                        dict[program.name]?.Productive += Int(duration/60.0)
                    }
                    else if(perceivedProductivity >= 1 && perceivedProductivity <= 3) {
                        dict[program.name]?.Unproductive += Int(duration/60.0)
                    }
                }
            }
            
        }
        
        /////////////////////
        // visualize data sets
        /////////////////////
        
        func sumProductive(_ dict: [String:MyPair]) -> Int{
            var sum = 0
            for entry in dict{
                sum += entry.value.Productive
            }
            return sum
        }
        
        func sumUnproductive(_ dict: [String:MyPair]) -> Int{
            var sum = 0
            for entry in dict{
                sum += entry.value.Unproductive
            }
            return sum
        }
        
        let totalProductive = sumProductive(dict)
        let totalUnproductive = sumUnproductive(dict)
        
        // check if enough data is available
        if (totalProductive == 0 && totalUnproductive == 0)
        {
            html += VisHelper.NotEnoughData(_notEnoughDataMsg);
            return html;
        }
        
        // create blank table
        html +=  "<table id='\(VisHelper.CreateChartHtmlTitle(title: title))'>"
        html += "<thead><tr><th>Program</th><th>Productive</th><th>Unproductive</th></tr></thead>";
        html += "<tbody>";
        for pair in dict
        {
            let programUnproductive: Int = (totalUnproductive == 0) ? 0 : Int((100.0 / Double(totalUnproductive) * Double(pair.value.Unproductive)).rounded())
            let programProductive: Int = (totalProductive == 0) ? 0 : Int((100.0 / Double(totalProductive) * Double(pair.value.Productive)).rounded())
            
            html += "<tr>";
            html += "<td>" + pair.key + "</td>";
            html += "<td style='color:green;'>" + String(programProductive) + "%</td>";
            html += "<td style='color:red;'>" + String(programUnproductive) + "%</td>";
            html += "</tr>";
        }
        html += "</tbody>";
        html += "</table>";
        
        /////////////////////
        // create & add javascript
        ////////////////////
        var js = "<script type='text/javascript'>"
           js += "var tf = new TableFilter('" + VisHelper.CreateChartHtmlTitle(title: title) + "', { base_path: '/', "
           js += "col_widths:[ '12.4em', '6.25em', '6.25em'], " // fixed columns sizes
           js += "col_0: 'none', col_1: 'none', col_2: 'none', "
           js += "alternate_rows: true, " // styling options
           js += "grid_layout: true, grid_width: '25.6em', grid_height: '18em', grid_cont_css_class: 'grd-main-cont', grid_tblHead_cont_css_class: 'grd-head-cont', " // styling & behavior of the table
            //+ "extensions: [{name: 'sort', types: [ 'string', 'number', 'number'] }], "
           js += "}); " // no content options
           js += "tf.init(); "
           js += "</script>";
        
        html += " " + js;
        
        return html;
    }

    
}
