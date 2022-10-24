//
//  Common.h
//  MEME_Academic
//
//  Created by Celleus on 2022/09/05.
//  Copyright © 2022 jins-jp. All rights reserved.
//

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@interface Common : NSObject

+ (void)setUserDefaults:(id)value forKey:(NSString *)key;
+ (void)setUserDefaults:(id)value forKey:(NSString *)key appGroups:(NSString *)appGroups;
+ (id)getUserDefaultsForKey:(NSString *)key;
+ (id)getUserDefaultsForKey:(NSString *)key appGroups:(NSString *)appGroups;
+ (NSString *)getIPAddress;

@end

NS_ASSUME_NONNULL_END
