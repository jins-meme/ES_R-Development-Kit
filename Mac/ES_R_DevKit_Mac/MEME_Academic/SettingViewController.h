//
//  SettingViewController.h
//  MEME_Academic
//
//  Created by Celleus on 2022/09/02.
//  Copyright © 2022 jins-jp. All rights reserved.
//

#import <Cocoa/Cocoa.h>

NS_ASSUME_NONNULL_BEGIN

@class SettingViewController;

@protocol SettingViewControllerDelegate<NSObject>

- (void)didApply:(SettingViewController *)settingViewController;

@end

@interface SettingViewController : NSViewController

@property (nonatomic, weak, nullable) id<SettingViewControllerDelegate> delegate;
@property (weak) IBOutlet NSButton *button_Browse;
@property (weak) IBOutlet NSButton *button_OpenFolder;
@property (weak) IBOutlet NSButton *button_Apply;
@property (weak) IBOutlet NSButton *button_Cancel;

@end

NS_ASSUME_NONNULL_END
