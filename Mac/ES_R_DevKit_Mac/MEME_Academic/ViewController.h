//
//  ViewController.h
//  MEME_Academic
//
//  Created by D-CLUE on 2017/03/22.
//  Copyright © 2017年 jins-jp. All rights reserved.
//

#import <Cocoa/Cocoa.h>
#import "MEMELib_Academic.h"
#import "TCPSocket.h"
#import "SettingViewController.h"

@interface ViewController : NSViewController<memelibDelegate,tcpSocketDelegate,SettingViewControllerDelegate>

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
@property (weak) IBOutlet NSButton *button_FreeMarking;
- (IBAction)button_FreeMarking_Tapped:(id)sender;

//@property (weak) IBOutlet NSTextField *label_DataCnt;
//@property (weak) IBOutlet NSTextField *label_DataAccX;
//@property (weak) IBOutlet NSTextField *label_DataAccY;
//@property (weak) IBOutlet NSTextField *label_DataAccZ;
//@property (weak) IBOutlet NSTextField *label_DataGyroX;
//@property (weak) IBOutlet NSTextField *label_DataGyroY;
//@property (weak) IBOutlet NSTextField *label_DataGyroZ;
//@property (weak) IBOutlet NSTextField *label_DataEogL;
//@property (weak) IBOutlet NSTextField *label_DataEogR;
//@property (weak) IBOutlet NSTextField *label_DataEogH;
//@property (weak) IBOutlet NSTextField *label_DataEogV;
//@property (weak) IBOutlet NSTextField *label_DataBattLv;


@property (weak) IBOutlet NSButton *button_Settings;
@property (weak) IBOutlet NSScrollView *scrollview_Chart1;
@property (weak) IBOutlet NSScrollView *scrollview_Chart2;
@property (weak) IBOutlet NSScrollView *scrollview_Chart3;
@property (weak) IBOutlet NSBox *box_BatteryLevel;
@property (weak) IBOutlet NSTextField *label_LocalAddress;
@property (weak) IBOutlet NSTextField *label_LocalProt;
@property (weak) IBOutlet NSTextField *label_SocketStatus;
@property (weak) IBOutlet NSButton *button_Chart_Apply;
@property (weak) IBOutlet NSTextField *label_AppVersion;
@property (weak) IBOutlet NSTextField *label_MemeVersion;
@property (weak) IBOutlet NSBox *box_SuccessRate;
@property (weak) IBOutlet NSTextField *label_SuccessRate;
@property (weak) IBOutlet NSBox *box_Communication;
@property (weak) IBOutlet NSTextField *label_Communication;

@property (weak) IBOutlet NSComboBox *combobox_Chart1;
@property (weak) IBOutlet NSComboBox *combobox_Chart2;
@property (weak) IBOutlet NSComboBox *combobox_Chart3;

@property (weak) IBOutlet NSTextField *textField_Chart1;
@property (weak) IBOutlet NSBox *box_Chart1_Electrooculography;
@property (weak) IBOutlet NSButton *button_Chart1_Electrooculography_Left;
@property (weak) IBOutlet NSButton *button_Chart1_Electrooculography_Right;
@property (weak) IBOutlet NSButton *button_Chart1_Electrooculography_DeltaH;
@property (weak) IBOutlet NSButton *button_Chart1_Electrooculography_DeltaV;
@property (weak) IBOutlet NSBox *box_Chart1_Gyroscope;
@property (weak) IBOutlet NSButton *button_Chart1_Gyroscope_X_Axis;
@property (weak) IBOutlet NSButton *button_Chart1_Gyroscope_Y_Axis;
@property (weak) IBOutlet NSButton *button_Chart1_Gyroscope_Z_Axis;
@property (weak) IBOutlet NSBox *box_Chart1_Accelerometer;
@property (weak) IBOutlet NSButton *button_Chart1_Accelerometer_X_Axis;
@property (weak) IBOutlet NSButton *button_Chart1_Accelerometer_Y_Axis;
@property (weak) IBOutlet NSButton *button_Chart1_Accelerometer_Z_Axis;
@property (weak) IBOutlet NSTextField *textField_Chart1_Y1;
@property (weak) IBOutlet NSTextField *textField_Chart1_Y2;
@property (weak) IBOutlet NSTextField *textField_Chart1_Y3;
@property (weak) IBOutlet NSTextField *textField_Chart1_Y4;
@property (weak) IBOutlet NSTextField *textField_Chart1_Y5;
@property (weak) IBOutlet NSTextField *textField_Chart1_Y6;
@property (weak) IBOutlet NSTextField *textField_Chart1_Y7;

@property (weak) IBOutlet NSTextField *textField_Chart2;
@property (weak) IBOutlet NSBox *box_Chart2_Electrooculography;
@property (weak) IBOutlet NSButton *button_Chart2_Electrooculography_Left;
@property (weak) IBOutlet NSButton *button_Chart2_Electrooculography_Right;
@property (weak) IBOutlet NSButton *button_Chart2_Electrooculography_DeltaH;
@property (weak) IBOutlet NSButton *button_Chart2_Electrooculography_DeltaV;
@property (weak) IBOutlet NSBox *box_Chart2_Gyroscope;
@property (weak) IBOutlet NSButton *button_Chart2_Gyroscope_X_Axis;
@property (weak) IBOutlet NSButton *button_Chart2_Gyroscope_Y_Axis;
@property (weak) IBOutlet NSButton *button_Chart2_Gyroscope_Z_Axis;
@property (weak) IBOutlet NSBox *box_Chart2_Accelerometer;
@property (weak) IBOutlet NSButton *button_Chart2_Accelerometer_X_Axis;
@property (weak) IBOutlet NSButton *button_Chart2_Accelerometer_Y_Axis;
@property (weak) IBOutlet NSButton *button_Chart2_Accelerometer_Z_Axis;
@property (weak) IBOutlet NSTextField *textField_Chart2_Y1;
@property (weak) IBOutlet NSTextField *textField_Chart2_Y2;
@property (weak) IBOutlet NSTextField *textField_Chart2_Y3;
@property (weak) IBOutlet NSTextField *textField_Chart2_Y4;
@property (weak) IBOutlet NSTextField *textField_Chart2_Y5;
@property (weak) IBOutlet NSTextField *textField_Chart2_Y6;
@property (weak) IBOutlet NSTextField *textField_Chart2_Y7;

@property (weak) IBOutlet NSTextField *textField_Chart3;
@property (weak) IBOutlet NSBox *box_Chart3_Electrooculography;
@property (weak) IBOutlet NSButton *button_Chart3_Electrooculography_Left;
@property (weak) IBOutlet NSButton *button_Chart3_Electrooculography_Right;
@property (weak) IBOutlet NSButton *button_Chart3_Electrooculography_DeltaH;
@property (weak) IBOutlet NSButton *button_Chart3_Electrooculography_DeltaV;
@property (weak) IBOutlet NSBox *box_Chart3_Gyroscope;
@property (weak) IBOutlet NSButton *button_Chart3_Gyroscope_X_Axis;
@property (weak) IBOutlet NSButton *button_Chart3_Gyroscope_Y_Axis;
@property (weak) IBOutlet NSButton *button_Chart3_Gyroscope_Z_Axis;
@property (weak) IBOutlet NSBox *box_Chart3_Accelerometer;
@property (weak) IBOutlet NSButton *button_Chart3_Accelerometer_X_Axis;
@property (weak) IBOutlet NSButton *button_Chart3_Accelerometer_Y_Axis;
@property (weak) IBOutlet NSButton *button_Chart3_Accelerometer_Z_Axis;
@property (weak) IBOutlet NSTextField *textField_Chart3_Y1;
@property (weak) IBOutlet NSTextField *textField_Chart3_Y2;
@property (weak) IBOutlet NSTextField *textField_Chart3_Y3;
@property (weak) IBOutlet NSTextField *textField_Chart3_Y4;
@property (weak) IBOutlet NSTextField *textField_Chart3_Y5;
@property (weak) IBOutlet NSTextField *textField_Chart3_Y6;
@property (weak) IBOutlet NSTextField *textField_Chart3_Y7;

@end

