//
//  ChartView.m
//  MEME_Academic
//
//  Created by Celleus on 2022/09/13.
//  Copyright © 2022 jins-jp. All rights reserved.
//

#import "ChartView.h"
#import "ChartData.h"

@implementation ChartView

- (id)initWithFrame:(CGRect)frame {
   self = [super initWithFrame:frame];
    
   if (self) {

       self.datas = [[NSMutableArray alloc] init];
       
       self.xMaxValue = 10;
       self.xMinValue = 0;
       self.yMaxValue = 10;
       self.yMinValue = 0;
       
       self.xInitialPosition = 0;
       self.xLongScale = 100;
       self.xShortScale = 20;
       
       self.xTextFields = [[NSMutableArray alloc] init];
       self.xTextFieldValue = 0;
       
//       self.yTextFields = [[NSMutableArray alloc] init];
//       self.yTextFieldValue = 7;
   }
   
   return self;
}

- (void)drawRect:(NSRect)dirtyRect {
    [super drawRect:dirtyRect];
   
   CGFloat marginTop = 20;
   CGFloat marginBottom = 0;
   CGFloat marginLeft = 0;
   CGFloat marginRight = 0;
   
   CGFloat chanvasH = self.frame.size.height - marginTop - marginBottom;
   CGFloat chanvasW = self.frame.size.width - marginLeft - marginRight;
    
//    NSBezierPath *border1 = [NSBezierPath bezierPath];
//    [[NSColor blueColor] set];
//    [border1 setLineWidth:1.0f];
//    [border1 moveToPoint:NSMakePoint(0,0)];
//    [border1 lineToPoint:NSMakePoint(self.frame.size.width,0)];
//    [border1 lineToPoint:NSMakePoint(self.frame.size.width,self.frame.size.height)];
//    [border1 lineToPoint:NSMakePoint(0,self.frame.size.height)];
//    [border1 lineToPoint:NSMakePoint(0,0)];
//    [border1 stroke];
    
    
    NSBezierPath *border = [NSBezierPath bezierPath];
    [[NSColor blackColor] set];
    [border setLineWidth:1.0f];
    [border moveToPoint:NSMakePoint(marginLeft,marginTop)];
    [border lineToPoint:NSMakePoint(marginLeft+chanvasW,marginTop)];
    [border lineToPoint:NSMakePoint(marginLeft+chanvasW,marginTop+chanvasH)];
    [border lineToPoint:NSMakePoint(marginLeft,marginTop+chanvasH)];
    [border lineToPoint:NSMakePoint(marginLeft,marginTop)];
    [border stroke];
    
//    for (NSTextField *yTextField in self.yTextFields) {
//        [yTextField removeFromSuperview];
//    }
//    [self.yTextFields removeAllObjects];
//
//    if (self.yTextFieldValue >= 2) {
//        for (int i = 0; i < self.yTextFieldValue; i++) {
//            int height = 20;
//            NSTextField *textField = [[NSTextField alloc] init];
//            textField.frame = CGRectMake(3,
//                                     marginTop + chanvasH - ((chanvasH / (self.yTextFieldValue-1) * i) + height/2),
//                                     marginLeft - 3*2,
//                                         height);
//            [textField setBordered:NO];
//            textField.backgroundColor = [NSColor clearColor];
//            textField.textColor = [NSColor colorNamed:@"ChartText"];
//            textField.stringValue = [NSString stringWithFormat:@"%d",( (self.yMaxValue - self.yMinValue)/(self.yTextFieldValue-1)*i ) + self.yMinValue];
//            textField.alignment = NSTextAlignmentRight;
//            [self addSubview:textField];
//            [self.yTextFields addObject:textField];
//        }
//    }
    
    for (NSTextField *textField in self.xTextFields) {
        [textField removeFromSuperview];
    }
    [self.xTextFields removeAllObjects];
    
    // 上下の小さいメモリ
    for (int i = 0; i < (self.xMaxValue + self.xLongScale)/self.xShortScale; i++) {
        float wScal = chanvasW / (self.xMaxValue - self.xMinValue);
        float x  =  marginLeft + wScal * (self.xShortScale*i + self.xInitialPosition);
        if (marginLeft < x && x < marginLeft + chanvasW) {
            
            int length = 3;
            if (self.xShortScale*i % self.xLongScale == 0) {
                length = 6;
                
                NSTextField *textField = [[NSTextField alloc] init];
                textField.frame = CGRectMake(x - 60/2, 0, 60, 20);
                [textField setEditable:NO];
                [textField setBordered:NO];
                textField.backgroundColor = [NSColor clearColor];
                textField.stringValue = [NSString stringWithFormat:@"%d",self.xTextFieldValue+(self.xShortScale*i / self.xLongScale)];
                textField.alignment = NSTextAlignmentCenter;
                textField.textColor = [NSColor colorNamed:@"ChartText"];
                [self addSubview:textField];
                [self.xTextFields addObject:textField];
            }
            
            // 下
            NSBezierPath *path1 = [NSBezierPath bezierPath];
            [[NSColor blackColor]  set];
            [path1 setLineWidth:1.0f];
            [path1 moveToPoint:NSMakePoint( x, marginTop)];
            [path1 lineToPoint:NSMakePoint( x, marginTop + length)];
            [path1 stroke];
            // 上
            NSBezierPath *path2 = [NSBezierPath bezierPath];
            [[NSColor blackColor]  set];
            [path2 setLineWidth:1.0f];
            [path2 moveToPoint:NSMakePoint( x, marginTop + chanvasH)];
            [path2 lineToPoint:NSMakePoint( x, marginTop + chanvasH - length)];
            [path2 stroke];
        }
    }
    
    
    for (ChartData *chartData in self.datas) {
        if ([chartData.datas count] > 1) {
            
            NSBezierPath *path = [NSBezierPath bezierPath];
            [chartData.lineColor set];
            [path setLineWidth:1.0f];
            
            float wScal = chanvasW / (self.xMaxValue - self.xMinValue);
            float hScal = chanvasH / (self.yMaxValue - self.yMinValue);
            
            NSNumber *number = chartData.datas[0];
            // 始点
            [path moveToPoint:NSMakePoint( marginLeft, marginTop + chanvasH + hScal*[number floatValue] + hScal*self.yMinValue)];
            
            // 移動点
            for (int i = 1; i < [chartData.datas count]; i++) {
                NSNumber *number = chartData.datas[i];
                [path lineToPoint:NSMakePoint( marginLeft + wScal*i, marginTop + chanvasH + hScal*[number floatValue] + hScal*self.yMinValue)];
            }
            
            //描画
            [path stroke];
        }
    }

}

- (void)setChartData:(NSMutableArray *)datas lineColor:(NSColor *)color {
    ChartData *data = [[ChartData alloc] init];
    data.lineColor = color;
    data.datas = datas;
    [self.datas addObject:data];
}

@end
