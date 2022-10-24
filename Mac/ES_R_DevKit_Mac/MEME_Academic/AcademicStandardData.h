//
//  AcademicStandardData.h
//  MEME_Academic
//
//  Created by Celleus on 2022/09/12.
//  Copyright © 2022 jins-jp. All rights reserved.
//

#import "AcademicData.h"

NS_ASSUME_NONNULL_BEGIN

@interface AcademicStandardData : AcademicData

@property int16_t AccX;
@property int16_t AccY;
@property int16_t AccZ;

@property int16_t EogL1;
@property int16_t EogR1;
@property int16_t EogL2;
@property int16_t EogR2;

@property int16_t EogH1;
@property int16_t EogH2;

@property int16_t EogV1;
@property int16_t EogV2;

@end

NS_ASSUME_NONNULL_END
