//
//  ViewController.h
//  MEME_Academic
//
//  Created by D-CLUE on 2017/03/22.
//  Copyright © 2017年 jins-jp. All rights reserved.
//

#import <Cocoa/Cocoa.h>
#import "MEMELib_Academic.h"

@interface ViewController : NSViewController<memelibDelegate>

@property (weak) IBOutlet NSButton *button_StartScan;
- (IBAction)button_StartScan_Tapped:(id)sender;

@property (weak) IBOutlet NSButton *button_Connect;
- (IBAction)button_Connect_Tapped:(id)sender;

@property (weak) IBOutlet NSComboBox *combobox_MEME;

@property (weak) IBOutlet NSTextField *label_StateConnect;

@property (weak) IBOutlet NSComboBox *combobox_SelectMode;
@property (weak) IBOutlet NSComboBox *combobox_TransSpeed;
@property (weak) IBOutlet NSComboBox *combobox_AccelRange;
@property (weak) IBOutlet NSComboBox *combobox_GyroRange;

@property (weak) IBOutlet NSButton *button_StartMeasurement;
- (IBAction)button_StartMeasurement_Tapped:(id)sender;

@property (weak) IBOutlet NSTextField *label_DataCnt;
@property (weak) IBOutlet NSTextField *label_DataAccX;
@property (weak) IBOutlet NSTextField *label_DataAccY;
@property (weak) IBOutlet NSTextField *label_DataAccZ;
@property (weak) IBOutlet NSTextField *label_DataGyroX;
@property (weak) IBOutlet NSTextField *label_DataGyroY;
@property (weak) IBOutlet NSTextField *label_DataGyroZ;
@property (weak) IBOutlet NSTextField *label_DataEogL;
@property (weak) IBOutlet NSTextField *label_DataEogR;
@property (weak) IBOutlet NSTextField *label_DataEogH;
@property (weak) IBOutlet NSTextField *label_DataEogV;
@property (weak) IBOutlet NSTextField *label_DataBattLv;

@end

