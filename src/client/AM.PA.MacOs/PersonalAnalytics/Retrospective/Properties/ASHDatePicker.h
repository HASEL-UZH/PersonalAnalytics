//
//  ASHDatePicker.h
//  ASHDatePicker
//
//  Created by Adam Hartford on 10/3/12.
//  Copyright (c) 2012 Adam Hartford. All rights reserved.
//

#import <Cocoa/Cocoa.h>

@protocol ASHDatePickerDelegate;

@class ASHDatePickerController;

@interface ASHDatePicker : NSDatePicker
{
    ASHDatePickerController *controller;
    BOOL showingPopover;
}

@property (strong, nonatomic) NSPopover *popover;
@property (strong, nonatomic) id <ASHDatePickerDelegate> delegate;

/**
 * The preferred edge at which to display the popover.
 * Default is NSMaxXEdge.
 */
@property (assign) NSRectEdge preferredPopoverEdge;

@end

@protocol ASHDatePickerDelegate <NSObject>

/**
 * Called each time a date picker becomes the first responder.
 * Return NO if the popover should not be displayed.
 * If not implemented, default is YES.
 */
- (BOOL)datePickerShouldShowPopover:(ASHDatePicker *)datepicker;

@end

@interface ASHDatePickerController : NSViewController

@property (strong, nonatomic) NSDatePicker *datePicker;

@end
