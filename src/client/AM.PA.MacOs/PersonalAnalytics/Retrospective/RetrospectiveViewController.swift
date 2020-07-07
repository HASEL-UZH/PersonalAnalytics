//
//  TestViewController.swift
//  PersonalAnalytics
//
//  Created by Chris Satterfield on 2017-05-23.
//

import Foundation
import WebKit

//https://stackoverflow.com/questions/27890144/setting-backgroundcolor-of-custom-nsview
extension NSView {
    
    var backgroundColor: NSColor? {
        
        get {
            if let colorRef = self.layer?.backgroundColor {
                return NSColor(cgColor: colorRef)
            } else {
                return nil
            }
        }
        
        set {
            self.wantsLayer = true
            self.layer?.backgroundColor = newValue?.cgColor
        }
    }
}

class RetrospectiveViewController: NSViewController, WKNavigationDelegate {
    
    @IBAction func loadButtonPressed(_ sender: Any) {
        let date = datePicker.dateValue
        if(date != activeDate){
            print(date)
            loadPage(date)
            activeDate = date
        }
    }
    @IBAction func toggleWeekly(_ sender: Any) {
        if(isTaskFocused){
            isTaskFocused = false
        }
        else{
            isWeekly = !isWeekly
        }
        loadPage(activeDate)
        toggleButton.title = "Switch to \(isWeekly ? "Daily" : "Weekly") Retrospection"
    }
    
    var isTaskFocused: Bool = false
    
    func loadPage(_ _date: Date){
        
        var date = _date
        
        if(date > Date()){
            date = Date()
            activeDate = Date()
            datePicker.dateValue = activeDate
        }
        let type: String
        let dateFormatter = DateFormatter()
        if(isWeekly){
            type = "week"
        }else{
            type = "day"
        }
        
        dateFormatter.dateFormat = "yyyy-MM-dd"
        let dateString = dateFormatter.string(from: date)
        
        let urlString: String? = "http://127.0.0.1:\(Environment.retrospectivePort)/stats?type=\(type)&date=\(dateString)"

            let url = URL(string: urlString!)!
            webView.load(URLRequest(url:url))
        
    }

    @IBOutlet weak var toggleButton: NSButton!
    @IBOutlet weak var titleBar: NSView!
    @IBOutlet weak var webView: WKWebView!
    @IBOutlet weak var datePicker: NSDatePicker!
    @IBOutlet weak var retrospectiveTitle: NSTextField!
    
    var isWeekly = false
    var currentURL: String?
    var activeDate: Date = Date()
    let backColor = NSColor(calibratedRed: 0.889, green: 0.889, blue: 0.889, alpha: 1)
    

    override func viewWillAppear() {
        print("starting")
        loadPage(Date())
    }
    
    override func viewDidLoad(){
        super.viewDidLoad()
        webView.navigationDelegate = self
        webView.isHidden = true
        webView.addObserver(self, forKeyPath: #keyPath(WKWebView.estimatedProgress), options: .new, context: nil)
        repositionWindow()
        self.view.backgroundColor = backColor
        titleBar.backgroundColor = NSColor(calibratedRed: 0.13725, green: 0.470589, blue: 0.788235, alpha: 1)
        datePicker.dateValue = Date()
        activeDate = datePicker.dateValue
        //toggleButton.isEnabled = false
    }
    
    func repositionWindow(){
        if let window = self.view.window, let screen = window.screen {
            let screenRect = screen.visibleFrame
            let offsetFromLeftOfScreen: CGFloat = screenRect.maxX * 0.5 - 540
            let offsetFromTopOfScreen: CGFloat = screenRect.maxY * 5 - 275
            let newOriginY = screenRect.maxY - window.frame.height - offsetFromTopOfScreen
            window.setFrameOrigin(NSPoint(x: offsetFromLeftOfScreen, y: newOriginY))
        }
    }
    
    @IBOutlet weak var loadAnimation: NSProgressIndicator!
    override func observeValue(forKeyPath keyPath: String?, of object: Any?, change: [NSKeyValueChangeKey : Any]?, context: UnsafeMutableRawPointer?) {
        if keyPath == "estimatedProgress"{
            if(webView.estimatedProgress == 1.0){
                loadAnimation.stopAnimation(nil)
                loadAnimation.isHidden = true
                webView.isHidden = false
                loadAnimation.resignFirstResponder()
                self.retrospectiveTitle.stringValue = webView.title!
            }
            else{
                loadAnimation.becomeFirstResponder()
                webView.isHidden = true
                loadAnimation.startAnimation(nil)
                loadAnimation.isHidden = false
            }
        }

        
    }
    
    func webView(_ webView: WKWebView, decidePolicyFor navigationAction: WKNavigationAction, decisionHandler: @escaping (WKNavigationActionPolicy) -> Void) {
        if navigationAction.navigationType == .linkActivated  {
            if let url = navigationAction.request.url,
                let host = url.host, !(host.hasPrefix("127.0.0.1") || host.hasPrefix("localhost")),
                NSWorkspace.shared.open(url) {
                print(url)
                decisionHandler(.cancel)
            } else {
                print("allowed")
                toggleButton.title = "Return to " + (isWeekly ? "Weekly" : "Daily") + " Retrospective"
                isTaskFocused = true
                decisionHandler(.allow)
            }
        } else {
            decisionHandler(.allow)
        }
    }

    
    
}
