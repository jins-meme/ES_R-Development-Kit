//
//  MEMELib_Academic.h
//  MEME_Academic
//
//  Created by D-CLUE on 2017/03/22.
//  Copyright © 2017年 jins-jp. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "AcademicStandardData.h"
#import "AcademicFullData.h"
#import "AcademicQuaternionData.h"
#import "Version.h"
#import "DecEnc.h"
@import CoreBluetooth;

static const uint32_t MEMELIB_OK = 0;
static const uint32_t MEMELIB_NG = 1;
static const uint32_t MEMELIB_TIMEOUT = 2;

static NSString* SERVICES_UUID = @"D6F25BD1-5B54-4360-96D8-7AA62E04C7EF";
static NSString* CHARACTERISTICS_READ_UUID = @"D6F25BD4-5B54-4360-96D8-7AA62E04C7EF";
static NSString* CHARACTERISTICS_WRITE_UUID = @"D6F25BD2-5B54-4360-96D8-7AA62E04C7EF";
static NSString* DEVICE_INFORMATION_UUID = @"180A";
static NSString* SYSTEM_ID_UUID = @"2A23";


static const uint32_t MEMEMode_Standard = 1;
static const uint32_t MEMEMode_Full = 2;
static const uint32_t MEMEMode_Quaternion = 3;

static const uint32_t MEMEQuality_High = 1;
static const uint32_t MEMEQuality_Low = 2;

static const uint32_t MEMEAccelRange_2G = 0;
static const uint32_t MEMEAccelRange_4G = 1;
static const uint32_t MEMEAccelRange_8G = 2;
static const uint32_t MEMEAccelRange_16G = 3;

static const uint32_t MEMEGyroRange_250dps = 0;
static const uint32_t MEMEGyroRange_500dps = 1;
static const uint32_t MEMEGyroRange_1000dps = 2;
static const uint32_t MEMEGyroRange_2000dps = 3;

static const NSInteger STATUS_IDLE = 0x01;
static const NSInteger STATUS_SCAN_REQ_EXEC = 0x02;
static const NSInteger STATUS_CONNECTION_REQ_EXEC = 0x03;
static const NSInteger STATUS_GET_STATUS_REQ_EXEC = 0x04;
static const NSInteger STATUS_GET_VERSION_REQ_EXEC = 0x05;
static const NSInteger STATUS_DISCONNECTION_REQ_EXEC = 0x06;
static const NSInteger STATUS_SET_TIME_REQ_EXEC = 0x07;
static const NSInteger STATUS_REC_START_REQ_EXEC = 0x08;
static const NSInteger STATUS_REC_STOP_REQ_EXEC = 0x9;

static const NSInteger CHECK_TIMEOUT_TIME = 20;
static const NSInteger TIME_SYNC_COUNT = 2;
static const NSInteger PACKET_LENGTH = 20;

@protocol memelibDelegate<NSObject>

- (void)memePeripheralFoundDelegate:(uint32_t)result DeviceName:(NSString*)DeviceName uuid:(NSString*)uuid;
- (void)memePeripheralConnectedDelegate:(uint32_t)result;
- (void)memePeripheralDisconnectedDelegate:(uint32_t)result;
- (void)memeAcademicStandardDataReceivedDelegate:(AcademicStandardData*)data;
- (void)memeAcademicFullDataReceivedDelegate:(AcademicFullData*)data;
- (void)memeAcademicQuaternionDataReceivedDelegate:(AcademicQuaternionData*)data;

@end

@interface MEMELib_Academic : NSObject<CBCentralManagerDelegate, CBPeripheralDelegate>

@property (nonatomic, strong) CBCentralManager *centralManager;
@property (nonatomic, strong) CBPeripheral *peripheral;
@property (nonatomic, strong) NSMutableArray *peripherals;
@property (nonatomic, strong) NSMutableArray *peripheralsLocalName;
@property (nonatomic, strong) CBCharacteristic *inputCharacteristic;
@property (nonatomic, strong) CBCharacteristic *outputCharacteristic;
@property (nonatomic, strong) NSMutableArray *recvData;
@property (nonatomic, strong) Version* MEMEVersion;
@property (nonatomic, strong) Version* SDKVersion;
@property (nonatomic, strong) NSString *macAddress;

@property (nonatomic, assign) id<memelibDelegate> delegate;

- (id)init;

- (uint32_t)startScanningPeripherals;
- (uint32_t)stopScanningPeripherals;
- (uint32_t)connectPeripheral:(NSString*)DeviceName;
- (uint32_t)disconnectPeripheral;
- (uint32_t)setSelectMode:(uint32_t)mode;
- (uint32_t)setTransMode:(uint32_t)mode;
- (uint32_t)setAccelRange:(uint32_t)accelRange;
- (uint32_t)setGyroRange:(uint32_t)gyroRange;
- (uint32_t)startDataReport;
- (uint32_t)stopDataReport;

- (uint32_t)getSelectMode;
- (uint32_t)getTransMode;
- (uint32_t)getAccelRange;
- (uint32_t)getGyroRange;

@end
