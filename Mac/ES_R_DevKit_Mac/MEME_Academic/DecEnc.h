//
//  DecEnc.h
//  DecEnc
//
//  Created by D-CLUE on 2016/06/27.
//  Copyright © 2016年 jins-jp.com. All rights reserved.
//

#import <Foundation/Foundation.h>

@interface DecEnc : NSObject

+ (void)Encode:(uint8_t *)buf;
+ (void)Decode:(uint8_t *)buf;

@end
