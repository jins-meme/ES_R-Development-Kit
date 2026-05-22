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
    // ボタンのスタイルを設定（ライト／ダークモード対応）
    [self styleButtonsForAppearance];
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
            case NSModalResponseOK:
                _browseTextField.stringValue = panel.URL.path;
                break;
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
    NSURL *url = [NSURL fileURLWithPath:urlString];
    if ([[NSWorkspace sharedWorkspace] openURL:url]) {
        NSLog(@"ファイルが開ける");
    }
    else {
        NSLog(@"ファイルが開けない");
    }
}

// =============================================================================
#pragma mark - button_Extemal_Output_Socket_Tapped
// =============================================================================
- (IBAction)button_Extemal_Output_Socket_Tapped:(id)sender {
    NSLog(@"button_Extemal_Output_Socket_Tapped");
}

// =============================================================================
#pragma mark - textField_Local_Port_Tapped
// =============================================================================
- (IBAction)textField_Local_Port_Tapped:(id)sender {
    NSLog(@"textField_Local_Port_Tapped");
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

- (void)styleButtonsForAppearance {
    NSString *appearanceName = [self.view.effectiveAppearance bestMatchFromAppearancesWithNames:@[NSAppearanceNameAqua, NSAppearanceNameDarkAqua]];
    BOOL isDark = [appearanceName isEqualToString:NSAppearanceNameDarkAqua];
    CGFloat bgWhite = isDark ? 0.60 : 0.85;
    NSColor *titleColor = isDark ? [NSColor labelColor] : [NSColor colorWithWhite:0.15 alpha:1.0];
    // Browse/Open/Apply/Cancel should keep the same (dark) title color even in dark mode
    NSColor *constantButtonTitleColor = [NSColor colorWithWhite:0.15 alpha:1.0];
    [self applyStyleToButtonsInView:self.view bgWhite:bgWhite titleColor:titleColor];
}

- (void)applyStyleToButtonsInView:(NSView *)parent bgWhite:(CGFloat)bg titleColor:(NSColor *)titleColor {
    for (NSView *sub in parent.subviews) {
        if ([sub isKindOfClass:[NSButton class]]) {
            NSButton *button = (NSButton *)sub;
            // 除外: Show save file dialog と External output socket のボタンは変更しない
            if (button == _showSaveFileDialogButton || button == _extermalOutputSocketButton) {
                // skip styling for these specific controls
            } else {
                button.wantsLayer = YES;
                button.layer.cornerRadius = 14.0;
                button.layer.masksToBounds = YES;
                button.layer.backgroundColor = [NSColor colorWithWhite:bg alpha:1.0].CGColor;
                // 枠無しにする
                button.bordered = NO;
                if ([button respondsToSelector:@selector(cell)]) {
                    id cell = button.cell;
                    if ([cell respondsToSelector:@selector(setBordered:)]) {
                        [cell setBordered:NO];
                    }
                }
                // Decide title color: keep constant for Browse/Open/Apply/Cancel, otherwise use appearance-based color
                NSColor *useTitleColor = titleColor;
                if (button == _button_Browse || button == _button_OpenFolder || button == _button_Apply || button == _button_Cancel) {
                    useTitleColor = [NSColor colorWithWhite:0.15 alpha:1.0];
                }

                NSMutableAttributedString *attr = [[NSMutableAttributedString alloc] initWithAttributedString:button.attributedTitle];
                NSRange range = NSMakeRange(0, attr.length);
                [attr addAttribute:NSForegroundColorAttributeName value:useTitleColor range:range];
                [button setAttributedTitle:attr];
            }
        }
        [self applyStyleToButtonsInView:sub bgWhite:bg titleColor:titleColor];
    }
}

@end
