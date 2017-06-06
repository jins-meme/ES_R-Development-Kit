//
//  MEMELib_Academic.m
//  MEME_Academic
//
//  Created by D-CLUE on 2017/03/22.
//  Copyright © 2017年 jins-jp. All rights reserved.
//

#import "MEMELib_Academic.h"

@import CoreBluetooth;            // BluetoothLE

#ifdef DEBUG
#define libLog(fmt,...) NSLog((@"%s %d "fmt), __PRETTY_FUNCTION__, __LINE__, ##__VA_ARGS__);
#else
#define libLog(...)
#endif

@implementation MEMELib_Academic
{
    uint32_t dataCount;
    NSTimer *checkTimer;
    NSTimer *calibTimer;
    boolean_t Library_Flag;
    boolean_t Scan_Flag;
    boolean_t Connected_Flag;
    boolean_t Measure_Flag;
    boolean_t Service_Flag;
    boolean_t Analysis_Flag;

    uint32_t Lib_Status;
    uint32_t selectMode;
    uint32_t transMode;
    uint32_t accelRange;
    uint32_t gyroRange;
}

// =============================================================================
#pragma mark - init
// =============================================================================
- (id)init
{
    if (self = [super init]) {
        self.peripherals = @[].mutableCopy;
        self.peripheralsLocalName = @[].mutableCopy;
        self.recvData = @[].mutableCopy;

        self.MEMEVersion = [[Version alloc] init];
        self.SDKVersion = [[Version alloc] init];
        
        Library_Flag = false;
        Scan_Flag = false;
        Connected_Flag = false;
        Measure_Flag = false;
        Service_Flag = false;
        Lib_Status = STATUS_IDLE;

        selectMode = MEMEMode_Full;
        transMode = MEMEQuality_High;
        accelRange = MEMEAccelRange_2G;
        gyroRange = MEMEGyroRange_250dps;
        
        self.centralManager = [[CBCentralManager alloc] initWithDelegate:self
                                                                   queue:nil];
    }
    return self;
}

// =============================================================================
#pragma mark - startScanningPeripherals
// =============================================================================
- (uint32_t)startScanningPeripherals {
    if ((Library_Flag == true) && (Scan_Flag == false) && (Connected_Flag == false)) {
        [self Device_Scan_Start];
        
        return (MEMELIB_OK);
    }
    else
    {
        return (MEMELIB_NG);
    }
    
}

// =============================================================================
#pragma mark - stopScanningPeripherals
// =============================================================================
- (uint32_t)stopScanningPeripherals {
    if ((Library_Flag == true) && (Scan_Flag == true) && (Connected_Flag == false)) {
        [self Device_Scan_Stop];
        
        return (MEMELIB_OK);
    }
    else {
        return (MEMELIB_NG);
    }
}

// =============================================================================
#pragma mark - connectPeripheral
// =============================================================================
- (uint32_t)connectPeripheral:(NSString *)DeviceName {
    if ((Library_Flag == true) && (Connected_Flag == false)) {
        [self Device_Connection_Start:DeviceName];
        return MEMELIB_OK;
    }
    else {
        return MEMELIB_NG;
    }
}

// =============================================================================
#pragma mark - Disconnection_Req
// =============================================================================
- (uint32_t)disconnectPeripheral {
    if ((Library_Flag == true) && (Connected_Flag == true))
    {
        [self Device_Disconnection_Start];
        return MEMELIB_OK;
    }
    else
    {
        return MEMELIB_NG;
    }
}

// =============================================================================
#pragma mark - getSelectMode
// =============================================================================
- (uint32_t)getSelectMode {
    return selectMode;
}

// =============================================================================
#pragma mark - setSelectMode
// =============================================================================
- (uint32_t)setSelectMode:(uint32_t)mode {
    selectMode = mode;
    [self MEME_ADN_SET_MODE];
    return MEMELIB_OK;
}

// =============================================================================
#pragma mark - getTransMode
// =============================================================================
- (uint32_t)getTransMode {
    return transMode;
}

// =============================================================================
#pragma mark - setTransMode
// =============================================================================
- (uint32_t)setTransMode:(uint32_t)mode {
    transMode = mode;
    [self MEME_ADN_SET_MODE];
    return MEMELIB_OK;
}

// =============================================================================
#pragma mark - getAccelRange
// =============================================================================
- (uint32_t)getAccelRange {
    return accelRange;
}

// =============================================================================
#pragma mark - setAccelRange
// =============================================================================
- (uint32_t)setAccelRange:(uint32_t)range {
    accelRange = range;
    [self MEME_ADN_SET_6AXIS_PARAMS];
    return MEMELIB_OK;
}

// =============================================================================
#pragma mark - getGyroRange
// =============================================================================
- (uint32_t)getGyroRange {
    return gyroRange;
}

// =============================================================================
#pragma mark - setGyroRange
// =============================================================================
- (uint32_t)setGyroRange:(uint32_t)range {
    gyroRange = range;
    [self MEME_ADN_SET_6AXIS_PARAMS];
    return MEMELIB_OK;
}

// =============================================================================
#pragma mark - startDataReport
// =============================================================================
- (uint32_t)startDataReport {
    if ((Library_Flag == true) && (Connected_Flag == true) && (Measure_Flag == false)) {
        Measure_Flag = true;
        [self MEME_ADN_GET_DATA:0x01];
        return MEMELIB_OK;
    }
    else {
        return MEMELIB_NG;
    }
}

// =============================================================================
#pragma mark - stopDataReport
// =============================================================================
- (uint32_t)stopDataReport {
    if ((Library_Flag == true) && (Connected_Flag == true) && (Measure_Flag == true)) {
        Measure_Flag = false;
        [self MEME_ADN_GET_DATA:0x00];
        return MEMELIB_OK;
    }
    else {
        return MEMELIB_NG;
    }
}

// =============================================================================
#pragma mark - Data_Send
// =============================================================================
-(void)Data_Send:(uint8_t *)buff {
    [DecEnc Encode:buff];
    NSData *sendData = [[NSData alloc] initWithBytes:buff length:PACKET_LENGTH];
    [self.peripheral writeValue:sendData forCharacteristic:self.outputCharacteristic type:CBCharacteristicWriteWithResponse];
}


// =============================================================================
#pragma mark - Device_Scan_Start
// =============================================================================
- (void)Device_Scan_Start {
    self.peripheral = nil;
    [self.peripherals removeAllObjects];
    [self.peripheralsLocalName removeAllObjects];
    NSArray *services = [NSArray arrayWithObjects:[CBUUID UUIDWithString:SERVICES_UUID], nil];
    NSDictionary *options = [NSDictionary dictionaryWithObject:[NSNumber numberWithBool:NO]
                                                        forKey:CBCentralManagerScanOptionAllowDuplicatesKey];
    [self.centralManager scanForPeripheralsWithServices:services options:options];
    
    Lib_Status = STATUS_SCAN_REQ_EXEC;
    Scan_Flag = true;
    [self checkTimerStart];
}

// =============================================================================
#pragma mark - Device_Connection_Start
// =============================================================================
- (void)Device_Connection_Start:(NSString *)DeviceName {
    boolean_t flg = false;
    
    if (Scan_Flag == true) {
        [self Device_Scan_Stop];
    }
    for (CBPeripheral *p in self.peripherals){
        if ([p.name isEqual: DeviceName]){
            flg = true;
            self.peripheral = p;
            break;
        }
    }
    if (flg == true){
        [self.centralManager connectPeripheral:self.peripheral
                                       options:nil];
        Lib_Status = STATUS_CONNECTION_REQ_EXEC;
        [self checkTimerStart];
    }
}

// =============================================================================
#pragma mark - Device_Disconnection_Start
// =============================================================================
- (void)Device_Disconnection_Start {
    [self.peripheral setNotifyValue:NO forCharacteristic:self.inputCharacteristic];
    [self.centralManager cancelPeripheralConnection:self.peripheral];
    
    Lib_Status = STATUS_DISCONNECTION_REQ_EXEC;
    [self checkTimerStart];
}

// =============================================================================
#pragma mark - Device_Scan_Stop
// =============================================================================
- (void)Device_Scan_Stop {
    [self.centralManager stopScan];
    [self checkTimerStop];
    Scan_Flag = false;
}

// =============================================================================
#pragma mark - MEME_ADN_GET_DEV_INFO
// =============================================================================
- (void)MEME_ADN_GET_DEV_INFO {
    UInt8 buff[PACKET_LENGTH];
    buff[0] = PACKET_LENGTH;
    buff[1] = 0xA1;
    [self Data_Send:buff];
}

// =============================================================================
#pragma mark - MEME_ADN_GET_MODE
// =============================================================================
- (void)MEME_ADN_GET_MODE {
    UInt8 buff[PACKET_LENGTH];
    buff[0] = PACKET_LENGTH;
    buff[1] = 0xA3;
    [self Data_Send:buff];
}

// =============================================================================
#pragma mark - MEME_ADN_SET_MODE
// =============================================================================
- (void)MEME_ADN_SET_MODE {
    UInt8 buff[PACKET_LENGTH];
    buff[0] = PACKET_LENGTH;
    buff[1] = 0xA4;
    buff[4] = selectMode;
    buff[5] = transMode;
    [self Data_Send:buff];
}

// =============================================================================
#pragma mark - MEME_ADN_GET_6AXIS_PARAMS
// =============================================================================
- (void)MEME_ADN_GET_6AXIS_PARAMS {
    UInt8 buff[PACKET_LENGTH];
    buff[0] = PACKET_LENGTH;
    buff[1] = 0xA9;
    [self Data_Send:buff];
}

// =============================================================================
#pragma mark - MEME_ADN_SET_6AXIS_PARAMS
// =============================================================================
- (void)MEME_ADN_SET_6AXIS_PARAMS {
    UInt8 buff[PACKET_LENGTH];
    buff[0] = PACKET_LENGTH;
    buff[1] = 0xAA;
    buff[2] = accelRange;
    buff[3] = gyroRange;
    [self Data_Send:buff];
}

// =============================================================================
#pragma mark - MEME_ADN_GET_DATA
// =============================================================================
- (void)MEME_ADN_GET_DATA:(int)state {
    UInt8 buff[PACKET_LENGTH];
    buff[0] = PACKET_LENGTH;
    buff[1] = 0xA0;
    buff[2] = state;
    [self Data_Send:buff];
}


// =============================================================================
#pragma mark - checkTimerStart
// =============================================================================
- (void)checkTimerStart {
    [self checkTimerStop];
    checkTimer = [NSTimer scheduledTimerWithTimeInterval:CHECK_TIMEOUT_TIME
                                                  target:self
                                                selector:@selector(checkTimeout:)
                                                userInfo:nil
                                                 repeats:YES];
}

// =============================================================================
#pragma mark - checkTimerStop
// =============================================================================
- (void)checkTimerStop {
    if ([checkTimer isValid]) {
        [checkTimer invalidate];
    }
}

// =============================================================================
#pragma mark - checkTimeout
// =============================================================================
- (void)checkTimeout:(NSTimer*)timer {
    [self checkTimerStop];
    switch(Lib_Status)
    {
        case STATUS_SCAN_REQ_EXEC:
            [_delegate memePeripheralFoundDelegate:(uint32_t)MEMELIB_TIMEOUT DeviceName:(NSString*)nil uuid:(NSString*)nil];
            break;
            
        case STATUS_GET_STATUS_REQ_EXEC:
            [self Device_Scan_Stop];
            break;
            
        case STATUS_GET_VERSION_REQ_EXEC:
            [self Device_Scan_Stop];
            break;
            
        case STATUS_SET_TIME_REQ_EXEC:
            [self Device_Scan_Stop];
            break;
            
        case STATUS_CONNECTION_REQ_EXEC:
            [self Device_Scan_Stop];
            break;
            
        default:
            break;
    }
}

// =============================================================================
#pragma mark - Data analysis
// =============================================================================
- (void)DataAnalysis {

    NSInteger cnt = ([self.recvData count] - 1);
    NSData *data = [self.recvData objectAtIndex:cnt];
    NSUInteger len = [data length];
    UInt8 buff[PACKET_LENGTH];
    memcpy(buff, [data bytes], len);
    [DecEnc Decode:buff];
    
    AcademicFullData* fullData = [[AcademicFullData alloc] init];
    
    if (buff[0] == PACKET_LENGTH) {
        switch (buff[1]) {
            case 0x81:
                self.MEMEVersion.Major = buff[6];
                self.MEMEVersion.Minor = buff[5];
                self.MEMEVersion.Revision = buff[4];
                [self MEME_ADN_GET_MODE];
                libLog(@"0x81");
                break;
            case 0x83:
                selectMode = buff[4];
                transMode = buff[5];
                [self MEME_ADN_GET_6AXIS_PARAMS];
                libLog(@"0x83");
                break;
            case 0x89:
                accelRange = buff[2];
                gyroRange = buff[3];
                [_delegate memePeripheralConnectedDelegate:(uint32_t)MEMELIB_OK];
                libLog(@"0x89");
                break;
            case 0x8F:
                break;
            case 0x98:
                break;
            case 0x99:
                fullData.Cnt = (uint16_t)(((buff[3] << 8) & 0x0F00) | buff[2]);
                fullData.BattLv = (uint16_t)(buff[3] >> 4);
                fullData.AccX = (int16_t)((buff[5] << 8) | buff[4]);
                fullData.AccY = (int16_t)((buff[7] << 8) | buff[6]);
                fullData.AccZ = (int16_t)((buff[9] << 8) | buff[8]);
                fullData.GyroX = (int16_t)((buff[11] << 8) | buff[10]);
                fullData.GyroY = (int16_t)((buff[13] << 8) | buff[12]);
                fullData.GyroZ = (int16_t)((buff[15] << 8) | buff[14]);
                fullData.EogL = (int16_t)((buff[17] << 8) | buff[16]);
                fullData.EogR = (int16_t)((buff[19] << 8) | buff[18]);
                fullData.EogH = (int16_t)(fullData.EogL -fullData.EogR);
                fullData.EogV = (int16_t)(0 - ((fullData.EogL + fullData.EogR) / 2));
                [_delegate memeAcademicFullDataReceivedDelegate:(AcademicFullData*)fullData];
                break;
            case 0x9A:
                break;
            default:
                break;
        }
    }
}

// =============================================================================
#pragma mark - CoreBluetooth
// =============================================================================
// =============================================================================
#pragma mark - centralManagerDidUpdateState
// =============================================================================
// CoreBluetooth が初期化完了すると呼ばれる
- (void)centralManagerDidUpdateState:(CBCentralManager *)central {
    // libLog(@"Call : centralManagerDidUpdateState");
    
    Library_Flag = true;
}

// =============================================================================
#pragma mark - didDiscoverPeripheral
// =============================================================================
// Peripheral が発見されると呼ばれる
- (void)   centralManager:(CBCentralManager *)central
    didDiscoverPeripheral:(CBPeripheral *)peripheral
        advertisementData:(NSDictionary *)advertisementData
                     RSSI:(NSNumber *)RSSI
{
    @try {
        libLog(@"Call : didDiscoverPeripheral");
        BOOL alreadyFound = NO;
        for (CBPeripheral *p in self.peripherals){
            if ([p.identifier isEqual: peripheral.identifier]){
                alreadyFound = YES;
                break;
            }
        }
        if (!alreadyFound)  {
            NSString *uuid = [peripheral.identifier UUIDString];
            NSString *name = peripheral.name;
            NSString *localName = [advertisementData objectForKey:CBAdvertisementDataLocalNameKey];
            if (name == nil) { return; }
            [self.peripherals addObject: peripheral];
            [self.peripheralsLocalName addObject: name];

            libLog(@"発見したBLEデバイス:%@", peripheral);
            libLog(@"アドバタイズメントデータ:%@", advertisementData);
            libLog(@"Name %@",name);
            libLog(@"Local Name %@",localName);
            libLog(@"RSSI:%@",RSSI);
            libLog(@"UUID:%@",uuid);
            
            [_delegate memePeripheralFoundDelegate:(uint32_t)MEMELIB_OK DeviceName:(NSString*)name uuid:(NSString*)uuid];
        }
    }
    @catch (NSException *exception) {
        libLog(@"例外発生");
    }
}

// =============================================================================
#pragma mark - didConnectPeripheral
// =============================================================================
// 接続成功すると呼ばれる
- (void)  centralManager:(CBCentralManager *)central
    didConnectPeripheral:(CBPeripheral *)peripheral
{
    libLog(@"Call : didConnectPeripheral");
    peripheral.delegate = self;
    Service_Flag = false;
    
    // サービス探索開始
    //NSArray *services = [NSArray arrayWithObjects:[CBUUID UUIDWithString: @"180D"], nil];
    [peripheral discoverServices:nil];
}

// =============================================================================
#pragma mark - didFailToConnectPeripheral
// =============================================================================
// 接続失敗すると呼ばれる
- (void)        centralManager:(CBCentralManager *)central
    didFailToConnectPeripheral:(CBPeripheral *)peripheral
                         error:(NSError *)error
{
    libLog(@"Call : didFailToConnectPeripheral");
    [_delegate memePeripheralConnectedDelegate:(uint32_t)MEMELIB_NG];
    [self checkTimerStop];
}

// =============================================================================
#pragma mark - didDisconnectPeripheral
// =============================================================================
// 切断されると呼ばれる
- (void)        centralManager:(CBCentralManager *)central
       didDisconnectPeripheral:(CBPeripheral *)peripheral
                         error:(NSError *)error
{
    libLog(@"Call : didDisconnectPeripheral");
    if (error) {
        [_delegate memePeripheralDisconnectedDelegate:MEMELIB_NG];
        [self checkTimerStop];
        Connected_Flag = false;
        Measure_Flag = false;
        Service_Flag = false;
    }
    else {
        [_delegate memePeripheralDisconnectedDelegate:MEMELIB_OK];
        [self checkTimerStop];
        Connected_Flag = false;
        Measure_Flag = false;
        Service_Flag = false;
    }
}


// =============================================================================
#pragma mark - didDiscoverServices
// =============================================================================
// サービス発見時に呼ばれる
- (void)     peripheral:(CBPeripheral *)peripheral
    didDiscoverServices:(NSError *)error
{
    libLog(@"Call : didDiscoverServices");
    if (error) {
        libLog(@"エラー:%@", error);
        return;
    }
    for (int i=0; i < peripheral.services.count; i++) {
        CBService *myService = [peripheral.services objectAtIndex:i];
        if ([myService.UUID isEqual:[CBUUID UUIDWithString:SERVICES_UUID]]) {
            self.inputCharacteristic = nil;
            self.outputCharacteristic = nil;
    
            NSArray *services = [NSArray arrayWithObjects:[CBUUID UUIDWithString:CHARACTERISTICS_READ_UUID], [CBUUID UUIDWithString:CHARACTERISTICS_WRITE_UUID], nil];
            [peripheral discoverCharacteristics:services forService:myService];
            break;
        }
    }
}

// =============================================================================
#pragma mark - didDiscoverCharacteristicsForService
// =============================================================================
// キャラクタリスティック発見時に呼ばれる
- (void)                      peripheral:(CBPeripheral *)peripheral
    didDiscoverCharacteristicsForService:(CBService *)service
                                   error:(NSError *)error
{
    libLog(@"Call : didDiscoverCharacteristicsForService");
    if (error) {
        libLog(@"エラー:%@", error);
        return;
    }
    
    if ([service.UUID isEqual:[CBUUID UUIDWithString:SERVICES_UUID]]) {
        for (CBCharacteristic *characteristic in service.characteristics) {
            libLog(@"characteristic.UUID %@", characteristic.UUID);
            libLog(@"characteristic.properties %lu", (unsigned long)characteristic.properties);
            libLog(@"characteristic.description %@", characteristic.description);
            
            if ([characteristic.UUID isEqual:[CBUUID UUIDWithString:CHARACTERISTICS_READ_UUID]]) {
                self.inputCharacteristic = characteristic;
                libLog(@"Read を発見！");
                
                [peripheral setNotifyValue:YES forCharacteristic:characteristic];
            }
            if ([characteristic.UUID isEqual:[CBUUID UUIDWithString:CHARACTERISTICS_WRITE_UUID]]) {
                self.outputCharacteristic = characteristic;
                libLog(@"Write を発見！");
                
                if (Service_Flag == true)
                {
                    [self MEME_ADN_GET_DEV_INFO];
                }
            }
        }
    }
}

// =============================================================================
#pragma mark - didModifyServices
// =============================================================================
- (void)   peripheral:(CBPeripheral *)peripheral
    didModifyServices:(nonnull NSArray<CBService *> *)invalidatedServices
                error:(NSError *)error
{
    libLog(@"Call : didModifyServices");
    // サービス探索開始
    NSArray *services = [NSArray arrayWithObjects:[CBUUID UUIDWithString:SERVICES_UUID], nil];
    [peripheral discoverServices:services];
}

// =============================================================================
#pragma mark - didUpdateNotificationStateForCharacteristic
// =============================================================================
// Notify開始／停止時に呼ばれる
- (void)                             peripheral:(CBPeripheral *)peripheral
    didUpdateNotificationStateForCharacteristic:(CBCharacteristic *)characteristic
                                          error:(NSError *)error
{
    libLog(@"Call : didUpdateNotificationStateForCharacteristic");
    if (error) {
        libLog(@"Notify状態更新失敗...error:%@", error);
    }
    else {
        libLog(@"Notify状態更新成功！characteristic UUID:%@, isNotifying:%d",
                characteristic.UUID ,characteristic.isNotifying ? YES : NO);
        
        Connected_Flag = true;
        
        if (characteristic.isNotifying == YES) {
            if (self.outputCharacteristic == nil) {
                Service_Flag = true;
            }
            else {
                [self MEME_ADN_GET_DEV_INFO];
            }
        }
    }
}

// =============================================================================
#pragma mark - didUpdateValueForCharacteristic
// =============================================================================
- (void)                 peripheral:(CBPeripheral *)peripheral
    didUpdateValueForCharacteristic:(CBCharacteristic *)characteristic
                              error:(NSError *)error
{
    if (error) {
        libLog(@"Read失敗...error:%@, characteristic uuid:%@", error, characteristic.UUID);
        return;
    }
    
    [self.recvData addObject:characteristic.value];
    [self DataAnalysis];
}

// =============================================================================
#pragma mark - didReadValueForCharacteristic
// =============================================================================
- (void)                peripheral:(CBPeripheral *)peripheral
     didReadValueForCharacteristic:(CBCharacteristic *)characteristic
                             error:(NSError *)error
{
    if (error) {
        libLog(@"Write失敗...error:%@", error);
        return;
    }
}

// =============================================================================
#pragma mark - didWriteValueForCharacteristic
// =============================================================================
- (void)                peripheral:(CBPeripheral *)peripheral
    didWriteValueForCharacteristic:(CBCharacteristic *)characteristic
                             error:(NSError *)error
{
    if (error) {
        libLog(@"Write失敗...error:%@", error);
        return;
    }
}

@end
