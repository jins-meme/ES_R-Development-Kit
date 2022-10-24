//
//  ViewController.m
//  MEME_Academic
//
//  Created by D-CLUE on 2017/03/22.
//  Copyright © 2017年 jins-jp. All rights reserved.
//

#import "ViewController.h"

@interface ViewController ()<NSComboBoxDelegate, NSComboBoxDataSource>

@end

@implementation ViewController
{
    MEMELib_Academic* memelib;
    boolean_t connected_flag;
    boolean_t measurement_flag;
}

// =============================================================================
#pragma mark - viewDidLoad
// =============================================================================
- (void)viewDidLoad {
    [super viewDidLoad];

    // Do any additional setup after loading the view.
    
    memelib = [[MEMELib_Academic alloc] init];
    memelib.delegate = self;

    connected_flag = false;
    measurement_flag = false;
    
    [self.button_StartScan setHidden:NO];
    [self.button_Connect setHidden:YES];
    [self.button_StartMeasurement setHidden:YES];
    
    [self.combobox_MEME removeAllItems];

    [self.combobox_SelectMode insertItemWithObjectValue:@"Full" atIndex:[self.combobox_SelectMode numberOfItems]];
    [self.combobox_SelectMode selectItemAtIndex:(0)];
    
    [self.combobox_TransSpeed insertItemWithObjectValue:@"100Hz" atIndex:[self.combobox_TransSpeed numberOfItems]];
    [self.combobox_TransSpeed selectItemAtIndex:(0)];
    
    [self.combobox_AccelRange insertItemWithObjectValue:@"±2G" atIndex:[self.combobox_AccelRange numberOfItems]];
    [self.combobox_AccelRange insertItemWithObjectValue:@"±4G" atIndex:[self.combobox_AccelRange numberOfItems]];
    [self.combobox_AccelRange insertItemWithObjectValue:@"±8G" atIndex:[self.combobox_AccelRange numberOfItems]];
    [self.combobox_AccelRange insertItemWithObjectValue:@"±16" atIndex:[self.combobox_AccelRange numberOfItems]];
    [self.combobox_AccelRange selectItemAtIndex:(0)];

    [self.combobox_GyroRange insertItemWithObjectValue:@"±250dps" atIndex:[self.combobox_GyroRange numberOfItems]];
    [self.combobox_GyroRange insertItemWithObjectValue:@"±500dps" atIndex:[self.combobox_GyroRange numberOfItems]];
    [self.combobox_GyroRange insertItemWithObjectValue:@"±1000dps" atIndex:[self.combobox_GyroRange numberOfItems]];
    [self.combobox_GyroRange insertItemWithObjectValue:@"±2000dps" atIndex:[self.combobox_GyroRange numberOfItems]];
    [self.combobox_GyroRange selectItemAtIndex:(0)];
}


// =============================================================================
#pragma mark - setRepresentedObject
// =============================================================================
- (void)setRepresentedObject:(id)representedObject {
    [super setRepresentedObject:representedObject];

    // Update the view, if already loaded.
}

// =============================================================================
#pragma mark - memePeripheralFoundDelegate
// =============================================================================
- (void)memePeripheralFoundDelegate:(uint32_t)result DeviceName:(NSString*)name uuid:(NSString*)uuid {
    if (result == MEMELIB_OK) {
        NSLog(@"memePeripheralFoundDelegate %d %@ %@",result, name, uuid);

        [self.combobox_MEME insertItemWithObjectValue:name atIndex:[self.combobox_MEME numberOfItems]];
        [self.combobox_MEME selectItemAtIndex:([self.combobox_MEME numberOfItems]-1)];

        [self.button_StartScan setHidden:NO];
        [self.button_Connect setHidden:NO];
        [self.button_StartMeasurement setHidden:YES];
    }
    else {
        NSLog(@"memePeripheralFoundDelegate %d",result);
        NSLog(@"Call : stopScanningPeripherals");
        [memelib stopScanningPeripherals];
    }
}

// =============================================================================
#pragma mark - memePeripheralConnectedDelegate
// =============================================================================
- (void)memePeripheralConnectedDelegate:(uint32_t)result {
    NSLog(@"memePeripheralConnectedDelegate : %d",result);
    connected_flag = true;
    self.button_Connect.title = @"Disconnect";
    self.label_StateConnect.stringValue = @"State : Connected";

    [self.button_StartScan setHidden:YES];
    [self.button_Connect setHidden:NO];
    [self.button_StartMeasurement setHidden:NO];
}

// =============================================================================
#pragma mark - memePeripheralDisconnectedDelegate
// =============================================================================
- (void)memePeripheralDisconnectedDelegate:(uint32_t)result {
    NSLog(@"memePeripheralDisconnectedDelegate : %d",result);
    connected_flag = false;
    self.button_Connect.title =@"Connect";
    self.label_StateConnect.stringValue = @"State : Disconnected";

    [self.button_StartScan setHidden:NO];
    [self.button_Connect setHidden:YES];
    [self.button_StartMeasurement setHidden:YES];
}

// =============================================================================
#pragma mark - memeAcademicFullDataReceivedDelegate
// =============================================================================
- (void)memeAcademicFullDataReceivedDelegate:(AcademicFullData *)data {
    //NSLog(@"memeAcademicFullDataReceivedDelegate");
    self.label_DataCnt.stringValue = [NSString stringWithFormat:@"%d", data.Cnt];
    self.label_DataAccX.stringValue = [NSString stringWithFormat:@"%d", data.AccX];
    self.label_DataAccY.stringValue = [NSString stringWithFormat:@"%d", data.AccY];
    self.label_DataAccZ.stringValue = [NSString stringWithFormat:@"%d", data.AccZ];
    self.label_DataGyroX.stringValue = [NSString stringWithFormat:@"%d", data.GyroX];
    self.label_DataGyroY.stringValue = [NSString stringWithFormat:@"%d", data.GyroY];
    self.label_DataGyroZ.stringValue = [NSString stringWithFormat:@"%d", data.GyroZ];
    self.label_DataEogL.stringValue = [NSString stringWithFormat:@"%d", data.EogL];
    self.label_DataEogR.stringValue = [NSString stringWithFormat:@"%d", data.EogR];
    self.label_DataEogH.stringValue = [NSString stringWithFormat:@"%d", data.EogH];
    self.label_DataEogV.stringValue = [NSString stringWithFormat:@"%d", data.EogV];
    self.label_DataBattLv.stringValue = [NSString stringWithFormat:@"%d", data.BattLv];
}

// =============================================================================
#pragma mark - button_StartScan_Tapped
// =============================================================================
- (IBAction)button_StartScan_Tapped:(id)sender {
    NSLog(@"Call : startScanningPeripherals");
    [self.combobox_MEME removeAllItems];
    [memelib startScanningPeripherals];
}

// =============================================================================
#pragma mark - button_Connect_Tapped
// =============================================================================
- (IBAction)button_Connect_Tapped:(id)sender {
    if (connected_flag == false) {
        NSLog(@"Call : connectPeripheral");
        NSString* name = [self.combobox_MEME stringValue];
        [memelib connectPeripheral:(NSString*)name];
    }
    else {
        NSLog(@"Call : disconnectPeripheral");
        //NSString* name = [self.combobox_MEME stringValue];
        [memelib disconnectPeripheral];
    }
}

// =============================================================================
#pragma mark - button_StartMeasurement_Tapped
// =============================================================================
- (IBAction)button_StartMeasurement_Tapped:(id)sender {
    NSInteger accelrange = [self.combobox_AccelRange indexOfSelectedItem];
    NSInteger gyrorange = [self.combobox_GyroRange indexOfSelectedItem];
    
    if (measurement_flag == false) {
        self.button_StartMeasurement.title = @"Stop Measurement";

        [self.button_StartScan setHidden:YES];
        [self.button_Connect setHidden:YES];
        [self.button_StartMeasurement setHidden:NO];

        [memelib setSelectMode:MEMEMode_Full];
        [memelib setTransMode:MEMEQuality_High];
        [memelib setAccelRange:(uint32_t)accelrange];
        [memelib setGyroRange:(uint32_t)gyrorange];
        measurement_flag = true;
        [memelib startDataReport];
    }
    else {
        self.button_StartMeasurement.title = @"Start Measurement";
        
        [self.button_StartScan setHidden:YES];
        [self.button_Connect setHidden:NO];
        [self.button_StartMeasurement setHidden:NO];

        measurement_flag = false;
        [memelib stopDataReport];
    }
}
@end
