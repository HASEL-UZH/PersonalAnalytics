//
//  WindowGrabber.m
//  PersonalAnalytics
//
//  Created by Jonathan Stiansen on 2015-11-10.
//

/*
 Copyright (C) 2015 Apple Inc. All Rights Reserved.
 See LICENSE.txt for this sample’s licensing information
 
 Abstract:
 Handles UI interaction and retrieves window images.
 */

#import "WindowGrabber.h"
@import CoreServices;

@interface WindowListApplierData : NSObject
{
}

@property (strong, nonatomic) NSMutableArray * outputArray;
@property int order;

@end

@implementation WindowListApplierData

-(instancetype)initWindowListData:(NSMutableArray *)array
{
    self = [super init];
    
    self.outputArray = array;
    self.order = 0;
    
    return self;
}

@end


@interface WindowGrabber ()
{
    IBOutlet NSImageView *outputView;
    IBOutlet NSArrayController *arrayController;
    
    CGWindowListOption listOptions;
    CGWindowListOption singleWindowListOptions;
    CGWindowImageOption imageOptions;
    CGRect imageBounds;
}

@property (strong) WindowListApplierData *windowListData;
@property (weak) IBOutlet NSButton * listOffscreenWindows;
@property (weak) IBOutlet NSButton * listDesktopWindows;
@property (weak) IBOutlet NSButton * imageFramingEffects;
@property (weak) IBOutlet NSButton * imageOpaqueImage;
@property (weak) IBOutlet NSButton * imageShadowsOnly;
@property (weak) IBOutlet NSButton * imageTightFit;
@property (weak) IBOutlet NSMatrix * singleWindow;

@end


@implementation WindowGrabber

#pragma mark Basic Profiling Tools
// Set to 1 to enable basic profiling. Profiling information is logged to console.
#ifndef PROFILE_WINDOW_GRAB
#define PROFILE_WINDOW_GRAB 0
#endif

#if PROFILE_WINDOW_GRAB
#define StopwatchStart() AbsoluteTime start = UpTime()
#define Profile(img) CFRelease(CGDataProviderCopyData(CGImageGetDataProvider(img)))
#define StopwatchEnd(caption) do { Duration time = AbsoluteDeltaToDuration(UpTime(), start); double timef = time < 0 ? time / -1000000.0 : time / 1000.0; NSLog(@"%s Time Taken: %f seconds", caption, timef); } while(0)
#else
#define StopwatchStart()
#define Profile(img)
#define StopwatchEnd(caption)
#endif

#pragma mark Utilities

// Simple helper to twiddle bits in a uint32_t.
uint32_t ChangeBits(uint32_t currentBits, uint32_t flagsToChange, BOOL setFlags);
inline uint32_t ChangeBits(uint32_t currentBits, uint32_t flagsToChange, BOOL setFlags)
{
    if(setFlags)
    {	// Set Bits
        return currentBits | flagsToChange;
    }
    else
    {	// Clear Bits
        return currentBits & ~flagsToChange;
    }
}

-(void)setOutputImage:(CGImageRef)cgImage
{
    if(cgImage != NULL)
    {
        // Create a bitmap rep from the image...
        NSBitmapImageRep *bitmapRep = [[NSBitmapImageRep alloc] initWithCGImage:cgImage];
        // Create an NSImage and add the bitmap rep to it...
        NSImage *image = [[NSImage alloc] init];
        [image addRepresentation:bitmapRep];
        // Set the output view to the new NSImage.
        [outputView setImage:image];
    }
    else
    {
        [outputView setImage:nil];
    }
}

#pragma mark Window List & Window Image Methods

NSString *kAppNameKey = @"applicationName";	// Application Name & PID
NSString *kWindowOriginKey = @"windowOrigin";	// Window Origin as a string
NSString *kWindowSizeKey = @"windowSize";		// Window Size as a string
NSString *kWindowIDKey = @"windowID";			// Window ID
NSString *kWindowLevelKey = @"windowLevel";	// Window Level
NSString *kWindowOrderKey = @"windowOrder";	// The overall front-to-back ordering of the windows as returned by the window server

void WindowListApplierFunction(const void *inputDictionary, void *context);
void WindowListApplierFunction(const void *inputDictionary, void *context)
{
    NSDictionary *entry = (__bridge NSDictionary*)inputDictionary;
    WindowListApplierData *data = (__bridge WindowListApplierData*)context;
    
    // The flags that we pass to CGWindowListCopyWindowInfo will automatically filter out most undesirable windows.
    // However, it is possible that we will get back a window that we cannot read from, so we'll filter those out manually.
    int sharingState = [entry[(id)kCGWindowSharingState] intValue];
    if(sharingState != kCGWindowSharingNone)
    {
        NSMutableDictionary *outputEntry = [NSMutableDictionary dictionary];
        
        // Grab the application name, but since it's optional we need to check before we can use it.
        NSString *applicationName = entry[(id)kCGWindowOwnerName];
        if(applicationName != NULL)
        {
            // PID is required so we assume it's present.
            NSString *nameAndPID = [NSString stringWithFormat:@"%@ (%@)", applicationName, entry[(id)kCGWindowOwnerPID]];
            outputEntry[kAppNameKey] = nameAndPID;
        }
        else
        {
            // The application name was not provided, so we use a fake application name to designate this.
            // PID is required so we assume it's present.
            NSString *nameAndPID = [NSString stringWithFormat:@"((unknown)) (%@)", entry[(id)kCGWindowOwnerPID]];
            outputEntry[kAppNameKey] = nameAndPID;
        }
        
        // Grab the Window Bounds, it's a dictionary in the array, but we want to display it as a string
        CGRect bounds;
        CGRectMakeWithDictionaryRepresentation((CFDictionaryRef)entry[(id)kCGWindowBounds], &bounds);
        NSString *originString = [NSString stringWithFormat:@"%.0f/%.0f", bounds.origin.x, bounds.origin.y];
        outputEntry[kWindowOriginKey] = originString;
        NSString *sizeString = [NSString stringWithFormat:@"%.0f*%.0f", bounds.size.width, bounds.size.height];
        outputEntry[kWindowSizeKey] = sizeString;
        
        // Grab the Window ID & Window Level. Both are required, so just copy from one to the other
        outputEntry[kWindowIDKey] = entry[(id)kCGWindowNumber];
        outputEntry[kWindowLevelKey] = entry[(id)kCGWindowLayer];
        
        // Finally, we are passed the windows in order from front to back by the window server
        // Should the user sort the window list we want to retain that order so that screen shots
        // look correct no matter what selection they make, or what order the items are in. We do this
        // by maintaining a window order key that we'll apply later.
        outputEntry[kWindowOrderKey] = @(data.order);
        data.order++;
        
        [data.outputArray addObject:outputEntry];
    }
}


-(BOOL)singleWindowShot:(NSString*)applicationName  filePathFromHome:(NSString*)file
{
    listOptions = kSingleWindowAboveIncluded;
    CFArrayRef windowList = CGWindowListCopyWindowInfo(listOptions, kCGNullWindowID);
    // Copy the returned list, further pruned, to another list. This also adds some bookkeeping
    // information to the list as well as
    NSMutableArray * prunedWindowList = [NSMutableArray array];
    self.windowListData = [[WindowListApplierData alloc] initWindowListData:prunedWindowList];
    //void CFArrayApplyFunction ( CFArrayRef theArray, CFRange range, CFArrayApplierFunction applier,void *context );
    CFArrayApplyFunction(windowList, CFRangeMake(0, CFArrayGetCount(windowList)), &WindowListApplierFunction, (__bridge void *)(self.windowListData));
    CFRelease(windowList);
    
    uint32_t windowID = 0;
    NSString *windowName = @"";
    NSInteger windowHeight = 0;
    NSInteger windowWidth = 0;
    for(NSDictionary *windowAttributes in prunedWindowList){
        // Displays in format "Appname (WindowID), remove last (WindowID)"
        windowName = [[windowAttributes[@"applicationName"] componentsSeparatedByString:@"("] objectAtIndex:0];
        windowName = [windowName substringToIndex:windowName.length -1]; // Remove space at end
        if ([applicationName isEqualToString:windowName]) {
            windowHeight = [[windowAttributes[@"windowSize"] componentsSeparatedByString:@"*"] objectAtIndex:0].integerValue;
            
            windowWidth = [[windowAttributes[@"windowSize"] componentsSeparatedByString:@"*"] objectAtIndex:1].integerValue;
            // Ensure non-windows, or uselessly small windows don't make it through
            if (windowHeight > 400 && windowWidth > 300){
                windowID = [windowAttributes[@"windowID"] unsignedIntValue];
                break;
            }
        }
    }
    

    
    //CGWindowID windowID = [selection[0][kWindowIDKey] unsignedIntValue];
    BOOL succeeded = false;
    if (windowID != 0){
        NSBitmapImageRep *bitmapRep = [self createSingleWindowShot:windowID];
        NSData *data = [bitmapRep representationUsingType:NSPNGFileType properties:nil];
        NSString *filePath = [NSString stringWithFormat:@"%@/%@",
                              [[[NSProcessInfo processInfo] environment] objectForKey:@"HOME"],
                              file];
        printf("%s\n", [filePath cStringUsingEncoding:NSUTF8StringEncoding]);
        succeeded = [data writeToFile:filePath atomically:YES];
    }
    return succeeded;
}


-(NSBitmapImageRep*)createSingleWindowShot:(CGWindowID)windowID
{
    // Create an image from the passed in windowID with the single window option selected by the user.
    CGImageRef windowImage = CGWindowListCreateImage(CGRectNull, 8, windowID, listOptions);
    
    
    NSBitmapImageRep *bitmapRep = [[NSBitmapImageRep alloc] initWithCGImage:windowImage];
    CGImageRelease(windowImage);
    return bitmapRep;
}

enum
{
    // Constants that correspond to the rows in the
    // Single Window Option matrix.
    kSingleWindowAboveOnly = 0,
    kSingleWindowAboveIncluded = 1,
    kSingleWindowOnly = 2,
    kSingleWindowBelowIncluded = 3,
    kSingleWindowBelowOnly = 4,
};

NSString *kvoContext = @"SonOfGrabContext";
-(void)dealloc
{
    // Remove our KVO notification
    [arrayController removeObserver:self forKeyPath:@"selectionIndexes"];
}
@end
