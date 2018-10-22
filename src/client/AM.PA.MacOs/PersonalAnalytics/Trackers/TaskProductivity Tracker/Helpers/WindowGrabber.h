//
//  WindowGrabber.h
//  PersonalAnalytics
//
//  Created by Jonathan Stiansen on 2015-11-10.
//
@import Cocoa;

@interface WindowGrabber : NSWindowController
{
}
-(BOOL) singleWindowShot:(NSString*)applicationName filePathFromHome:(NSString*)file;
@end

