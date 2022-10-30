//
//  ViewController.m
//  MEME_Academic
//
//  Created by D-CLUE on 2017/03/22.
//  Copyright © 2017年 jins-jp. All rights reserved.
//

#import "ViewController.h"
#import "Const.h"
#import "Common.h"
#import "UserSetting.h"
#import "CsvManager.h"
#import "ChartView.h"

@interface ViewController ()<NSComboBoxDelegate, NSComboBoxDataSource, CBPeripheralManagerDelegate>

@end

@implementation ViewController
{
    MEMELib_Academic* memelib;
    boolean_t connected_flag;
    boolean_t measurement_flag;
    
    NSMutableArray *csvDatas;
    CsvManager *csvManager;
    long mPrevCount;
    long mPrevTime;
    long mTotalCount;
    long mErrorCount;
    long mQuality;
    long dataCount;
    NSDate *startDate;
    long dataCount200ms;
    NSMutableArray *socketDatas;
    NSMutableArray *chartDatas;
    Boolean isFreeMarking;
    
    NSTimer *communicationTimer;
    ChartView *chartView1;
    ChartView *chartView2;
    ChartView *chartView3;
    TCPSocket *socket;
    
    NSMutableArray *chart1Y;
    NSMutableArray *chart2Y;
    NSMutableArray *chart3Y;
    
    CBPeripheralManager *peripheralManager;
}

// =============================================================================
#pragma mark - viewDidLoad
// =============================================================================
- (void)viewDidLoad {
    [super viewDidLoad];

    // Do any additional setup after loading the view.
    
    [UserSetting fristSetting];
    
    memelib = [[MEMELib_Academic alloc] init];
    memelib.delegate = self;

    connected_flag = false;
    measurement_flag = false;
    
    chart1Y = [[NSMutableArray alloc] init];
    [chart1Y addObject:self.textField_Chart1_Y1];
    [chart1Y addObject:self.textField_Chart1_Y2];
    [chart1Y addObject:self.textField_Chart1_Y3];
    [chart1Y addObject:self.textField_Chart1_Y4];
    [chart1Y addObject:self.textField_Chart1_Y5];
    [chart1Y addObject:self.textField_Chart1_Y6];
    [chart1Y addObject:self.textField_Chart1_Y7];
    chart2Y = [[NSMutableArray alloc] init];
    [chart2Y addObject:self.textField_Chart2_Y1];
    [chart2Y addObject:self.textField_Chart2_Y2];
    [chart2Y addObject:self.textField_Chart2_Y3];
    [chart2Y addObject:self.textField_Chart2_Y4];
    [chart2Y addObject:self.textField_Chart2_Y5];
    [chart2Y addObject:self.textField_Chart2_Y6];
    [chart2Y addObject:self.textField_Chart2_Y7];
    chart3Y = [[NSMutableArray alloc] init];
    [chart3Y addObject:self.textField_Chart3_Y1];
    [chart3Y addObject:self.textField_Chart3_Y2];
    [chart3Y addObject:self.textField_Chart3_Y3];
    [chart3Y addObject:self.textField_Chart3_Y4];
    [chart3Y addObject:self.textField_Chart3_Y5];
    [chart3Y addObject:self.textField_Chart3_Y6];
    [chart3Y addObject:self.textField_Chart3_Y7];
    
    [self reset];
    
    [self.button_StartScan setHidden:NO];
    [self.button_Connect setHidden:YES];
    [self.button_StartMeasurement setHidden:YES];
    [self.button_FreeMarking setHidden:YES];
    
    [self.combobox_MEME removeAllItems];

    [self.combobox_SelectMode insertItemWithObjectValue:@"Standard" atIndex:[self.combobox_SelectMode numberOfItems]];
    [self.combobox_SelectMode insertItemWithObjectValue:@"Full" atIndex:[self.combobox_SelectMode numberOfItems]];
    [self.combobox_SelectMode insertItemWithObjectValue:@"Quaternion" atIndex:[self.combobox_SelectMode numberOfItems]];
    [self.combobox_SelectMode selectItemAtIndex:(0)];
    
    [self.combobox_TransSpeed insertItemWithObjectValue:@"100Hz" atIndex:[self.combobox_TransSpeed numberOfItems]];
    [self.combobox_TransSpeed insertItemWithObjectValue:@"50Hz" atIndex:[self.combobox_TransSpeed numberOfItems]];
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
    
    [self.combobox_Chart1 insertItemWithObjectValue:@"Electrooculography" atIndex:[self.combobox_Chart1 numberOfItems]];
    [self.combobox_Chart1 insertItemWithObjectValue:@"Gyroscope" atIndex:[self.combobox_Chart1 numberOfItems]];
    [self.combobox_Chart1 insertItemWithObjectValue:@"Accelerometer" atIndex:[self.combobox_Chart1 numberOfItems]];
    [self.combobox_Chart1 selectItemAtIndex:(0)];
    
    [self.combobox_Chart2 insertItemWithObjectValue:@"Electrooculography" atIndex:[self.combobox_Chart2 numberOfItems]];
    [self.combobox_Chart2 insertItemWithObjectValue:@"Gyroscope" atIndex:[self.combobox_Chart2 numberOfItems]];
    [self.combobox_Chart2 insertItemWithObjectValue:@"Accelerometer" atIndex:[self.combobox_Chart2 numberOfItems]];
    [self.combobox_Chart2 selectItemAtIndex:(1)];
    
    [self.combobox_Chart3 insertItemWithObjectValue:@"Electrooculography" atIndex:[self.combobox_Chart3 numberOfItems]];
    [self.combobox_Chart3 insertItemWithObjectValue:@"Gyroscope" atIndex:[self.combobox_Chart3 numberOfItems]];
    [self.combobox_Chart3 insertItemWithObjectValue:@"Accelerometer" atIndex:[self.combobox_Chart3 numberOfItems]];
    [self.combobox_Chart3 selectItemAtIndex:(2)];
    
    [self appVersion];
    chartView1 = [self setChartView:self.scrollview_Chart1
                          xMaxValue:200
                          xMinValue:0
                          yMaxValue:1200
                          yMinValue:-1200];
    chartView2 = [self setChartView:self.scrollview_Chart2
                          xMaxValue:200
                          xMinValue:0
                          yMaxValue:36000
                          yMinValue:-36000];
    chartView3 = [self setChartView:self.scrollview_Chart3
                          xMaxValue:200
                          xMinValue:0
                          yMaxValue:36000
                          yMinValue:-36000];
    [self localAddress];
    [self localPort];
    [self batteryLevel:0];
    [self successRate];
    [self communication];
    _label_MemeVersion.stringValue = @"MEME Version：";
    
    [self socketStart];
    

}

- (void)viewWillAppear {
    [super viewWillAppear];
    self.view.window.title = @"JINS MEME Academic";
}

- (void)reset {
    csvDatas = [[NSMutableArray alloc] init];
    csvManager = [[CsvManager alloc] init];
    mPrevCount = -1;
    mPrevTime = 0;
    mTotalCount = 0;
    mErrorCount = 0;
    mQuality = 1;
    dataCount = 0;
    startDate = [NSDate date];
    dataCount200ms = 0;
    socketDatas = [[NSMutableArray alloc] init];
    chartDatas = [[NSMutableArray alloc] init];
    self.label_SocketStatus.stringValue = @"Status : ";
    chartView1.xTextFieldValue = 0;
    chartView1.xInitialPosition = 0;
    chartView2.xTextFieldValue = 0;
    chartView2.xInitialPosition = 0;
    chartView3.xTextFieldValue = 0;
    chartView3.xInitialPosition = 0;
    isFreeMarking = NO;
}



// =============================================================================
#pragma mark - appVersion
// =============================================================================
- (void)appVersion {
    NSLog(@"appVersion");
    NSString *version = [[NSBundle mainBundle] objectForInfoDictionaryKey: @"CFBundleShortVersionString"];
    NSString *build = [[[NSBundle mainBundle] infoDictionary] objectForKey:@"CFBundleVersion"];
    _label_AppVersion.stringValue = [NSString stringWithFormat:@"Version %@.%@",version,build];
}

// =============================================================================
#pragma mark - localAddress
// =============================================================================
- (void)localAddress {
    NSLog(@"localAddress");
    NSString *localAddress = [Common getIPAddress];
    _label_LocalAddress.stringValue = [NSString stringWithFormat:@"IP address:%@",localAddress];
//    [UserSetting setLocalAddress:localAddress];
}

// =============================================================================
#pragma mark - localPort
// =============================================================================
- (void)localPort {
    NSLog(@"localPort");
    NSString *localPort = [UserSetting getLocalPort];
    _label_LocalProt.stringValue = [NSString stringWithFormat:@"Prot:%@",localPort];
}

// =============================================================================
#pragma mark - batteryLevel
// =============================================================================
- (void)batteryLevel:(int)BattLv {
    _box_BatteryLevel.frame = CGRectMake(0, 0, 100/5*BattLv, _box_BatteryLevel.frame.size.height);
}

// =============================================================================
#pragma mark - memeVersion
// =============================================================================
- (void)memeVersion {
    NSLog(@"memeVersion");
    _label_MemeVersion.stringValue = [NSString stringWithFormat:@"MEME Version：%d.%d.%d",memelib.MEMEVersion.Major,memelib.MEMEVersion.Minor,memelib.MEMEVersion.Revision];
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
        [self.button_FreeMarking setHidden:YES];
    }
    else {
        NSLog(@"memePeripheralFoundDelegate %d",result);
        NSLog(@"Call : stopScanningPeripherals");
        [self.button_StartScan setEnabled:YES];
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

    [self.button_StartScan setEnabled:YES];
    [self.button_StartScan setHidden:YES];
    [self.button_Connect setEnabled:YES];
    [self.button_Connect setHidden:NO];
    [self.button_StartMeasurement setHidden:NO];
    
    [self memeVersion];
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
    
    [self.combobox_MEME setEnabled:YES];
}

- (NSDictionary *)dataToDictionary:(AcademicData *)data {
    long count = data.Cnt;
    long deff = 0;
    if (mPrevCount < 0) {
        deff = 0;
        mPrevTime = [[NSDate date] timeIntervalSince1970];
    } else {
        if (mPrevCount < (long) count) {
            deff = (long) count - mPrevCount;
        } else if (mPrevCount > (long) count) {
            deff = 0x1000 - mPrevCount + (long) count;
        }
    }
    mPrevCount = count;
    mPrevTime += deff * 10 * mQuality;

    if (deff == 0) {
        mTotalCount += deff + 1;
        mErrorCount += deff;
    } else {
        mTotalCount += deff;
        if (deff - 1 > 0) {
            mErrorCount += deff - 1;
        }
    }
//    NSLog(@"mTotalCount:%ld",mTotalCount);
    
    return @{@"data":data,
             @"packetCount":[NSNumber numberWithLong:mTotalCount],
             @"date":[NSDate date],
             @"isFreeMarking":[NSNumber numberWithBool:isFreeMarking]};
}

// =============================================================================
#pragma mark - memeAcademicStandardDataReceivedDelegate
// =============================================================================
- (void)memeAcademicStandardDataReceivedDelegate:(AcademicStandardData *)data {
    //NSLog(@"memeAcademicStandardDataReceivedDelegate");
    [csvDatas addObject:[self dataToDictionary:data]];
    isFreeMarking = NO;
    [self saveCsv];
    if ([socket isConnected]) {
        [socketDatas addObject:[[NSDictionary alloc] initWithDictionary:[csvDatas lastObject]]];
        [self writeSocket];
    }
    [self batteryLevelAndSuccessRate:data.BattLv];
    
    int interval = ([memelib getTransMode] == MEMEQuality_High) ? 4 : 2;
    if (mTotalCount % interval == 0) {
        [chartDatas addObject:data];
        if (self.combobox_Chart1.indexOfSelectedItem == 0) {
            chartView1.yMaxValue = 1200;
            chartView1.yMinValue = -1200;
            [self drowStandardElectooculographyChart:chartView1
                                           datas:chartDatas
                                          isLeft:self.button_Chart1_Electrooculography_Left.state
                                         isRight:self.button_Chart1_Electrooculography_Right.state
                                        isDelftH:self.button_Chart1_Electrooculography_DeltaH.state
                                        isDelftV:self.button_Chart1_Electrooculography_DeltaV.state];
        }
        else if (self.combobox_Chart1.indexOfSelectedItem == 2) {
            chartView1.yMaxValue = 36000;
            chartView1.yMinValue = -36000;
            [self drowStandardAccelerometerChart:chartView1
                                           datas:chartDatas
                                          isAccX:self.button_Chart1_Accelerometer_X_Axis.state
                                          isAccY:self.button_Chart1_Accelerometer_Y_Axis.state
                                          isAccZ:self.button_Chart1_Accelerometer_Z_Axis.state];
        }

        if (self.combobox_Chart2.indexOfSelectedItem == 0) {
            chartView2.yMaxValue = 1200;
            chartView2.yMinValue = -1200;
            [self drowStandardElectooculographyChart:chartView2
                                           datas:chartDatas
                                          isLeft:self.button_Chart2_Electrooculography_Left.state
                                         isRight:self.button_Chart2_Electrooculography_Right.state
                                        isDelftH:self.button_Chart2_Electrooculography_DeltaH.state
                                        isDelftV:self.button_Chart2_Electrooculography_DeltaV.state];
        }
        else if (self.combobox_Chart2.indexOfSelectedItem == 2) {
            chartView2.yMaxValue = 36000;
            chartView2.yMinValue = -36000;
            [self drowStandardAccelerometerChart:chartView2
                                           datas:chartDatas
                                          isAccX:self.button_Chart2_Accelerometer_X_Axis.state
                                          isAccY:self.button_Chart2_Accelerometer_Y_Axis.state
                                          isAccZ:self.button_Chart2_Accelerometer_Z_Axis.state];
        }
        
        if (self.combobox_Chart3.indexOfSelectedItem == 0) {
            chartView3.yMaxValue = 1200;
            chartView3.yMinValue = -1200;
            [self drowStandardElectooculographyChart:chartView3
                                           datas:chartDatas
                                          isLeft:self.button_Chart3_Electrooculography_Left.state
                                         isRight:self.button_Chart3_Electrooculography_Right.state
                                        isDelftH:self.button_Chart3_Electrooculography_DeltaH.state
                                        isDelftV:self.button_Chart3_Electrooculography_DeltaV.state];
        }
        else if (self.combobox_Chart3.indexOfSelectedItem == 2) {
            chartView3.yMaxValue = 36000;
            chartView3.yMinValue = -36000;
            [self drowStandardAccelerometerChart:chartView3
                                           datas:chartDatas
                                          isAccX:self.button_Chart3_Accelerometer_X_Axis.state
                                          isAccY:self.button_Chart3_Accelerometer_Y_Axis.state
                                          isAccZ:self.button_Chart3_Accelerometer_Z_Axis.state];
        }
    }
}

// =============================================================================
#pragma mark - memeAcademicFullDataReceivedDelegate
// =============================================================================
- (void)memeAcademicFullDataReceivedDelegate:(AcademicFullData *)data {
    //NSLog(@"memeAcademicFullDataReceivedDelegate");
    [csvDatas addObject:[self dataToDictionary:data]];
    [self saveCsv];
    if ([socket isConnected]) {
        [socketDatas addObject:[[NSDictionary alloc] initWithDictionary:[csvDatas lastObject]]];
        [self writeSocket];
    }
    [self batteryLevelAndSuccessRate:data.BattLv];
    
    int interval = ([memelib getTransMode] == MEMEQuality_High) ? 4 : 2;
    if (mTotalCount % interval == 0) {
        [chartDatas addObject:data];
        if (self.combobox_Chart1.indexOfSelectedItem == 0) {
            chartView1.yMaxValue = 1200;
            chartView1.yMinValue = -1200;
            [self drowFullElectooculographyChart:chartView1
                                           datas:chartDatas
                                          isLeft:self.button_Chart1_Electrooculography_Left.state
                                         isRight:self.button_Chart1_Electrooculography_Right.state
                                        isDelftH:self.button_Chart1_Electrooculography_DeltaH.state
                                        isDelftV:self.button_Chart1_Electrooculography_DeltaV.state];
        }
        else if (self.combobox_Chart1.indexOfSelectedItem == 1) {
            chartView1.yMaxValue = 36000;
            chartView1.yMinValue = -36000;
            [self drowFullGyroscopeChart:chartView1
                                   datas:chartDatas
                                 isGyroX:self.button_Chart1_Gyroscope_X_Axis.state
                                 isGyroY:self.button_Chart1_Gyroscope_Y_Axis.state
                                 isGyroZ:self.button_Chart1_Gyroscope_Z_Axis.state];
        }
        else if (self.combobox_Chart1.indexOfSelectedItem == 2) {
            chartView1.yMaxValue = 36000;
            chartView1.yMinValue = -36000;
            [self drowFullAccelerometerChart:chartView1
                                       datas:chartDatas
                                      isAccX:self.button_Chart1_Accelerometer_X_Axis.state
                                      isAccY:self.button_Chart1_Accelerometer_Y_Axis.state
                                      isAccZ:self.button_Chart1_Accelerometer_Z_Axis.state];
        }

        if (self.combobox_Chart2.indexOfSelectedItem == 0) {
            chartView2.yMaxValue = 1200;
            chartView2.yMinValue = -1200;
            [self drowFullElectooculographyChart:chartView2
                                           datas:chartDatas
                                          isLeft:self.button_Chart2_Electrooculography_Left.state
                                         isRight:self.button_Chart2_Electrooculography_Right.state
                                        isDelftH:self.button_Chart2_Electrooculography_DeltaH.state
                                        isDelftV:self.button_Chart2_Electrooculography_DeltaV.state];
        }
        else if (self.combobox_Chart2.indexOfSelectedItem == 1) {
            chartView2.yMaxValue = 36000;
            chartView2.yMinValue = -36000;
            [self drowFullGyroscopeChart:chartView2
                                   datas:chartDatas
                                 isGyroX:self.button_Chart2_Gyroscope_X_Axis.state
                                 isGyroY:self.button_Chart2_Gyroscope_Y_Axis.state
                                 isGyroZ:self.button_Chart2_Gyroscope_Z_Axis.state];
        }
        else if (self.combobox_Chart2.indexOfSelectedItem == 2) {
            chartView2.yMaxValue = 36000;
            chartView2.yMinValue = -36000;
            [self drowFullAccelerometerChart:chartView2
                                       datas:chartDatas
                                      isAccX:self.button_Chart2_Accelerometer_X_Axis.state
                                      isAccY:self.button_Chart2_Accelerometer_Y_Axis.state
                                      isAccZ:self.button_Chart2_Accelerometer_Z_Axis.state];
        }

        if (self.combobox_Chart3.indexOfSelectedItem == 0) {
            chartView3.yMaxValue = 1200;
            chartView3.yMinValue = -1200;
            [self drowFullElectooculographyChart:chartView3
                                           datas:chartDatas
                                          isLeft:self.button_Chart3_Electrooculography_Left.state
                                         isRight:self.button_Chart3_Electrooculography_Right.state
                                        isDelftH:self.button_Chart3_Electrooculography_DeltaH.state
                                        isDelftV:self.button_Chart3_Electrooculography_DeltaV.state];
        }
        else if (self.combobox_Chart3.indexOfSelectedItem == 1) {
            chartView3.yMaxValue = 36000;
            chartView3.yMinValue = -36000;
            [self drowFullGyroscopeChart:chartView3
                                   datas:chartDatas
                                 isGyroX:self.button_Chart3_Gyroscope_X_Axis.state
                                 isGyroY:self.button_Chart3_Gyroscope_Y_Axis.state
                                 isGyroZ:self.button_Chart3_Gyroscope_Z_Axis.state];
        }
        else if (self.combobox_Chart3.indexOfSelectedItem == 2) {
            chartView3.yMaxValue = 36000;
            chartView3.yMinValue = -36000;
            [self drowFullAccelerometerChart:chartView3
                                       datas:chartDatas
                                      isAccX:self.button_Chart3_Accelerometer_X_Axis.state
                                      isAccY:self.button_Chart3_Accelerometer_Y_Axis.state
                                      isAccZ:self.button_Chart3_Accelerometer_Z_Axis.state];
        }
    }
}

// =============================================================================
#pragma mark - memeAcademicQuaternionDataReceivedDelegate
// =============================================================================
- (void)memeAcademicQuaternionDataReceivedDelegate:(AcademicQuaternionData *)data {
    //NSLog(@"memeAcademicQuaternionDataReceivedDelegate");
    [csvDatas addObject:[self dataToDictionary:data]];
    [self saveCsv];
    if ([socket isConnected]) {
        [socketDatas addObject:[[NSDictionary alloc] initWithDictionary:[csvDatas lastObject]]];
        [self writeSocket];
    }
    [self batteryLevelAndSuccessRate:data.BattLv];
}

// =============================================================================
#pragma mark - batteryLevelAndSuccessRate
// =============================================================================

- (void)batteryLevelAndSuccessRate:(int)BattLv {
    dataCount++;
    [self successRate];
    dataCount200ms++;
    [self batteryLevel:BattLv];
}

// =============================================================================
#pragma mark - saveCsv
// =============================================================================

- (void)saveCsv {
    
    // 100Hzばら100件たまったら、50Hzなら50件たまったら保存開始
    if ([csvDatas count] >= 100/mQuality) {

        // 保存中か確認
        if (!csvManager.isSave) {
            NSLog(@"作成");
            // 未保存なのでファイルから作成
            NSString *directoryPath = [UserSetting getSaveFilePath];
            
            NSDateFormatter *dateFormatter =[[NSDateFormatter alloc] init];
            [dateFormatter setLocale:[[NSLocale alloc] initWithLocaleIdentifier:@"ja_JP"]]; // Localeの指定
            [dateFormatter setDateFormat:@"yyyyMMddHHmmss"];
            NSString *dateString = [dateFormatter stringFromDate:[NSDate date]];
            NSString *macAddressString = memelib.macAddress;
            NSString *fileName = [NSString stringWithFormat:@"%@_%@.csv",macAddressString,dateString];
            NSString *stringBuffer = [self headerString];
            [self dataToStoring:csvDatas stringBuffer:stringBuffer];
            NSData *data = [stringBuffer dataUsingEncoding:NSUTF8StringEncoding];
            
            // 作成
            [csvManager createWithDirectoryPath:directoryPath fileName:fileName firstData:data];
        }
        else {
            NSLog(@"追記");
            // 保存中なので追記
            NSMutableString *stringBuffer = [[NSMutableString alloc] init];
            [self dataToStoring:csvDatas stringBuffer:stringBuffer];
            NSData *data = [stringBuffer dataUsingEncoding:NSUTF8StringEncoding];
            // 追記
            [csvManager appendData:data];
        }

        [csvDatas removeAllObjects];
    }
}

// =============================================================================
#pragma mark - writeSocket
// =============================================================================

- (void)writeSocket {
    // 10件たまったらソケットに送信
    if ([socketDatas count] >= 10) {
        
        NSMutableString *stringBuffer = [[NSMutableString alloc] init];
        [self dataToStoring:socketDatas stringBuffer:stringBuffer];
        [socket writeData:stringBuffer];
        
        [socketDatas removeAllObjects];
    }
}

- (NSString *)headerString {
    NSString *selectMode = ([memelib getSelectMode] == MEMEMode_Standard) ? @"Standard" : ([memelib getSelectMode] == MEMEMode_Full) ? @"Full" : @"Quaternion";
    NSString *transMode = ([memelib getTransMode] == MEMEQuality_High) ? @"100Hz" : @"50Hz";
    NSString *accelRange = ([memelib getAccelRange] == MEMEAccelRange_2G) ? @"2g" : ([memelib getAccelRange] == MEMEAccelRange_4G) ? @"4g" : ([memelib getAccelRange] == MEMEAccelRange_8G) ? @"8g" : @"16g";
    NSString *gyroRange = ([memelib getGyroRange] == MEMEGyroRange_250dps) ? @"250dps" : ([memelib getGyroRange] == MEMEGyroRange_500dps) ? @"500dps" : ([memelib getGyroRange] == MEMEGyroRange_1000dps) ? @"1000dps" : @"2000dps";
    NSMutableString *headerString = [[NSMutableString alloc] init];
    [headerString appendString:[NSString stringWithFormat:@"// Data mode  : %@\n",selectMode]];
    [headerString appendString:[NSString stringWithFormat:@"// Transmission speed  : %@\n",transMode]];
    [headerString appendString:[NSString stringWithFormat:@"// Acceleration sensor's range  : %@\n",accelRange]];
    [headerString appendString:[NSString stringWithFormat:@"// Gyroscope sensor's range  : %@\n",gyroRange]];
    [headerString appendString:@"//\n"];
    if ([memelib getSelectMode] == MEMEMode_Standard) {
        [headerString appendString:@"//ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,EOG_L1,EOG_R1,EOG_L2,EOG_R2,EOG_H1,EOG_H2,EOG_V1,EOG_V2\n"];
    }
    else if ([memelib getSelectMode] == MEMEMode_Full) {
        [headerString appendString:@"//ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,GYRO_X,GYRO_Y,GYRO_Z,EOG_L,EOG_R,EOG_H,EOG_V\n"];
    }
    else {
        [headerString appendString:@"//ARTIFACT,NUM,DATE,QUATERNION_W,QUATERNION_X,QUATERNION_Y,QUATERNION_Z\n"];
    }
    return headerString;
}

- (void)dataToStoring:(NSMutableArray *)datas stringBuffer:(NSMutableString *)stringBuffer {
    
    for (NSDictionary *dic in datas) {
        
        NSDate *date = dic[@"date"];
        NSDateFormatter *dateFormatter = [[NSDateFormatter alloc] init];
        [dateFormatter setDateFormat:@"yyyy/MM/dd HH:mm:ss.SS"];
        NSString *dateString = [dateFormatter stringFromDate:date];
//        NSLog(@"dateString:%@",dateString);
        AcademicData *data = dic[@"data"];
        NSNumber *packetCount = dic[@"packetCount"];
        NSNumber *isFreeMarking = dic[@"isFreeMarking"];
        NSString *isFreeMarkingString = [isFreeMarking boolValue] ? @"x" : @"";
        
        if ([memelib getSelectMode] == MEMEMode_Standard) {
            AcademicStandardData *standardData = data;
            [stringBuffer appendString:[NSString stringWithFormat:@"%@,%@,%@,%d,%d,%d,%d,%d,%d,%d,%d,%d,%d,%d\n",isFreeMarkingString,packetCount,dateString,standardData.AccX,standardData.AccY,standardData.AccZ,standardData.EogL1,standardData.EogR1,standardData.EogL2,standardData.EogR2,standardData.EogH1,standardData.EogH2,standardData.EogV1,standardData.EogV2]];
        }
        else if ([memelib getSelectMode] == MEMEMode_Full) {
            AcademicFullData *fullData = data;
            [stringBuffer appendString:[NSString stringWithFormat:@"%@,%@,%@,%d,%d,%d,%d,%d,%d,%d,%d,%d,%d\n",isFreeMarkingString,packetCount,dateString,fullData.AccX,fullData.AccY,fullData.AccZ,fullData.GyroX,fullData.GyroY,fullData.GyroZ,fullData.EogL,fullData.EogR,fullData.EogH,fullData.EogV]];
        }
        else {
            AcademicQuaternionData *quaternionData = data;
            [stringBuffer appendString:[NSString stringWithFormat:@"%@,%@,%@,%d,%d,%d,%d\n",isFreeMarkingString,packetCount,dateString,quaternionData.QuaternionW,quaternionData.QuaternionX,quaternionData.QuaternionY,quaternionData.QuaternionZ]];
        }
    }
}

// =============================================================================
#pragma mark - successRate
// =============================================================================
- (void)successRate {
    double timeCount = [[NSDate date] timeIntervalSince1970] - [startDate timeIntervalSince1970];
    double successRate = (dataCount/(timeCount*100.0/mQuality))*100.0;
    _label_SuccessRate.stringValue = [NSString stringWithFormat:@"%.2f%%",successRate];
    _box_SuccessRate.frame = CGRectMake(0, 0, successRate, _box_SuccessRate.frame.size.height);
}

// =============================================================================
#pragma mark - startCommunicationTimer
// =============================================================================
- (void)startCommunicationTimer {
    communicationTimer = [NSTimer scheduledTimerWithTimeInterval:0.2
                                                         repeats:true
                                                           block:^(NSTimer * _Nonnull timer) {
        [self communication];
    }];
}
// =============================================================================
#pragma mark - stopCommunicationTimer
// =============================================================================
- (void)stopCommunicationTimer {
    [communicationTimer invalidate];
    communicationTimer = nil;
}
// =============================================================================
#pragma mark - communication
// =============================================================================
- (void)communication {
    double communication = (dataCount200ms/(0.2*100.0/mQuality))*100.0;
    _label_Communication.stringValue = [NSString stringWithFormat:@"%.2f%%",communication];
    dataCount200ms = 0;
    _box_Communication.frame = CGRectMake(0, 0, communication, _box_Communication.frame.size.height);
}

// =============================================================================
#pragma mark - button_StartScan_Tapped
// =============================================================================
- (IBAction)button_Setting_Tapped:(id)sender {
    NSLog(@"button_Setting_Tapped");
    [self showSetting];
}

// =============================================================================
#pragma mark - showSetting
// =============================================================================
- (void)showSetting {
    NSLog(@"showSetting");
    NSStoryboard *storyboard = [NSStoryboard storyboardWithName:@"Setting" bundle:nil];
    NSWindowController *windowController = [storyboard instantiateInitialController];
    SettingViewController *settingViewController = windowController.contentViewController;
    settingViewController.delegate = self;
    [windowController showWindow:self];
}

// =============================================================================
#pragma mark - button_StartScan_Tapped
// =============================================================================
- (IBAction)button_StartScan_Tapped:(id)sender {
    NSLog(@"Call : startScanningPeripherals");
    peripheralManager = [[CBPeripheralManager alloc] initWithDelegate:self queue:nil options:@{CBPeripheralManagerOptionShowPowerAlertKey:@"YES"}];
//    [self.button_StartScan setEnabled:NO];
//    [self.combobox_MEME removeAllItems];
//    [memelib startScanningPeripherals];
}

- (void)peripheralManagerDidUpdateState:(CBPeripheralManager *)peripheral {
    if (peripheral.state == CBPeripheralManagerStatePoweredOn) {
        NSLog(@"bluetooth ON");
        [self.button_StartScan setEnabled:NO];
        [self.combobox_MEME removeAllItems];
        [memelib startScanningPeripherals];
    }
    else {
        NSLog(@"bluetooth それ以外");
        NSAlert *alert = [[NSAlert alloc] init];
        [alert setMessageText:@"端末のBluetoothをオンにしてください"];
        [alert setInformativeText:@""];
        [alert runModal];
    }
}

// =============================================================================
#pragma mark - button_Connect_Tapped
// =============================================================================
- (IBAction)button_Connect_Tapped:(id)sender {
    if (connected_flag == false) {
        NSLog(@"Call : connectPeripheral");
        [self.button_Connect setEnabled:NO];
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
        startDate = [NSDate date];
        [self startCommunicationTimer];
        
        self.button_StartMeasurement.title = @"Stop Measurement";

        [self.button_StartScan setHidden:YES];
        [self.button_Connect setHidden:YES];
        [self.button_StartMeasurement setHidden:NO];
        [self.button_FreeMarking setHidden:NO];
        
        [self.button_Settings setEnabled:NO];
        [self.combobox_MEME setEnabled:NO];
        [self.combobox_SelectMode setEnabled:NO];
        [self.combobox_TransSpeed setEnabled:NO];
        [self.combobox_AccelRange setEnabled:NO];
        [self.combobox_GyroRange setEnabled:NO];
        [self.combobox_Chart1 setEnabled:NO];
        [self.combobox_Chart2 setEnabled:NO];
        [self.combobox_Chart3 setEnabled:NO];
        [self.button_Chart_Apply setEnabled:NO];
        
//        //
//        if (self.combobox_TransSpeed.indexOfSelectedItem == 0) {
//            chartView1.xMaxValue = 200;
//            chartView1.xLongScale = 25;
//            chartView1.xShortScale = 5;
//            chartView2.xMaxValue = 200;
//            chartView2.xLongScale = 25;
//            chartView2.xShortScale = 5;
//            chartView3.xMaxValue = 200;
//            chartView3.xLongScale = 25;
//            chartView3.xShortScale = 5;
//        }
//        else {
//            chartView1.xMaxValue = 200;
//            chartView1.xLongScale = 25;
//            chartView1.xShortScale = 5;
//            chartView2.xMaxValue = 200;
//            chartView2.xLongScale = 25;
//            chartView2.xShortScale = 5;
//            chartView3.xMaxValue = 200;
//            chartView3.xLongScale = 25;
//            chartView3.xShortScale = 5;
//        }

        [memelib setSelectMode:(uint32_t)self.combobox_SelectMode.indexOfSelectedItem+1];
        [memelib setTransMode:(uint32_t)self.combobox_TransSpeed.indexOfSelectedItem+1];
        mQuality = self.combobox_TransSpeed.indexOfSelectedItem+1;
        [memelib setAccelRange:(uint32_t)accelrange];
        [memelib setGyroRange:(uint32_t)gyrorange];
        
        if (socket != nil) {
            socket.headerString = [self headerString];
            [socket writeHeader];
        }
        
        measurement_flag = true;
        [memelib startDataReport];
    }
    else {
        self.button_StartMeasurement.title = @"Start Measurement";
        
//        [self.button_StartScan setHidden:YES];
//        [self.button_Connect setHidden:NO];
//        [self.button_StartMeasurement setHidden:NO];

//        measurement_flag = false;
        [memelib stopDataReport];
        
        [self stopCommunicationTimer];
        
        // 0.5秒後に初期化、遅延させないと[memelib stopDataReport]後にすこしデータが流れこんでくる
        dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(0.5 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
            
            [self.button_StartScan setHidden:YES];
            [self.button_Connect setHidden:NO];
            [self.button_StartMeasurement setHidden:NO];
            [self.button_FreeMarking setHidden:YES];
            
            [self.button_Settings setEnabled:YES];
            [self.combobox_MEME setEnabled:YES];
            [self.combobox_SelectMode setEnabled:YES];
            [self.combobox_TransSpeed setEnabled:YES];
            [self.combobox_AccelRange setEnabled:YES];
            [self.combobox_GyroRange setEnabled:YES];
            [self.combobox_Chart1 setEnabled:YES];
            [self.combobox_Chart2 setEnabled:YES];
            [self.combobox_Chart3 setEnabled:YES];
            [self.button_Chart_Apply setEnabled:YES];
            
            measurement_flag = false;
            
            [self reset];
        });
        
        // 保存先の変更
        if ([UserSetting getShowSaveFileDialog]) {
            [self fileMove];
        }
        else {
            [csvManager reset];
        }
    }
}

// =============================================================================
#pragma mark - button_StartMeasurement_Tapped
// =============================================================================

- (IBAction)button_FreeMarking_Tapped:(id)sender {
    NSLog(@"button_FreeMarking_Tapped");
    isFreeMarking = YES;
}

// =============================================================================
#pragma mark - fileMove
// =============================================================================
- (void)fileMove {
    NSSavePanel *savePanel = [[NSSavePanel alloc] init];
    savePanel.canCreateDirectories = true;
    savePanel.showsTagField = false;
    savePanel.extensionHidden = false;
    savePanel.allowedFileTypes = @[@"csv"];
    savePanel.nameFieldStringValue = csvManager.saveFileName;
    savePanel.level = NSModalPanelWindowLevel;
    [savePanel beginSheetModalForWindow:NSApp.mainWindow completionHandler:^(NSModalResponse result) {
        if (result == NSModalResponseOK) {
            NSLog(@"OK");
            NSURL *url = [savePanel URL];
            NSError *error = nil;
            [[NSFileManager defaultManager] copyItemAtURL:[NSURL URLWithString:csvManager.saveFilePath]
                                                    toURL:url
                                                    error:&error];
            if (error) {
                NSLog(@"コピー失敗:%@",error);
            }
            else {
                [[NSFileManager defaultManager] removeItemAtURL:[NSURL URLWithString:csvManager.saveFilePath]
                                                          error:&error];
                if (error) {
                    NSLog(@"削除失敗:%@",error);
                }
                else {
                    NSLog(@"成功");
                }
            }
        }
        else {
            NSLog(@"NO");
        }
        [csvManager reset];
    }];
}

// =============================================================================
#pragma mark - socketStart
// =============================================================================

- (void)socketStart {
    NSLog(@"socketStart");
    if ([UserSetting getExtermalOutputSocket]) {
        socket = [[TCPSocket alloc] init];
        socket.delegate = self;
        socket.headerString = [self headerString];
        NSString *status = [socket start];
        self.label_SocketStatus.stringValue = [NSString stringWithFormat:@"Status : %@",status];
    }
    else {
        NSLog(@"ソケット通信しない");
    }
}


// =============================================================================
#pragma mark - didAccept
// =============================================================================

- (void)didAccept {
    NSLog(@"didAccept");
    self.label_SocketStatus.stringValue = @"Status : Accept";
}

// =============================================================================
#pragma mark - didDisconnect:withError:
// =============================================================================

- (void)socketDidDisconnect:(GCDAsyncSocket *)sock withError:(NSError *)err  {
    NSLog(@"didDisconnect");
    if (socket.socket == sock) {
        self.label_SocketStatus.stringValue = @"Status : ";
        [self socketStart];
    }
}

// =============================================================================
#pragma mark - socketStop
// =============================================================================

- (void)socketStop {
    NSLog(@"stop");
    [socket stop];
    socket = nil;
    self.label_SocketStatus.stringValue = @"Status : ";
}

// =============================================================================
#pragma mark - comboBoxSelectionDidChange
// =============================================================================

- (ChartView *)setChartView:(NSScrollView *)scrollView
                  xMaxValue:(double)xMaxValue
                  xMinValue:(double)xMinValue
                  yMaxValue:(double)yMaxValue
                  yMinValue:(double)yMinValue {
    ChartView *chartView = [[ChartView alloc] initWithFrame:CGRectMake(0,
                                                                       0,
                                                                       scrollView.frame.size.width,
                                                                       scrollView.frame.size.height)];
    chartView.xMaxValue = xMaxValue;
    chartView.xMinValue = xMinValue;
    chartView.xLongScale = 25;
    chartView.xShortScale = 5;
    chartView.yMaxValue = yMaxValue;
    chartView.yMinValue = yMinValue;
    [scrollView addSubview:chartView];
    return chartView;
}

// =============================================================================
#pragma mark - drowStandardElectooculographyChart
// =============================================================================

- (void)drowStandardElectooculographyChart:(ChartView *)chartView
                                     datas:(NSMutableArray *)datas
                                    isLeft:(BOOL)isLeft
                                    isRight:(BOOL)isRight
                                    isDelftH:(BOOL)isDelftH
                                    isDelftV:(BOOL)isDelftV {
    NSMutableArray *left = [[NSMutableArray alloc] init];
    NSMutableArray *right = [[NSMutableArray alloc] init];
    NSMutableArray *deltaH = [[NSMutableArray alloc] init];
    NSMutableArray *deltaV = [[NSMutableArray alloc] init];
    if ([datas count] >= chartView.xMaxValue-chartView.xLongScale/2) {
        for (int i = [datas count] - (chartView.xMaxValue-chartView.xLongScale/2); i<[datas count]; i++){
            AcademicStandardData *data = datas[i];
            if (isLeft) [left addObject:[NSNumber numberWithDouble:data.EogL1]];
            if (isRight) [right addObject:[NSNumber numberWithDouble:data.EogR1]];
            if (isDelftH) [deltaH addObject:[NSNumber numberWithDouble:data.EogH1]];
            if (isDelftV) [deltaV addObject:[NSNumber numberWithDouble:data.EogV1]];
        }
    }
    else {
        for (AcademicStandardData *data in chartDatas) {
            if (isLeft) [left addObject:[NSNumber numberWithDouble:data.EogL1]];
            if (isRight) [right addObject:[NSNumber numberWithDouble:data.EogR1]];
            if (isDelftH) [deltaH addObject:[NSNumber numberWithDouble:data.EogH1]];
            if (isDelftV) [deltaV addObject:[NSNumber numberWithDouble:data.EogV1]];
        }
    }

    [chartView.datas removeAllObjects];
    if (isLeft) [chartView setChartData:left lineColor:[NSColor yellowColor]];
    if (isRight) [chartView setChartData:right lineColor:[NSColor greenColor]];
    if (isDelftH) [chartView setChartData:deltaH lineColor:[NSColor redColor]];
    if (isDelftV) [chartView setChartData:deltaV lineColor:[NSColor blueColor]];
    if ([datas count] > chartView.xMaxValue - chartView.xLongScale) {
        chartView.xInitialPosition = [datas count] % chartView.xLongScale;
        chartView.xInitialPosition = chartView.xInitialPosition * -1;
        chartView.xTextFieldValue = ([datas count] / chartView.xLongScale) - (chartView.xMaxValue / chartView.xLongScale) + 1;
    }
    [chartView setNeedsDisplay:YES];
}

// =============================================================================
#pragma mark - drowFullElectooculographyChart
// =============================================================================

- (void)drowFullElectooculographyChart:(ChartView *)chartView
                                 datas:(NSMutableArray *)datas
                                isLeft:(BOOL)isLeft
                                isRight:(BOOL)isRight
                                isDelftH:(BOOL)isDelftH
                                isDelftV:(BOOL)isDelftV {
    NSMutableArray *left = [[NSMutableArray alloc] init];
    NSMutableArray *right = [[NSMutableArray alloc] init];
    NSMutableArray *deltaH = [[NSMutableArray alloc] init];
    NSMutableArray *deltaV = [[NSMutableArray alloc] init];
    if ([datas count] >= chartView.xMaxValue-chartView.xLongScale/2) {
        for (int i = [datas count] - (chartView.xMaxValue-chartView.xLongScale/2); i<[datas count]; i++){
            AcademicFullData *data = datas[i];
            if (isLeft) [left addObject:[NSNumber numberWithDouble:data.EogL]];
            if (isRight) [right addObject:[NSNumber numberWithDouble:data.EogR]];
            if (isDelftH) [deltaH addObject:[NSNumber numberWithDouble:data.EogH]];
            if (isDelftV) [deltaV addObject:[NSNumber numberWithDouble:data.EogV]];
        }
    }
    else {
        for (AcademicFullData *data in chartDatas) {
            if (isLeft) [left addObject:[NSNumber numberWithDouble:data.EogL]];
            if (isRight) [right addObject:[NSNumber numberWithDouble:data.EogR]];
            if (isDelftH) [deltaH addObject:[NSNumber numberWithDouble:data.EogH]];
            if (isDelftV) [deltaV addObject:[NSNumber numberWithDouble:data.EogV]];
        }
    }

    [chartView.datas removeAllObjects];
    if (isLeft) [chartView setChartData:left lineColor:[NSColor yellowColor]];
    if (isRight) [chartView setChartData:right lineColor:[NSColor greenColor]];
    if (isDelftH) [chartView setChartData:deltaH lineColor:[NSColor redColor]];
    if (isDelftV) [chartView setChartData:deltaV lineColor:[NSColor blueColor]];
    if ([datas count] > chartView.xMaxValue - chartView.xLongScale) {
        chartView.xInitialPosition = [datas count] % chartView.xLongScale;
        chartView.xInitialPosition = chartView.xInitialPosition * -1;
        chartView.xTextFieldValue = ([datas count] / chartView.xLongScale) - (chartView.xMaxValue / chartView.xLongScale) + 1;
    }
    [chartView setNeedsDisplay:YES];
}

// =============================================================================
#pragma mark - drowFullGyroscopeChart
// =============================================================================

- (void)drowFullGyroscopeChart:(ChartView *)chartView
                         datas:(NSMutableArray *)datas
                       isGyroX:(BOOL)isGyroX
                       isGyroY:(BOOL)isGyroY
                       isGyroZ:(BOOL)isGyroZ {
    NSMutableArray *gyroX = [[NSMutableArray alloc] init];
    NSMutableArray *gyroY = [[NSMutableArray alloc] init];
    NSMutableArray *gyroZ = [[NSMutableArray alloc] init];
    if ([datas count] >= chartView.xMaxValue-chartView.xLongScale/2) {
        for (int i = [datas count] - (chartView.xMaxValue-chartView.xLongScale/2); i<[datas count]; i++){
            AcademicFullData *data = datas[i];
            if (isGyroX) [gyroX addObject:[NSNumber numberWithDouble:data.GyroX]];
            if (isGyroY) [gyroY addObject:[NSNumber numberWithDouble:data.GyroY]];
            if (isGyroZ) [gyroZ addObject:[NSNumber numberWithDouble:data.GyroZ]];
        }
    }
    else {
        for (AcademicFullData *data in chartDatas) {
            if (isGyroX) [gyroX addObject:[NSNumber numberWithDouble:data.GyroX]];
            if (isGyroY) [gyroY addObject:[NSNumber numberWithDouble:data.GyroY]];
            if (isGyroZ) [gyroZ addObject:[NSNumber numberWithDouble:data.GyroZ]];
        }
    }
   
    [chartView.datas removeAllObjects];
    if (isGyroX) [chartView setChartData:gyroX lineColor:[NSColor redColor]];
    if (isGyroY) [chartView setChartData:gyroY lineColor:[NSColor greenColor]];
    if (isGyroZ) [chartView setChartData:gyroZ lineColor:[NSColor blueColor]];
    if ([datas count] > chartView.xMaxValue - chartView.xLongScale) {
        chartView.xInitialPosition = [datas count] % chartView.xLongScale;
        chartView.xInitialPosition = chartView.xInitialPosition * -1;
        chartView.xTextFieldValue = ([datas count] / chartView.xLongScale) - (chartView.xMaxValue / chartView.xLongScale) + 1;
    }
    [chartView setNeedsDisplay:YES];
}

// =============================================================================
#pragma mark - drowStandardAccelerometerChart
// =============================================================================

- (void)drowStandardAccelerometerChart:(ChartView *)chartView
                                 datas:(NSMutableArray *)datas
                                isAccX:(BOOL)isAccX
                                isAccY:(BOOL)isAccY
                                isAccZ:(BOOL)isAccZ  {
    NSMutableArray *accX = [[NSMutableArray alloc] init];
    NSMutableArray *accY = [[NSMutableArray alloc] init];
    NSMutableArray *accZ = [[NSMutableArray alloc] init];
    if ([datas count] >= chartView.xMaxValue-chartView.xLongScale/2) {
        for (int i = [datas count] - (chartView.xMaxValue-chartView.xLongScale/2); i<[datas count]; i++){
            AcademicStandardData *data = datas[i];
            if (isAccX) [accX addObject:[NSNumber numberWithDouble:data.AccX + [UserSetting getXAxis]]];
            if (isAccY) [accY addObject:[NSNumber numberWithDouble:data.AccY + [UserSetting getYAxis]]];
            if (isAccZ) [accZ addObject:[NSNumber numberWithDouble:data.AccZ + [UserSetting getZAxis]]];
        }
    }
    else {
        for (AcademicStandardData *data in chartDatas) {
            if (isAccX) [accX addObject:[NSNumber numberWithDouble:data.AccX + [UserSetting getXAxis]]];
            if (isAccY) [accY addObject:[NSNumber numberWithDouble:data.AccY + [UserSetting getYAxis]]];
            if (isAccZ) [accZ addObject:[NSNumber numberWithDouble:data.AccZ + [UserSetting getZAxis]]];
        }
    }
    
//    for (AcademicStandardData *data in chartDatas) {
//        [accX addObject:[NSNumber numberWithDouble:data.AccX]];
//        [accY addObject:[NSNumber numberWithDouble:data.AccY]];
//        [accZ addObject:[NSNumber numberWithDouble:data.AccZ]];
//    }
//    if ([chartDatas count] >= 800) {
//        graph1.xMaxValue = [chartDatas count];
//        graph1.xMinValue = [chartDatas count] - 800;
//    }
   
    [chartView.datas removeAllObjects];
    if (isAccX) [chartView setChartData:accX lineColor:[NSColor redColor]];
    if (isAccY) [chartView setChartData:accY lineColor:[NSColor greenColor]];
    if (isAccZ) [chartView setChartData:accZ lineColor:[NSColor blueColor]];
    if ([datas count] > chartView.xMaxValue - chartView.xLongScale) {
        chartView.xInitialPosition = [datas count] % chartView.xLongScale;
        chartView.xInitialPosition = chartView.xInitialPosition * -1;
        chartView.xTextFieldValue = ([datas count] / chartView.xLongScale) - (chartView.xMaxValue / chartView.xLongScale) + 1;
    }
    [chartView setNeedsDisplay:YES];
}

// =============================================================================
#pragma mark - drowFullAccelerometerChart
// =============================================================================

- (void)drowFullAccelerometerChart:(ChartView *)chartView
                             datas:(NSMutableArray *)datas
                            isAccX:(BOOL)isAccX
                            isAccY:(BOOL)isAccY
                            isAccZ:(BOOL)isAccZ {
    NSMutableArray *accX = [[NSMutableArray alloc] init];
    NSMutableArray *accY = [[NSMutableArray alloc] init];
    NSMutableArray *accZ = [[NSMutableArray alloc] init];
    if ([datas count] >= chartView.xMaxValue-chartView.xLongScale/2) {
        for (int i = [datas count] - (chartView.xMaxValue-chartView.xLongScale/2); i<[datas count]; i++){
            AcademicFullData *data = datas[i];
            if (isAccX) [accX addObject:[NSNumber numberWithDouble:data.AccX + [UserSetting getXAxis]]];
            if (isAccY) [accY addObject:[NSNumber numberWithDouble:data.AccY + [UserSetting getYAxis]]];
            if (isAccZ) [accZ addObject:[NSNumber numberWithDouble:data.AccZ + [UserSetting getZAxis]]];
        }
    }
    else {
        for (AcademicFullData *data in chartDatas) {
            if (isAccX) [accX addObject:[NSNumber numberWithDouble:data.AccX + [UserSetting getXAxis]]];
            if (isAccY) [accY addObject:[NSNumber numberWithDouble:data.AccY + [UserSetting getYAxis]]];
            if (isAccZ) [accZ addObject:[NSNumber numberWithDouble:data.AccZ + [UserSetting getZAxis]]];
        }
    }
   
    [chartView.datas removeAllObjects];
    if (isAccX) [chartView setChartData:accX lineColor:[NSColor redColor]];
    if (isAccY) [chartView setChartData:accY lineColor:[NSColor greenColor]];
    if (isAccZ) [chartView setChartData:accZ lineColor:[NSColor blueColor]];
    if ([datas count] > chartView.xMaxValue - chartView.xLongScale) {
        chartView.xInitialPosition = [datas count] % chartView.xLongScale;
        chartView.xInitialPosition = chartView.xInitialPosition * -1;
        chartView.xTextFieldValue = ([datas count] / chartView.xLongScale) - (chartView.xMaxValue / chartView.xLongScale) + 1;
    }
    [chartView setNeedsDisplay:YES];
}

//- (IBAction)chart1_Changed:(NSComboBox *)sender {
//    if (sender.indexOfSelectedItem == 0) {
//        self.textField_Chart1.stringValue = @"Chart1：Electrooculography";
//        [self setChartY:1200
//         textFieldArray:chart1Y];
//        [self.box_Chart1_Electrooculography setHidden:NO];
//        [self.box_Chart1_Gyroscope setHidden:YES];
//        [self.box_Chart1_Accelerometer setHidden:YES];
//    }
//    else if (sender.indexOfSelectedItem == 1) {
//        self.textField_Chart1.stringValue = @"Chart1：Gyroscope";
//        [self setChartY:36000
//         textFieldArray:chart1Y];
//        [self.box_Chart1_Electrooculography setHidden:YES];
//        [self.box_Chart1_Gyroscope setHidden:NO];
//        [self.box_Chart1_Accelerometer setHidden:YES];
//    }
//    else if (sender.indexOfSelectedItem == 2) {
//        self.textField_Chart1.stringValue = @"Chart1：Accelerometer";
//        [self setChartY:36000
//         textFieldArray:chart1Y];
//        [self.box_Chart1_Electrooculography setHidden:YES];
//        [self.box_Chart1_Gyroscope setHidden:YES];
//        [self.box_Chart1_Accelerometer setHidden:NO];
//    }
//}
//
//- (IBAction)chart2_Changed:(NSComboBox *)sender {
//    if (sender.indexOfSelectedItem == 0) {
//        self.textField_Chart2.stringValue = @"Chart2：Electrooculography";
//        [self setChartY:1200
//         textFieldArray:chart2Y];
//        [self.box_Chart2_Electrooculography setHidden:NO];
//        [self.box_Chart2_Gyroscope setHidden:YES];
//        [self.box_Chart2_Accelerometer setHidden:YES];
//    }
//    else if (sender.indexOfSelectedItem == 1) {
//        self.textField_Chart2.stringValue = @"Chart2：Gyroscope";
//        [self setChartY:36000
//         textFieldArray:chart2Y];
//        [self.box_Chart2_Electrooculography setHidden:YES];
//        [self.box_Chart2_Gyroscope setHidden:NO];
//        [self.box_Chart2_Accelerometer setHidden:YES];
//    }
//    else if (sender.indexOfSelectedItem == 2) {
//        self.textField_Chart2.stringValue = @"Chart2：Accelerometer";
//        [self setChartY:36000
//         textFieldArray:chart2Y];
//        [self.box_Chart2_Electrooculography setHidden:YES];
//        [self.box_Chart2_Gyroscope setHidden:YES];
//        [self.box_Chart2_Accelerometer setHidden:NO];
//    }
//}
//
//- (IBAction)chart3_Changed:(NSComboBox *)sender {
//    if (sender.indexOfSelectedItem == 0) {
//        self.textField_Chart3.stringValue = @"Chart3：Electrooculography";
//        [self setChartY:1200
//         textFieldArray:chart3Y];
//        [self.box_Chart3_Electrooculography setHidden:NO];
//        [self.box_Chart3_Gyroscope setHidden:YES];
//        [self.box_Chart3_Accelerometer setHidden:YES];
//    }
//    else if (sender.indexOfSelectedItem == 1) {
//        self.textField_Chart3.stringValue = @"Chart3：Gyroscope";
//        [self setChartY:36000
//         textFieldArray:chart3Y];
//        [self.box_Chart3_Electrooculography setHidden:YES];
//        [self.box_Chart3_Gyroscope setHidden:NO];
//        [self.box_Chart3_Accelerometer setHidden:YES];
//    }
//    else if (sender.indexOfSelectedItem == 2) {
//        self.textField_Chart3.stringValue = @"Chart3：Accelerometer";
//        [self setChartY:36000
//         textFieldArray:chart3Y];
//        [self.box_Chart3_Electrooculography setHidden:YES];
//        [self.box_Chart3_Gyroscope setHidden:YES];
//        [self.box_Chart3_Accelerometer setHidden:NO];
//    }
//}

// =============================================================================
#pragma mark - chart_Apply
// =============================================================================

- (IBAction)chart_Apply:(NSButton *)sender {
    // Chart1
    if (self.combobox_Chart1.indexOfSelectedItem == 0) {
        self.textField_Chart1.stringValue = @"Chart1：Electrooculography";
        [self setChartY:1200
         textFieldArray:chart1Y];
        [self.box_Chart1_Electrooculography setHidden:NO];
        [self.box_Chart1_Gyroscope setHidden:YES];
        [self.box_Chart1_Accelerometer setHidden:YES];
    }
    else if (self.combobox_Chart1.indexOfSelectedItem == 1) {
        self.textField_Chart1.stringValue = @"Chart1：Gyroscope";
        [self setChartY:36000
         textFieldArray:chart1Y];
        [self.box_Chart1_Electrooculography setHidden:YES];
        [self.box_Chart1_Gyroscope setHidden:NO];
        [self.box_Chart1_Accelerometer setHidden:YES];
    }
    else if (self.combobox_Chart1.indexOfSelectedItem == 2) {
        self.textField_Chart1.stringValue = @"Chart1：Accelerometer";
        [self setChartY:36000
         textFieldArray:chart1Y];
        [self.box_Chart1_Electrooculography setHidden:YES];
        [self.box_Chart1_Gyroscope setHidden:YES];
        [self.box_Chart1_Accelerometer setHidden:NO];
    }
    // Chart2
    if (self.combobox_Chart2.indexOfSelectedItem == 0) {
        self.textField_Chart2.stringValue = @"Chart2：Electrooculography";
        [self setChartY:1200
         textFieldArray:chart2Y];
        [self.box_Chart2_Electrooculography setHidden:NO];
        [self.box_Chart2_Gyroscope setHidden:YES];
        [self.box_Chart2_Accelerometer setHidden:YES];
    }
    else if (self.combobox_Chart2.indexOfSelectedItem == 1) {
        self.textField_Chart2.stringValue = @"Chart2：Gyroscope";
        [self setChartY:36000
         textFieldArray:chart2Y];
        [self.box_Chart2_Electrooculography setHidden:YES];
        [self.box_Chart2_Gyroscope setHidden:NO];
        [self.box_Chart2_Accelerometer setHidden:YES];
    }
    else if (self.combobox_Chart2.indexOfSelectedItem == 2) {
        self.textField_Chart2.stringValue = @"Chart2：Accelerometer";
        [self setChartY:36000
         textFieldArray:chart2Y];
        [self.box_Chart2_Electrooculography setHidden:YES];
        [self.box_Chart2_Gyroscope setHidden:YES];
        [self.box_Chart2_Accelerometer setHidden:NO];
    }
    // Chart3
    if (self.combobox_Chart3.indexOfSelectedItem == 0) {
        self.textField_Chart3.stringValue = @"Chart3：Electrooculography";
        [self setChartY:1200
         textFieldArray:chart3Y];
        [self.box_Chart3_Electrooculography setHidden:NO];
        [self.box_Chart3_Gyroscope setHidden:YES];
        [self.box_Chart3_Accelerometer setHidden:YES];
    }
    else if (self.combobox_Chart3.indexOfSelectedItem == 1) {
        self.textField_Chart3.stringValue = @"Chart3：Gyroscope";
        [self setChartY:36000
         textFieldArray:chart3Y];
        [self.box_Chart3_Electrooculography setHidden:YES];
        [self.box_Chart3_Gyroscope setHidden:NO];
        [self.box_Chart3_Accelerometer setHidden:YES];
    }
    else if (self.combobox_Chart3.indexOfSelectedItem == 2) {
        self.textField_Chart3.stringValue = @"Chart3：Accelerometer";
        [self setChartY:36000
         textFieldArray:chart3Y];
        [self.box_Chart3_Electrooculography setHidden:YES];
        [self.box_Chart3_Gyroscope setHidden:YES];
        [self.box_Chart3_Accelerometer setHidden:NO];
    }
}


- (void)setChartY:(int)value
   textFieldArray:(NSMutableArray *)textFieldArray {
    int scale = value*2 / 6;
    for (int i = 0; i < 7; i++) {
        NSTextField *textField = textFieldArray[i];
        textField.stringValue = [NSString stringWithFormat:@"%d",value - scale*i];
    }
}

// =============================================================================
#pragma mark - didApply
// =============================================================================

- (void)didApply:(SettingViewController *)settingViewController {
    NSLog(@"didApply");
    [self socketStop];
    [self socketStart];
    [self localPort];
}

@end
