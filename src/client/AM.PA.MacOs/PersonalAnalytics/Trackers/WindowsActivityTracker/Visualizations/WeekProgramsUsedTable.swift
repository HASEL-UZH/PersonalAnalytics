//
//  WeekProgramsUsedTable.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-06-06.
//
//  Adapted from Windows version created by André Meyer


import Foundation

class WeekProgramsUsedTable: IVisualization{

    
    fileprivate class Programs{
        var Days: [Double]
        var Total: Double
        
        init(dayNumber: Int, dur: Double){
            Total = dur
            Days = [Double](repeating: 0.0, count: 7)
            Days[dayNumber] = dur
        }
    }
    
    var Size: String
    var title: String
    var _type: [String] = [VisConstants.Week]
    var color: String
    
    required init() {
        Size = "Square"
        color = AppConstants.retrospectiveColor
        title = "Top Programs Used During the Week"
    }
    
    func getHtml(_ _date: Date, type: String) -> String {
        
        if(!_type.contains(type)){
            return ""
        }
        
        var html: String = ""
        
        
        /////////////////////
        // fetch data sets
        /////////////////////
        let programUsePerDay: [String:Programs] = GetProgramUsePerDay(_date: _date);
        let totalHoursPerDay = GetTotalHoursPerDay(programs: programUsePerDay);
        
        if (programUsePerDay.count < 1)
        {
            html += VisHelper.NotEnoughData()
            return html;
        }
        
        
        /////////////////////
        // HTML
        /////////////////////
        
        html += "<table id='\(VisHelper.CreateChartHtmlTitle(title: title))'>"
        html += GetTableHeader();
        html += "<tbody style='overflow:auto;'>";
        for prog in programUsePerDay
        {
            html += "<tr>";
            html += "<td>" + prog.key + "</div></td>";
            var i = 0
            for index in prog.value.Days
            {
                html += GetTableRow(perc: GetPercentage(programs: index, total: totalHoursPerDay[i]))
                i += 1
            }
            html += "</tr>";
        }
        html += "</tbody>";
        html += "</table>";
        
        
        /////////////////////
        // create & add javascript
        ////////////////////
        var js: String = "<script type='text/javascript'>"
         js += "var tf = new TableFilter('" + VisHelper.CreateChartHtmlTitle(title: title) + "', { base_path: '/', "
         js += "col_widths:[ '9.6875em', '2.1875em', '2.1875em', '2.1875em', '2.1875em', '2.1875em', '2.1875em', '2.1875em'], " // fixed columns sizes
         js += "col_0: 'none', col_1: 'none', col_2: 'none', col_3: 'none', col_4: 'none', col_5: 'none', col_6: 'none', col_7: 'none', "
         js += "alternate_rows: true, " // styling options
         js += "grid_layout: true, grid_width: '25.6em', grid_height: '16.5em', grid_cont_css_class: 'grd-main-cont', grid_tblHead_cont_css_class: 'grd-head-cont', tbl_cont_css_class: 'grd-cont'" // styling & behavior of the table
         js += "}); " // no content options
         js += "tf.init(); "
         js += "</script>";
        
        html += " " + js;
 
        return html;
    }
    
    func GetPercentage(programs: Double, total: Double) -> Double
    {
        if (total == 0){
            return 0
        }
        return 1.0 / total * programs;
    }
    
    func GetTableHeader() -> String
    {
        var header: String = "<thead><tr><th>Program</th>";
        header += "<th>  Sun</th>"
        header += "<th>  Mon</th>"
        header += "<th>  Tue</th>"
        header += "<th>  Wed</th>"
        header += "<th>  Thu</th>"
        header += "<th>  Fri</th>"
        header += "<th>  Sat</th>"
        header += "</tr></thead>";
        return header;
    }
    
    func GetTableRow(perc: Double) -> String
    {
        let colorWithWeight = perc * 2;
        let percentage = ""; // Math.Round(GetPerc(prog.Value.Days[i], totalHoursPerDay[i]) * 100, 0) + "%";
        
        var result: String = "<td style='background-color:rgba(0,122,203, " + String(colorWithWeight)
        result += ");'>" + percentage
        result += "</td>";
    
        return result
    }
    
    fileprivate func GetProgramUsePerDay(_date: Date) -> [String: Programs]
    {
        var dict = [String:Programs]()
        var first = _date.startOfWeek!
        let last = _date.endOfWeek!
        
        // fetch & format data
        while (first < last)
        {
            let programsDay = WindowsActivityQueries.GetActivityPieChartData(date: first)
            let dayNumber = getDayOfWeek(first) - 1
            
            for program in programsDay
            {
                let process = program.key
                let dur = program.value
                
                if(dict[process] == nil){
                    dict[process] = Programs(dayNumber: dayNumber, dur: dur)
                }
                
                dict[process]?.Days[dayNumber] += dur
            }
            
            first = first.addingTimeInterval(24*60*60)
        }
        
        // sort & filter
        return dict
    }
    
    fileprivate func GetTotalHoursPerDay(programs: [String: Programs]) -> [Double]
    {
        var total = [Double](repeating: 0.0, count: 7)
        
        var i = 0
        while(i < 7) {
            for program in programs{
                total[i] += program.value.Days[i]
            }
            i += 1
        }
        return total
    }
    
    //https://stackoverflow.com/questions/25533147/get-day-of-week-using-nsdate-swift
    func getDayOfWeek(_ todayDate: Date) -> Int {
        let myCalendar = Calendar(identifier: .gregorian)
        let weekDay = myCalendar.component(.weekday, from: todayDate)
        return weekDay
    }
}
