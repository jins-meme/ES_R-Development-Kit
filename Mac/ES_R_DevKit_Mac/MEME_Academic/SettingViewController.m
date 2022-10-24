//
//  SettingViewController.m
//  MEME_Academic
//
//  Created by Celleus on 2022/09/02.
//  Copyright © 2022 jins-jp. All rights reserved.
//

#import "SettingViewController.h"
#import "Const.h"
#import "Common.h"
#import "UserSetting.h"

@interface SettingViewController ()

@property (weak) IBOutlet NSTextField *browseTextField;

@property (weak) IBOutlet NSTextFieldCell *xAxisTextField;
@property (weak) IBOutlet NSTextFieldCell *yAxisTextField;
@property (weak) IBOutlet NSTextFieldCell *zAxisTextField;

@property (weak) IBOutlet NSButton *showSaveFileDialogButton;
@property (weak) IBOutlet NSButton *extermalOutputSocketButton;

@property (weak) IBOutlet NSTextFieldCell *localProtTextField;
@property (weak) IBOutlet NSTextField *localAddressTextField;

@end

@implementation SettingViewController

- (void)viewDidLoad {
    [super viewDidLoad];
    // Do view setup here.
    
    [self setSettingParameter];
}

- (void)viewWillAppear {
    [super viewWillAppear];
    self.view.window.title = @"Setting";
}

- (void)setSettingParameter {
    NSLog(@"setSettingParameter");
    _browseTextField.stringValue = [UserSetting getSaveFilePath];
    _xAxisTextField.stringValue = [NSString stringWithFormat:@"%g",[UserSetting getXAxis]];
    _yAxisTextField.stringValue = [NSString stringWithFormat:@"%g",[UserSetting getYAxis]];
    _zAxisTextField.stringValue = [NSString stringWithFormat:@"%g",[UserSetting getZAxis]];
    _showSaveFileDialogButton.state = [UserSetting getShowSaveFileDialog];
    _extermalOutputSocketButton.state = [UserSetting getExtermalOutputSocket];
    _localProtTextField.stringValue = [UserSetting getLocalPort];
    _localAddressTextField.stringValue = [Common getIPAddress];
}

- (void)saveSettingParameter {
    NSLog(@"saveSettingParameter");
    [UserSetting setSaveFilePath:_browseTextField.stringValue];
    [UserSetting setXAxis:[_xAxisTextField.stringValue doubleValue]];
    [UserSetting setYAxis:[_yAxisTextField.stringValue doubleValue]];
    [UserSetting setZAxis:[_zAxisTextField.stringValue doubleValue]];
    [UserSetting setShowSaveFileDialog:_showSaveFileDialogButton.state];
    [UserSetting setExtermalOutputSocket:_extermalOutputSocketButton.state];
    [UserSetting setLocalPort:_localProtTextField.stringValue];
//    [UserSetting setLocalAddress:_localAddressTextField.stringValue];
}

// =============================================================================
#pragma mark - button_StartScan_Tapped
// =============================================================================
- (IBAction)button_Browse_Tapped:(id)sender {
    NSLog(@"button_Browse_Tapped");
    [self showOpenPanel];
}

- (void)showOpenPanel {
    NSOpenPanel *panel = [[NSOpenPanel alloc] init];
    panel.canChooseDirectories = true;
    panel.canChooseFiles = false;
    [panel beginSheetModalForWindow:self.view.window
                  completionHandler:^(NSModalResponse result) {
        
        switch (result) {
            case NSFileHandlingPanelOKButton:
                _browseTextField.stringValue = [panel URLs][0];
            default:
                break;
        }
        
    }];
}

// =============================================================================
#pragma mark - button_OpenFolder_Tapped
// =============================================================================
- (IBAction)button_OpenFolder_Tapped:(id)sender {
    NSLog(@"button_OpenFolder_Tapped");
    [self showOpenFolder];
}

- (void)showOpenFolder {
    NSString *urlString = _browseTextField.stringValue;
    NSURL *url = [NSURL URLWithString:urlString];
    if ([[NSWorkspace sharedWorkspace] openURL:url]) {
        NSLog(@"ファイルが開ける");
    }
    else {
        NSLog(@"ファイルが開けない");
    }
}

// =============================================================================
#pragma mark - button_Apply_Tapped
// =============================================================================
- (IBAction)button_Apply_Tapped:(id)sender {
    NSLog(@"button_Apply_Tapped");
    [self saveSettingParameter];
    if (self.delegate) {
        [self.delegate didApply:self];
    }
    [self.view.window close];
}

// =============================================================================
#pragma mark - button_Cancel_Tapped
// =============================================================================
- (IBAction)button_Cancel_Tapped:(id)sender {
    NSLog(@"button_Cancel_Tapped");
    [self.view.window close];
}

@end
