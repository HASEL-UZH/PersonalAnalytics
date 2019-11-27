//
//  HttpServer.swift
//  PersonalAnalytics
//
//  Created by Jonathan Stiansen on 2016-08-07.
//

import Cocoa
import Swifter


class PAHttpServer: NSObject {

    // Constants
    let requiredFields: [String] = ["html_code", "datetime", "title","url"]
        
    var baseurl: String { return "http://localhost:\(Environment.retrospectivePort)/" }
    
    // API constants
    var v1: String { return "\(baseurl)1/" }
    var authorizeURL: String { return "\(v1)authorize" }
    var accessTokenURL: String { return "\(v1)accessToken" }
    var requestTokenURL: String { return "\(v1)requestToken" }
    
    // Data types
    enum AccessReturnType {
        case json, data
    }
    
    var accessReturnType: AccessReturnType  = .data
    
    // Oauth info
    let oauth_token = "accesskey"
    let oauth_token_secret = "accesssecret"
    let valid_key = "key"
    let valid_secret = "key"

    let server = HttpServer()
    // This is used to save requests into database
    var dataController: DataObjectController
    
    init(coreDataController:DataObjectController) {
        dataController = coreDataController
    }
    
    struct paHtml {
        var title = ""
        var url = ""
        var html = ""
        var date:Double = 0.0
    }
    
    func startServer() {
        
        print("Starting server")
        
        func getValueFrom(_ requestBody: [(String, String)], withKey key: String ) -> String? {
            let key_value = requestBody.filter({$0.0 == key }).first
            if key_value != nil {
                return key_value!.1
            } else {
                return nil
            }
        }
        
        server["1/requestToken"] = { request in
            guard request.method == "POST" else {
                return .badRequest(.text("Method must be POST"))
            }
            // TODO check request.headers["authorization"] for consumer key, etc...
            
            let oauth_token = "requestkey"
            let oauth_token_secret = "requestsecret"
            
            return .ok(.text("oauth_token=\(oauth_token)&oauth_token_secret=\(oauth_token_secret)" as String) )
        }
        
        server["1/accessToken"] = { request in
            guard request.method == "POST" else {
                return HttpResponse.badRequest(.text("Method must be POST"))
            }
            // TODO check request.headers["authorization"] for consumer key, etc...
            
            return .ok(.text("oauth_token=\(self.oauth_token)&oauth_token_secret=\(self.oauth_token_secret)" as String) )
        }
                
        server["/stats"] = { request in
            
            guard request.method == "GET" else{
                return HttpResponse.badRequest(.text("Method must be GET"))
            }
            var dashboard: String
            
            do{
                let filepath = Bundle.main.path(forResource: "personalanalytics", ofType: "html")
                
                let contents = try String(contentsOfFile: filepath!, encoding: .utf8)
                dashboard = contents
            }
            catch{
                print("error in server/stats")
                return .internalServerError
            }
            
            let dateFormatter = DateFormatter()
            dateFormatter.dateFormat = "yyyy-MM-dd"
            
            var visualizations: String = ""
            var title = "Your Retrospective for " + dateFormatter.string(from: Date())
                
            
            if(request.queryParams.count == 2){
                let type: String = request.queryParams[0].1
                let dateString: String = request.queryParams[1].1
                (title, visualizations) = self.processQuery(dateString: dateString, type: type)

            }else if(request.queryParams.count == 3){
                let type: String = request.queryParams[0].1
                let dateString: String = request.queryParams[1].1
                let task: String = request.queryParams[2].1
                (title, visualizations) = self.processQuery(dateString: dateString, type: type, task: task)
                
            }
            else{
                //TODO: shouldnt get here, handle this
            }
            
            dashboard = dashboard.replacingOccurrences(of: "{visualizations}", with: visualizations)
            dashboard = dashboard.replacingOccurrences(of : "{title}", with: title)


            
            
            return .ok(.text(dashboard))
        }
        
        server["/styles.css"] = getResource(name:"styles",type:"css")
        server["/jquery.1.11.3.min.js"] = getResource(name:"jquery.1.11.3.min",type:"js")
        server["/d3.min.js"] = getResource(name:"d3.min",type:"js")
        server["/d3.timeline.js"] = getResource(name:"d3.timeline",type:"js")
        server["/c3.min.js"] = getResource(name:"c3.min",type:"js")
        server["/masonry.pkgd.min.js"] = getResource(name:"masonry.pkgd.min",type:"js")
        server["/c3.min.css"] = getResource(name:"c3.min",type:"css")
        server["/tablefilter.js"] = getResource(name:"tablefilter",type:"js")
        server["/tablefilter.css"] = getResource(name:"tablefilter",type:"css")

    
        do{
            try server.start(UInt16(Environment.retrospectivePort)!)
        } catch {
            print(error)
        }
    }
    
    func getResource(name: String, type: String) -> ((HttpRequest) -> HttpResponse){
        
        return shareFile(Bundle.main.path(forResource: name, ofType: type)!)
    }
    
    func processQuery(dateString: String, type: String, task: String? = nil) -> (String, String){
        var title = ""
        var visualizations = ""
        let dateFormatter = DateFormatter()
        dateFormatter.dateFormat = "yyyy-MM-dd"
        
        if(type == "week") {// || type == "week"))
            if let date = dateFormatter.date(from: dateString){
                visualizations = Stats.getVisualizationsWeekly(date: date, type: type)
                title = "Your Retrospective for the Week of " + dateFormatter.string(from: date.startOfWeek!)
                print("success")
            }
            else{
                print("error in dateFormatter/week")
                visualizations = Stats.getVisualizations() //TODO fix this
            }
        }
        else if(type == "day"){
            if let date = dateFormatter.date(from: dateString){
                    visualizations = Stats.getVisualizations(date: date, type: type)
                    title = "Your Retrospective for " + dateString
            }
            else{
                visualizations = Stats.getVisualizations()
            }
        }
        else{
            visualizations = Stats.getVisualizations()
        }
        
        return (title, visualizations)
    }

    
    func stopServer(){
        server.stop()
    }
    
    // This is from https://github.com/httpswift/swifter/issues/148
//     private static func corsDecorate(var response : HttpResponse) -> HttpResponse {
//        response = setHeadersOnResponse(response, headersToAdd:[
//            "Access-Control-Allow-Origin":"*",
//            "Access-Control-Allow-Methods":"GET, POST, OPTIONS",
//            "Access-Control-Allow-Headers":HEADER_KEY_CONTENT_TYPE ])
//        return response
//    }
}
