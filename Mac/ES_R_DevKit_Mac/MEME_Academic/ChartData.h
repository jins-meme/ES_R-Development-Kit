//
//  ChartData.h
//  MEME_Academic
//
//  Created by Celleus on 2022/09/14.
//  Copyright © 2022 jins-jp. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <Cocoa/Cocoa.h>

NS_ASSUME_NONNULL_BEGIN

@interface ChartData : NSObject

@property (nonatomic,strong) NSColor *lineColor;
@property (nonatomic,strong) NSMutableArray *datas;

@end

NS_ASSUME_NONNULL_END
