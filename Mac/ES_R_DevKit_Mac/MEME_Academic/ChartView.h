//
//  ChartView.h
//  MEME_Academic
//
//  Created by Celleus on 2022/09/13.
//  Copyright © 2022 jins-jp. All rights reserved.
//

#import <Cocoa/Cocoa.h>

NS_ASSUME_NONNULL_BEGIN

@interface ChartView : NSView

@property (nonatomic,strong) NSMutableArray *datas;

@property (nonatomic) float xMaxValue;
@property (nonatomic) float xMinValue;
@property (nonatomic) float yMaxValue;
@property (nonatomic) float yMinValue;

@property (nonatomic) float xInitialPosition;
@property (nonatomic) int xLongScale;
@property (nonatomic) int xShortScale;

@property (nonatomic) NSMutableArray *xTextFields;
@property (nonatomic) int xTextFieldValue;

@property (nonatomic) NSMutableArray *yTextFields;
@property (nonatomic) int yTextFieldValue;

- (void)setChartData:(NSMutableArray *)datas lineColor:(NSColor *)color;

@end

NS_ASSUME_NONNULL_END
