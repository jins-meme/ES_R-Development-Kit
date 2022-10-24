//
//  SettingViewController.h
//  MEME_Academic
//
//  Created by Celleus on 2022/09/02.
//  Copyright © 2022 jins-jp. All rights reserved.
//

#import <Cocoa/Cocoa.h>
@class SettingViewController;

@protocol SettingViewControllerDelegate<NSObject>

- (void)didApply:(SettingViewController *)settingViewController;

@end

NS_ASSUME_NONNULL_BEGIN

@interface SettingViewController : NSViewController

@property (nonatomic, assign) id<SettingViewControllerDelegate> delegate;

@end

NS_ASSUME_NONNULL_END
