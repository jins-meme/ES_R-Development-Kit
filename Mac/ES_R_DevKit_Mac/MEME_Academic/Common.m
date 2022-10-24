//
//  Common.m
//  MEME_Academic
//
//  Created by Celleus on 2022/09/05.
//  Copyright © 2022 jins-jp. All rights reserved.
//

#import "Common.h"

#include <ifaddrs.h>
#include <arpa/inet.h>

@implementation Common

+ (void)setUserDefaults:(id)value forKey:(NSString *)key {
    [self setUserDefaults:value forKey:key appGroups:nil];
}

+ (void)setUserDefaults:(id)value forKey:(NSString *)key appGroups:(NSString *)appGroups {
    NSUserDefaults *userDefaults;
    if (!appGroups) {
        userDefaults = [NSUserDefaults standardUserDefaults];
    }
    else {
        userDefaults = [[NSUserDefaults alloc] initWithSuiteName:appGroups];
    }
   
    NSData *data = [NSKeyedArchiver archivedDataWithRootObject:value];
    
    if(value != nil){
        //保存
        [userDefaults setObject:data forKey:key];
    }
    else{
        //削除
        [userDefaults removeObjectForKey:key];
    }
    
    [userDefaults synchronize];
}

+ (id)getUserDefaultsForKey:(NSString *)key {
    return [self getUserDefaultsForKey:key appGroups:nil];
}

+ (id)getUserDefaultsForKey:(NSString *)key appGroups:(NSString *)appGroups {
    NSUserDefaults *userDefaults;
    if (!appGroups) {
        userDefaults = [NSUserDefaults standardUserDefaults];
    }
    else {
        userDefaults = [[NSUserDefaults alloc] initWithSuiteName:appGroups];
    }
    
    NSData *data = [userDefaults objectForKey:key];
    
    if(nil != data){
        return [NSKeyedUnarchiver unarchiveObjectWithData:data];
    }
    return nil;
}

+ (NSString *)getIPAddress {

    NSMutableArray *array = [[NSMutableArray alloc] init];
    struct ifaddrs *interfaces = NULL;
    struct ifaddrs *temp_addr = NULL;
    int success = 0;
    // retrieve the current interfaces - returns 0 on success
    success = getifaddrs(&interfaces);
    if (success == 0) {
        // Loop through linked list of interfaces
        temp_addr = interfaces;
        while(temp_addr != NULL) {
            if(temp_addr->ifa_addr->sa_family == AF_INET) {
                // Check if interface is en0 which is the wifi connection on the iPhone
//                if([[NSString stringWithUTF8String:temp_addr->ifa_name] isEqualToString:@"en0"]) {
//                    // Get NSString from C String
//                    address = [NSString stringWithUTF8String:inet_ntoa(((struct sockaddr_in *)temp_addr->ifa_addr)->sin_addr)];
//                    NSLog(@"address:%@",address);
//                }
//                NSString *address192xxx = [NSString stringWithUTF8String:inet_ntoa(((struct sockaddr_in *)temp_addr->ifa_addr)->sin_addr)];
//                NSLog(@"address192xxx:%@",address192xxx);
//                if ([address192xxx hasPrefix:@"192"]) {
//                    address = address192xxx;
//                    NSLog(@"address:%@",address);
//                }
                
                NSString *address = [NSString stringWithUTF8String:inet_ntoa(((struct sockaddr_in *)temp_addr->ifa_addr)->sin_addr)];
                NSLog(@"address:%@",address);
                if (![address isEqualToString:@""] && ![address isEqualToString:@"127.0.0.1"]) {
                    [array addObject:address];
                }
            }
            temp_addr = temp_addr->ifa_next;
        }
    }
    // Free memory
    freeifaddrs(interfaces);
    if ([array count] > 0) {
        return [array firstObject];
    }
    else {
        return @"";
    }
}

@end
