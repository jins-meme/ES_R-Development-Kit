//
//  UserSetting.m
//  MEME_Academic
//
//  Created by Celleus on 2022/09/14.
//  Copyright © 2022 jins-jp. All rights reserved.
//

#import "UserSetting.h"
#import "Const.h"

@implementation UserSetting

//+ (void)clear {
//    NSUserDefaults *userDefaults = [NSUserDefaults standardUserDefaults];
//    [userDefaults setInteger:0 forKey:kConst_Chart1_Type];
//    [userDefaults setInteger:0 forKey:kConst_Chart2_Type];
//    [userDefaults setInteger:0 forKey:kConst_Chart3_Type];
//    [userDefaults setDouble:0.0 forKey:kConst_X_Axis];
//    [userDefaults setDouble:0.0 forKey:kConst_Y_Axis];
//    [userDefaults setDouble:0.0 forKey:kConst_Z_Axis];
//    [userDefaults setBool:false forKey:kConst_ShowSaveFileDialog];
//    [userDefaults setBool:false forKey:kConst_ExtermalOutputSocket];
//    [userDefaults setObject:nil forKey:kConst_LocalPort];
//    [userDefaults setObject:nil forKey:kConst_LocalAddress];
//}

// =============================================================================
#pragma mark - fristSetting
// =============================================================================

+ (void)fristSetting {
    if ([[NSUserDefaults standardUserDefaults] objectForKey:kConst_LocalPort] == nil) {
        NSLog(@"初期設定開始");
        [self defaultSetting];
    }
    else {
        NSLog(@"初期設定済み");
    }
}

// =============================================================================
#pragma mark - defaultSetting
// =============================================================================

+ (void)defaultSetting {
    [self createDefaultSaveDirectory];
    NSUserDefaults *userDefaults = [NSUserDefaults standardUserDefaults];
//    [userDefaults setInteger:0 forKey:kConst_Chart1_Type];
//    [userDefaults setInteger:0 forKey:kConst_Chart2_Type];
//    [userDefaults setInteger:0 forKey:kConst_Chart3_Type];
    [userDefaults setDouble:0.0 forKey:kConst_X_Axis];
    [userDefaults setDouble:0.0 forKey:kConst_Y_Axis];
    [userDefaults setDouble:0.0 forKey:kConst_Z_Axis];
    [userDefaults setBool:false forKey:kConst_ShowSaveFileDialog];
    [userDefaults setBool:false forKey:kConst_ExtermalOutputSocket];
    [userDefaults setObject:@"88" forKey:kConst_LocalPort];
//    [userDefaults setObject:nil forKey:kConst_LocalAddress];
}

// =============================================================================
#pragma mark - createDefaultSaveDirectory
// =============================================================================
+ (void)createDefaultSaveDirectory {
    NSArray *paths = NSSearchPathForDirectoriesInDomains(NSDocumentDirectory, NSUserDomainMask, YES);
    NSString *documentsDirectory = [paths objectAtIndex:0];
    NSString *defaultDirectory =  [documentsDirectory stringByAppendingPathComponent:@"/JINS/MEME_Academic"];
    NSLog(@"defaultDirectory:%@",defaultDirectory);
    if (![[NSFileManager defaultManager] isExecutableFileAtPath:defaultDirectory]) {
        NSLog(@"ディレクトリがないので作成");
        if ([[NSFileManager defaultManager] createDirectoryAtPath:defaultDirectory withIntermediateDirectories:YES attributes:nil error:nil]) {
            NSLog(@"ディレクトリ作成 成功");
            NSUserDefaults *userDefaults = [NSUserDefaults standardUserDefaults];
            [userDefaults setObject:[NSString stringWithFormat:@"file://%@",defaultDirectory] forKey:kConst_SaveFilePath];
        }
        else {
            NSLog(@"ディレクトリ作成 失敗");
            NSUserDefaults *userDefaults = [NSUserDefaults standardUserDefaults];
            [userDefaults setObject:@"" forKey:kConst_SaveFilePath];
        }
    }
    else {
        NSLog(@"既にディレクトリがある");
        NSUserDefaults *userDefaults = [NSUserDefaults standardUserDefaults];
        [userDefaults setObject:[NSString stringWithFormat:@"file://%@",defaultDirectory] forKey:kConst_SaveFilePath];
    }
}

// =============================================================================
#pragma mark - Home
// =============================================================================

//+ (void)setChart1Type:(int)value {
//    [[NSUserDefaults standardUserDefaults] setInteger:value forKey:kConst_Chart1_Type];
//}
//+ (long)getChart1Type {
//    return [[NSUserDefaults standardUserDefaults] integerForKey:kConst_Chart1_Type];
//}
//
//+ (void)setChart2Type:(int)value {
//    [[NSUserDefaults standardUserDefaults] setInteger:value forKey:kConst_Chart2_Type];
//}
//+ (long)getChart2Type {
//    return [[NSUserDefaults standardUserDefaults] integerForKey:kConst_Chart2_Type];
//}
//
//+ (void)setChart3Type:(int)value {
//    [[NSUserDefaults standardUserDefaults] setInteger:value forKey:kConst_Chart3_Type];
//}
//+ (long)getChart3Type {
//    return [[NSUserDefaults standardUserDefaults] integerForKey:kConst_Chart3_Type];
//}

// =============================================================================
#pragma mark - Setting
// =============================================================================

+ (void)setSaveFilePath:(id)value {
    [[NSUserDefaults standardUserDefaults] setObject:value forKey:kConst_SaveFilePath];
}
+ (NSString *)getSaveFilePath {
    return [[NSUserDefaults standardUserDefaults] objectForKey:kConst_SaveFilePath];
}

+ (void)setXAxis:(double)value {
    [[NSUserDefaults standardUserDefaults] setDouble:value forKey:kConst_X_Axis];
}
+ (double)getXAxis {
    return [[NSUserDefaults standardUserDefaults] doubleForKey:kConst_X_Axis];
}

+ (void)setYAxis:(double)value {
    [[NSUserDefaults standardUserDefaults] setDouble:value forKey:kConst_Y_Axis];
}
+ (double)getYAxis {
    return [[NSUserDefaults standardUserDefaults] doubleForKey:kConst_Y_Axis];
}

+ (void)setZAxis:(double)value {
    [[NSUserDefaults standardUserDefaults] setDouble:value forKey:kConst_Z_Axis];
}
+ (double)getZAxis {
    return [[NSUserDefaults standardUserDefaults] doubleForKey:kConst_Z_Axis];
}

+ (void)setShowSaveFileDialog:(BOOL)value {
    [[NSUserDefaults standardUserDefaults] setBool:value forKey:kConst_ShowSaveFileDialog];
}
+ (BOOL)getShowSaveFileDialog {
    return [[NSUserDefaults standardUserDefaults] boolForKey:kConst_ShowSaveFileDialog];
}

+ (void)setExtermalOutputSocket:(BOOL)value {
    [[NSUserDefaults standardUserDefaults] setBool:value forKey:kConst_ExtermalOutputSocket];
}
+ (BOOL)getExtermalOutputSocket {
    return [[NSUserDefaults standardUserDefaults] boolForKey:kConst_ExtermalOutputSocket];
}

+ (void)setLocalPort:(id)value {
    [[NSUserDefaults standardUserDefaults] setObject:value forKey:kConst_LocalPort];
}
+ (NSString *)getLocalPort {
    return [[NSUserDefaults standardUserDefaults] objectForKey:kConst_LocalPort];
}

//+ (void)setLocalAddress:(id)value {
//    [[NSUserDefaults standardUserDefaults] setObject:value forKey:kConst_LocalAddress];
//}
//+ (NSString *)getLocalAddress {
//    return [[NSUserDefaults standardUserDefaults] objectForKey:kConst_LocalAddress];
//}

@end
