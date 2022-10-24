//
//  UserSetting.h
//  MEME_Academic
//
//  Created by Celleus on 2022/09/14.
//  Copyright © 2022 jins-jp. All rights reserved.
//

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@interface UserSetting : NSObject

//+ (void)clear;

+ (void)fristSetting;
+ (void)defaultSetting;

+ (void)setSaveFilePath:(id)value;
+ (NSString *)getSaveFilePath;
+ (void)setXAxis:(double)value;
+ (double)getXAxis;
+ (void)setYAxis:(double)value;
+ (double)getYAxis;
+ (void)setZAxis:(double)value;
+ (double)getZAxis;
+ (void)setShowSaveFileDialog:(BOOL)value;
+ (BOOL)getShowSaveFileDialog;
+ (void)setExtermalOutputSocket:(BOOL)value;
+ (BOOL)getExtermalOutputSocket;
+ (void)setLocalPort:(id)value;
+ (NSString *)getLocalPort;
//+ (void)setLocalAddress:(id)value;
//+ (NSString *)getLocalAddress;

@end

NS_ASSUME_NONNULL_END
