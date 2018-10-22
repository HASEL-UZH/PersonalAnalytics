//
//  ASHDatePicker.m
//  ASHDatePicker
//
//  Created by Adam Hartford on 10/3/12.
//  Copyright (c) 2012 Adam Hartford. All rights reserved.
//

#import "ASHDatePicker.h"

@implementation ASHDatePicker

@synthesize popover = _popover;
@synthesize delegate = _delegate;
@synthesize preferredPopoverEdge = _preferredPopoverEdge;

- (void)popoverDateAction
{
    self.dateValue = controller.datePicker.dateValue;
    // Update bound value...
    NSDictionary *bindingInfo = [self infoForBinding:NSValueBinding];
    [[bindingInfo valueForKey:NSObservedObjectKey] setValue:self.dateValue
                                                 forKeyPath:[bindingInfo valueForKey:NSObservedKeyPathKey]];
}

-(void)dateAction
{
    controller.datePicker.dateValue = self.dateValue;
}

- (void)awakeFromNib
{
    controller = [[ASHDatePickerController alloc] init];
    self.action = @selector(dateAction);
    controller.datePicker.action = @selector(popoverDateAction);
    [controller.datePicker bind:NSValueBinding toObject:self withKeyPath:@"dateValue" options:nil];
    
    _popover = [[NSPopover alloc] init];
    _popover.contentViewController = controller;
    _popover.behavior = NSPopoverBehaviorSemitransient;
    
    _preferredPopoverEdge = NSMinXEdge;
}

- (id)initWithFrame:(NSRect)frame
{
    self = [super initWithFrame:frame];
    if (self) {
        // Initialization code here.   
    }
    
    return self;
}

- (void)mouseDown:(NSEvent *)theEvent
{
    [self becomeFirstResponder];
    [super mouseDown:theEvent];
}

- (BOOL)becomeFirstResponder
{
    showingPopover = YES;
    controller.datePicker.dateValue = self.dateValue;
    
    if (![_delegate respondsToSelector:@selector(datePickerShouldShowPopover:)]
        || [_delegate datePickerShouldShowPopover:self]) {
        
        [_popover showRelativeToRect:self.bounds ofView:self preferredEdge:_preferredPopoverEdge];
    }
    
    showingPopover = NO;
    return [super becomeFirstResponder];
}

- (BOOL)resignFirstResponder
{
    if (showingPopover) return NO;
    [_popover close];
    return [super resignFirstResponder];
}

@end

@implementation ASHDatePickerController

@synthesize datePicker = _datePicker;

- (id)init
{
    self = [super init];
    if (self) {
        NSRect viewFrame = NSMakeRect(0.0f, 0.0f, 180.0f, 180.0f);
        NSView *popoverView = [[NSView alloc] initWithFrame:viewFrame];
        
        NSRect pickerFrame = NSMakeRect(22.0f, 17.0f, 139.0f, 148.0f);
        _datePicker = [[NSDatePicker alloc] initWithFrame:pickerFrame];
        _datePicker.datePickerStyle = NSClockAndCalendarDatePickerStyle;
        _datePicker.drawsBackground = NO;
        [_datePicker.cell setBezeled:NO];
        [popoverView addSubview:_datePicker];
        self.view = popoverView;
    }
    
    return self;
}

@end
