//
//  TimeSpentVisualizer.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-31.
//
//  Adapted from Windows version created by André Meyer

import Foundation

class DayTimeSpentVisualization: Visualization {
    
    let sql: ActivitySQLController
    let Size: String = "Wide"
    var title: String = "Details: Time Spent"
    let color: String = AppConstants.retrospectiveColor
    let _type: [String] = [VisConstants.Day]

    required init() throws{
        sql = try ActivitySQLController()
    }

    
    func getHtml(_ _date: Date, type: String) -> String
    {
        
        if(!_type.contains(type)){
            return ""
        }
        var html = ""
        var list: [Activity] = []
    
        /////////////////////
        // fetch & combine data sets
        /////////////////////
        
        list = sql.GetDayTimelineData(date: _date)
        
        /////////////////////
        // sort the list again
        /////////////////////
        let sortedList = list
        
        
        /////////////////////
        // visualize data sets
        ////////////////////
        if (sortedList.count == 0)
        {
            html += VisHelper.NotEnoughData("We couldn't collect any data for this day")
            return html;
        }
        
        // create blank table
        html += "<table id='\(VisHelper.CreateChartHtmlTitle(title: title))'>"
        html += "<thead><tr><th>Type</th><th>Title</th><th>Time spent</th></tr></thead>";
        html += "<tbody>";
        for item in sortedList {
            if(item.activityType == "Idle" || item.activityType == "Finder"){
                continue
            }
            html += "<tr>";
            html += "<td>" + item.activityType + "</td>";
            html += "<td>"
            if(item.activityType == "Other"){
                html += GetFormattedTitle(item.title, name: item.name)
            }
            else if let url = item.URL{
                html += "<a href=" + url + ">"
                html += GetFormattedTitle(item.title) + "</a></td>"
            }
            else{
                html += GetFormattedTitle(item.title) + "</td>";
            }
            html += "<td>" + GetFormattedDuration(item.duration) + "</td>";
            html += "</tr>";
        }
        html += "</tbody>";
        html += "</table>";
        
        /////////////////////
        // create & add javascript
        ////////////////////
        var js =  "<script type='text/javascript'>"
        js += "var tf = new TableFilter('" + VisHelper.CreateChartHtmlTitle(title: title) + "', { base_path: '/', "
        js += "col_0: 'select', col_2: 'none', popup_filters: false, auto_filter: true, auto_filter_delay: 700, highlight_keywords: true, " // filtering options  (column 0: checklist or select)
        js += "alternate_rows: true, " // styling options
        js += "col_widths:[ '5.625em', '40em', '5.625em'], " // fixed columns sizes
        js += "grid_layout: true, grid_width: '51.25em', grid_height: '13.9em', grid_cont_css_class: 'grd-main-cont', grid_tblHead_cont_css_class: 'grd-head-cont', " // styling & behavior of the table
        //+ "extensions: [{name: 'sort', types: [ 'string', 'string', 'null'] }], "
        js += "msg_filter: 'Filtering...', display_all_text: 'Show all', no_results_message: true, watermark: ['', 'Type to filter...', ''], "
        js += "}); " // no content options
        js += "tf.init(); "
        js += "</script>";
        
        html += " " + js;
        
        return html;
    }
    
    fileprivate func GetFormattedDuration(_ duration: Double) -> String
    {
        var formatted = "";
        if (duration >= 3600){
            formatted = String((duration / 3600.0 * 10).rounded()/10) + " hrs";
        }
        else if (duration >= 600){
            formatted = String((duration/60.0).rounded()) + " mins";
        }
        else{
            formatted = String((duration/60.0 * 10).rounded()/10) + " mins"
        }

        return formatted;
    }
    
    fileprivate func GetFormattedTitle(_ titlein: String) -> String
    {
        var title = titlein
        let maxLength = 100
        if (title.count > maxLength)
        {
            let index = title.index(title.startIndex, offsetBy: maxLength-3)

            title = title[..<index] + "..."
        }
        return title
    }
    
    fileprivate func GetFormattedTitle(_ titlein: String, name: String) -> String
    {
        var title = titlein
        let maxLength = 100 - name.count + 3
        if (title.count > maxLength)
        {
            let index = title.index(title.startIndex, offsetBy: maxLength-3)
            
            title = title[..<index] + "..."
        }
        return name + " - " + title
    }
}
