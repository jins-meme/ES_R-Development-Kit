//
//  AcademicFullData.h
//  MEME_Academic
//
//  Created by D-CLUE on 2017/03/22.
//  Copyright © 2017年 jins-jp. All rights reserved.
//

#import "AcademicData.h"

@interface AcademicFullData : AcademicData

@property int16_t AccX;
@property int16_t AccY;
@property int16_t AccZ;

@property int16_t GyroX;
@property int16_t GyroY;
@property int16_t GyroZ;

@property int16_t EogL;
@property int16_t EogR;

@property int16_t EogH;
@property int16_t EogV;

@end
