//
//  DayFragmentationTimeline.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-31.
//
//  Adapted from Windows version created by André Meyer


class DayFragmentationTimeline: Visualization{
    
    var title: String
    let color = AppConstants.retrospectiveColor
    var Size: String
    let sql: ActivitySQLController
    let timelineZoomFactor = 1
    var _type: [String] = [VisConstants.Day]
    
    required init() throws {
        title = "Timeline: Activities over the Day"
        Size = "Wide"
        sql = try ActivitySQLController()
    }
    
    func getHtml(_ _date: Date, type: String) -> String {
        
        if(!_type.contains(type)){
            return ""
        }
        
        var html = ""
        
        /////////////////////
        // fetch data sets
        /////////////////////
        var orderedTimelineList: [Activity] = sql.GetDayTimelineData(date: _date);
        
        /////////////////////
        // data cleaning
        /////////////////////
        
        // show message if not enough data
        if (orderedTimelineList.count <= 3) // 3 is the minimum number of input-data-items
        {
            html += VisHelper.NotEnoughData()
            return html;
        }
        
        /////////////////////
        // Create HTML
        /////////////////////
        
        html += GetActivityVisualizationContent(activityList: orderedTimelineList)
        
        return html
    }
    
    func getCategories(_ activityList: [Activity]) -> [String]{
        var activities: Set<String> = []
        for activity in activityList{
            activities.insert(activity.activityType)
        }
        return Array(activities).sorted()
    }
    
    func GetHtmlColorForContextCategory(_ category: String) -> String{
        
        switch category{
        case "Browsing":
            return "#FF9333"
        case "Other":
            return "gray"
        case "Instant Messaging":
            return "#12A5F4"
        case "Finder":
            return "#d3d3d3"
        case "Editor":
            return "#99EBFF"
        case "Coding":
            return "#A547D1"
        case "Idle":
            return "white"
        default:
            return "gray"
            
        }
    }
    
    func CreateJavascriptActivityDataList(activityList: [Activity]) -> String{
        var html = ""
        
        var categories = getCategories(activityList)
        let dateFormatter = DateFormatter()
        dateFormatter.dateFormat = "h:mm a"
        
        for category in categories
        {
            var times = ""
            for activityEntry in activityList where activityEntry.activityType == category
            {
                var startTime = activityEntry.startTime * 1000 //javascript time
                var endTime = activityEntry.endTime * 1000
                
                // add data used for the timeline and the timeline hover
                times += "{'starting_time': " + String(startTime) + ", 'ending_time': " + String(endTime)
                times +=    ", 'starting_time_formatted': '" + dateFormatter.string(from: Date(timeIntervalSince1970: activityEntry.startTime))
                times +=    "', 'ending_time_formatted': '" + dateFormatter.string(from: Date(timeIntervalSince1970: activityEntry.endTime))
                times +=    "', 'duration': " + String((activityEntry.duration / 60.0 * 10).rounded()/10)
                times +=    ", 'window_title': '" + activityEntry.title.replacingOccurrences(of: "'", with:"\\'")
                times +=    "', 'app': '" + activityEntry.name.replacingOccurrences(of: "'", with:"\\'")
                times +=    "', 'color': '" + GetHtmlColorForContextCategory(activityEntry.activityType)
                times +=    "', 'activity': '" + activityEntry.activityType + "'}, "
            }
            
            html += "{activity: '" + category + "', times: [" + times + "]}, ";
        }
        
        return html;
    }
    
    func GetActivityVisualizationContent(activityList: [Activity]) -> String{
        var categories = getCategories(activityList)
        var activityTimeline: String = "activityTimeline"
        let defaultHoverText = "Hint: Hover over the timeline to see details.";
        
        var html = ""
        
        /////////////////////
        // CSS
        /////////////////////
        
        html += "<style type='text/css'>\n"
        html += ".axis path,\n"
        html += ".axis line {\n"
        html += "    fill: none;\n"
        html += "    stroke: black;\n"
        html += "    shape-rendering: crispEdges;\n"
        html += "}\n"
        html += ".axis text {\n"
        html += "    font-size: .71em;\n"
        html += "    }\n"
        html += "    .timeline-label {\n"
        html += "        font-size: .71em;\n"
        html += "}\n"
        html += "</style>"
        
        /////////////////////
        // Javascript
        /////////////////////
        
        html += "<script type='text/javascript'>\n"
        html += "var onLoad = window.onload;\n"
        html += "window.onload = function() {\n"
        html += "if (typeof onLoad == 'function') { onLoad(); } "
        
        // create formatted javascript data list
        html += "var data = [" + CreateJavascriptActivityDataList(activityList: activityList) + "]; "
        
        // create color scale
        html += CreateColorScheme(categories);
        
        // width & height
        html += "var itemWidth = 0.98 * document.getElementsByClassName('item Wide')[0].offsetWidth;";
        html += "var itemHeight = 0.15 * document.getElementsByClassName('item Wide')[0].offsetHeight;";
        
        // hover Event (d: current rendering object, i: index during d3 rendering, data: data object)
        var hover = ".hover(function(d, i, data) {\n"
        hover += "console.log(d);\n"
        hover += "console.log(data);\n"
        
        hover += "document.getElementById('hoverDetails').innerHTML = '<span style=\\'font-size:1.2em; color:#007acc;\\'>From ' + d['starting_time_formatted'] + ' to ' + d['ending_time_formatted'] + ' (' + d['duration'] + 'min)</span>' + '<br /><strong>Activity</strong>: <span style=\\'color:' + d['color'] + '\\'>■</span> ' + d['activity'] + '<br /><strong>App</strong>: ' + d['app'] + '<br /><strong>Window title</strong>: ' + d['window_title']\n})"
        
        // mouseout Event
        var mouseout = ".mouseout(function (d, i, datum) { document.getElementById('hoverDetails').innerHTML = '" + defaultHoverText + "'; })";
        
        // define configuration
        html += "var " + activityTimeline + " = d3.timeline().width(" + String(timelineZoomFactor)
        html += " * itemWidth).itemHeight(itemHeight)" + hover
        html += mouseout + ";"; // .colors(colorScale).colorProperty('activity') // .stack()
        html += "var svg = d3.select('#" + activityTimeline
        html += "').append('svg').attr('width', itemWidth).datum(data).call(" + activityTimeline + "); ";
        html += "}; "; // end #1
        html += "</script>";
        
        /////////////////////
        // HTML
        /////////////////////
        
        // show details on hover
        html += "<div style='height:35%; style='align: center'><p id='hoverDetails'>" + defaultHoverText + "</p></div>";
        
        // add timeline
        html += "<div id='" + activityTimeline + "' align='center'></div>";
        
        // add legend
        html += GetLegendForCategories(categoryList: categories);
        
        return html;
    }
    
    func CreateColorScheme(_ categories: [String]) -> String{
        var rangeString = ""
        var activityString = ""
        for category in categories{
            rangeString += "'" + GetHtmlColorForContextCategory(category) + "', "
            activityString += "'" + category + "', "
        }
        
        var html = "var colorScale = d3.scale.ordinal().range([" + rangeString + "]).domain([" + activityString + "]); "
        
        return html
        
    }
    
    func GetLegendForCategories(categoryList: [String]) -> String{
        var html = ""
        html += "<style type='text/css'>\n"
        html += "#legend li { display: inline-block; padding-right: 1em; list-style-type: square; }\n"
        html += "li:before { content: '■ '}\n"
        html += "li span { font-size: .71em; color: black;}\n"
        html += "</style>"
        
        html += "<div><ul id='legend' align='center'>" // style='width:" + visWidth + "px'
        
        for category in categoryList where category != "Idle"{
            html += "<li style='color:" + GetHtmlColorForContextCategory(category) + "'><span>" + category + "</span></li>"
        }
        html += "</ul></div>"
        return html;
    }
    
    
    
}
