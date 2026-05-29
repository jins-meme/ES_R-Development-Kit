//
//  DecEnc.h
//  Copyright © 2016年 jins.com. All rights reserved.
//

#import <Foundation/Foundation.h>

@interface DecEnc : NSObject

+ (void)Encode:(uint8_t *)buf;
+ (void)Decode:(uint8_t *)buf;

@end
