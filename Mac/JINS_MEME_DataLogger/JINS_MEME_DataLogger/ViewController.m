
#import "ViewController.h"
@import CoreBluetooth;
#include <sys/socket.h>
#include <netinet/in.h>

// Graph
NSString *const gyroXData = @"GyroX";
NSString *const gyroYData = @"GyroY";
NSString *const gyroZData = @"GyroZ";
NSString *const accXData = @"AccX";
NSString *const accYData = @"AccY";
NSString *const accZData = @"AccZ";
NSString *const eogL1Data = @"EogL1";
NSString *const eogR1Data = @"EogR1";
NSString *const eogH1Data = @"EogH1";
NSString *const eogV1Data = @"EogV1";

@interface ViewController ()<NSComboBoxDelegate, NSComboBoxDataSource, CBCentralManagerDelegate, CBPeripheralDelegate>

// Bluetooth
@property (nonatomic, strong) CBCentralManager *centralManager;
@property (nonatomic, strong) CBPeripheral *peripheral;
@property (nonatomic, strong) NSMutableArray *peripherals;
@property (nonatomic, strong) CBCharacteristic *settingCharacteristic;
@property (nonatomic, strong) CBCharacteristic *inputCharacteristic;
@property (nonatomic, strong) CBCharacteristic *outputCharacteristic;
@property (nonatomic, strong) NSMutableArray *recvData;
@property (nonatomic, strong) NSMutableArray *fileList;

@end

@implementation ViewController
{
    NSTimer *scanTimer;
    NSTimer *connectTimer;
    uint8_t DataBuff[20];
    BOOL ConnectFlag;
    BOOL DataRecvExec;
    NSInteger RecvCnt;

    uint32_t accGraphCnt;
    uint32_t gyroGraphCnt;
    uint32_t eogGraphCnt;
    
    uint32_t accGraphXMin;
    uint32_t accGraphXMax;
    uint32_t gyroGraphXMin;
    uint32_t gyroGraphXMax;
    uint32_t eogGraphXMin;
    uint32_t eogGraphXMax;
    
    CPTXYPlotSpace *accPlotSpace;
    CPTXYAxis *accXYAxisY;
    CPTXYPlotSpace *gyroPlotSpace;
    CPTXYAxis *gyroXYAxisY;
    CPTXYPlotSpace *eogPlotSpace;
    CPTXYAxis *eogXYAxisY;
    
    uint32_t csvDataCnt;
    NSMutableArray *csvData;
    NSURL *urlFilePath;
    NSString *sFilePath;
    NSString *sFilePath2;
    NSString *sFileName;
    NSString *wFilePath;
    NSString *rFilePath;
    BOOL artifact;
    NSString *sDeviceName;
    
    NSInteger csvDataMode;
    NSInteger csvCnt;
    NSArray *csvArray;

    NSTimer *csvTimer;
    float_t csvSpeed;
    
    int32_t tcpSock;
}

- (void)viewDidLoad {
    [super viewDidLoad];

    // Do any additional setup after loading the view.
    self.centralManager = [[CBCentralManager alloc] initWithDelegate:self
                                                               queue:nil];
    
    //self.comboBoxMEME.delegate = self;
    //self.comboBoxMEME.dataSource = self;
    
    self.comboBoxMEME.tag = 0;
    self.comboBoxSelectMode.tag = 1;
    self.comboBoxTransSpped.tag = 2;
    self.comboBoxAccelRange.tag = 3;
    self.comboBoxGyroRange.tag = 4;

    [self.comboBoxMEME removeAllItems];

    [self.comboBoxSelectMode removeAllItems];
    [self.comboBoxSelectMode insertItemWithObjectValue:@"Standard" atIndex:[self.comboBoxSelectMode numberOfItems]];
    [self.comboBoxSelectMode insertItemWithObjectValue:@"Full" atIndex:[self.comboBoxSelectMode numberOfItems]];
    [self.comboBoxSelectMode insertItemWithObjectValue:@"Quaternion" atIndex:[self.comboBoxSelectMode numberOfItems]];
    [self.comboBoxSelectMode selectItemAtIndex:0];

    [self.comboBoxTransSpped removeAllItems];
    [self.comboBoxTransSpped insertItemWithObjectValue:@"100Hz" atIndex:[self.comboBoxTransSpped numberOfItems]];
    [self.comboBoxTransSpped insertItemWithObjectValue:@"50Hz" atIndex:[self.comboBoxTransSpped numberOfItems]];
    [self.comboBoxTransSpped selectItemAtIndex:0];
    
    [self.comboBoxAccelRange removeAllItems];
    [self.comboBoxAccelRange insertItemWithObjectValue:@"2g" atIndex:[self.comboBoxAccelRange numberOfItems]];
    [self.comboBoxAccelRange insertItemWithObjectValue:@"4g" atIndex:[self.comboBoxAccelRange numberOfItems]];
    [self.comboBoxAccelRange insertItemWithObjectValue:@"8g" atIndex:[self.comboBoxAccelRange numberOfItems]];
    [self.comboBoxAccelRange insertItemWithObjectValue:@"16g" atIndex:[self.comboBoxAccelRange numberOfItems]];
    [self.comboBoxAccelRange selectItemAtIndex:0];

    [self.comboBoxGyroRange removeAllItems];
    [self.comboBoxGyroRange insertItemWithObjectValue:@"250dps" atIndex:[self.comboBoxGyroRange numberOfItems]];
    [self.comboBoxGyroRange insertItemWithObjectValue:@"500dps" atIndex:[self.comboBoxGyroRange numberOfItems]];
    [self.comboBoxGyroRange insertItemWithObjectValue:@"1000dps" atIndex:[self.comboBoxGyroRange numberOfItems]];
    [self.comboBoxGyroRange insertItemWithObjectValue:@"2000dps" atIndex:[self.comboBoxGyroRange numberOfItems]];
    [self.comboBoxGyroRange selectItemAtIndex:0];

    self.peripherals = @[].mutableCopy;
    self.recvData = @[].mutableCopy;
    self.fileList = @[].mutableCopy;
    
    [self.buttonScanDevice setHidden:NO];
    [self.buttonConnect setHidden:YES];
    [self.buttonInitialize setHidden:YES];
    [self.buttonStartMesure setHidden:YES];
    [self.buttonFreeMarking setHidden:YES];
    
    ConnectFlag = NO;
    DataRecvExec = NO;
    
    [self graphCreate];
    
    csvCnt = 0;
    csvData = [NSMutableArray array];
    sFilePath = NSHomeDirectory();
    sFilePath2 = @"/JINS/MEME academic/SensorData/";
    sFilePath = [sFilePath stringByAppendingString:sFilePath2];
    [self.comboBoxReplaySpeed selectItemAtIndex:0];
    [self.buttonFileReadStart setHidden:YES];
    [self.buttonFileReadPause setHidden:YES];
    [self.buttonFileReadStop setHidden:YES];
    
    [self checkIPAddress];
    [self tcpThread];
    [self.levelBattery setIntValue:0];
}

- (void)setRepresentedObject:(id)representedObject {
    [super setRepresentedObject:representedObject];

    // Update the view, if already loaded.
}

// =============================================================================
#pragma mark -
#pragma mark CBCentralManagerDelegate

- (void)centralManagerDidUpdateState:(CBCentralManager *)central {
}
- (void)   centralManager:(CBCentralManager *)central
    didDiscoverPeripheral:(CBPeripheral *)peripheral
        advertisementData:(NSDictionary *)advertisementData
                     RSSI:(NSNumber *)RSSI
{
    BOOL alreadyFound = NO;
    for (CBPeripheral *p in self.peripherals){
        if ([p.identifier isEqual: peripheral.identifier]){
            alreadyFound = YES;
            break;
        }
    }
    if (!alreadyFound)  {
        [self.peripherals addObject: peripheral];
        NSString *s = [peripheral.identifier UUIDString];
        [self.comboBoxMEME insertItemWithObjectValue:s atIndex:[self.comboBoxMEME numberOfItems]];
        [self.comboBoxMEME selectItemAtIndex:([self.comboBoxMEME numberOfItems]-1)];
        [self.buttonConnect setHidden:NO];
    }
}
- (void)  centralManager:(CBCentralManager *)central
    didConnectPeripheral:(CBPeripheral *)peripheral
{
    peripheral.delegate = self;
    [peripheral discoverServices:nil];
}
- (void)        centralManager:(CBCentralManager *)central
    didFailToConnectPeripheral:(CBPeripheral *)peripheral
                         error:(NSError *)error
{
}

// =============================================================================
#pragma mark - CBPeripheralDelegate

- (void)     peripheral:(CBPeripheral *)peripheral
    didDiscoverServices:(NSError *)error
{
    if (error) {
        return;
    }
    for (int i=0; i < peripheral.services.count; i++) {
        CBService *myService = [peripheral.services objectAtIndex:i];
        if([myService.UUID isEqual:[CBUUID UUIDWithString:@"D6F25BD1-5B54-4360-96D8-7AA62E04C7EF"]]) {
            [peripheral discoverCharacteristics:nil forService:myService];
        }
    }
}
- (void)                      peripheral:(CBPeripheral *)peripheral
    didDiscoverCharacteristicsForService:(CBService *)service
                                   error:(NSError *)error
{
    if (error) {
        return;
    }
    
    if ([service.UUID isEqual:[CBUUID UUIDWithString:@"D6F25BD1-5B54-4360-96D8-7AA62E04C7EF"]]) {
        for (CBCharacteristic *characteristic in service.characteristics) {
            if ([characteristic.UUID isEqual:[CBUUID UUIDWithString:@"D6F25BD2-5B54-4360-96D8-7AA62E04C7EF"]]) {
                self.inputCharacteristic = characteristic;
            }
            else if ([characteristic.UUID isEqual:[CBUUID UUIDWithString:@"D6F25BD4-5B54-4360-96D8-7AA62E04C7EF"]]) {
                self.outputCharacteristic = characteristic;
                [peripheral setNotifyValue:YES forCharacteristic:characteristic];
            }
        }
    }
}
- (void)                             peripheral:(CBPeripheral *)peripheral
    didUpdateNotificationStateForCharacteristic:(CBCharacteristic *)characteristic
                                          error:(NSError *)error
{
    if (error) {
    }
    else {
        if (characteristic.isNotifying == YES) {
            [self MEME_ADN_GET_DEV_INFO];
            [self MEME_ADN_GET_MODE];
        }
    }
}
- (void)                 peripheral:(CBPeripheral *)peripheral
    didUpdateValueForCharacteristic:(CBCharacteristic *)characteristic
                              error:(NSError *)error
{
    if (error) {
        return;
    }
    [self.recvData addObject:characteristic.value];
    [self DataAnalysis];
}
- (void)                peripheral:(CBPeripheral *)peripheral
    didWriteValueForCharacteristic:(CBCharacteristic *)characteristic
                             error:(NSError *)error
{
}

// =============================================================================
#pragma mark - Timer

- (void)time:(NSTimer*)timer{
    [scanTimer invalidate];
    [self.scanIndicator stopAnimation:nil];
    [self.centralManager stopScan];
}

- (void)connectTime:(NSTimer*)timer{
    [connectTimer invalidate];
    [self.centralManager cancelPeripheralConnection:self.peripheral];
    [self.scanIndicator stopAnimation:nil];
    [self.buttonConnect setHidden:YES];
    [self.buttonConnect setTitle:@"Connect"];
    [self.buttonInitialize setHidden:YES];
    [self.buttonStartMesure setHidden:YES];
    self.labelConnect.stringValue = @"Status : Disconnect";
}

- (void)csvTime:(NSTimer*)timer{
    [self csvDataRead];
}

// =============================================================================
#pragma mark - Button

- (IBAction)buttonScanDeviceTapped:(id)sender {
    if (![scanTimer isValid]) {
        [self.scanIndicator startAnimation:nil];
        [self.peripherals removeAllObjects];
        [self.comboBoxMEME removeAllItems];
        scanTimer = [NSTimer scheduledTimerWithTimeInterval:10
                                                     target:self
                                                   selector:@selector(time:)
                                                   userInfo:nil
                                                    repeats:YES];
        NSArray *services = [NSArray arrayWithObjects:[CBUUID UUIDWithString:@"D6F25BD1-5B54-4360-96D8-7AA62E04C7EF"], nil];
        NSDictionary *options = [NSDictionary dictionaryWithObject:[NSNumber numberWithBool:NO] forKey:CBCentralManagerScanOptionAllowDuplicatesKey];
        [self.centralManager scanForPeripheralsWithServices:services
                                                    options:options];
    }
}

- (IBAction)buttonConnectTapped:(id)sender {
    if (ConnectFlag == NO) {
        if ([scanTimer isValid]) {
            [scanTimer invalidate];
            [self.centralManager stopScan];
            [self.scanIndicator stopAnimation:nil];
        }
        if (![connectTimer isValid]) {
            [self.scanIndicator startAnimation:nil];
            NSInteger no = [self.comboBoxMEME indexOfSelectedItem];
            CBPeripheral *cbPeripheral = [self.peripherals objectAtIndex:no];
            self.peripheral = cbPeripheral;
            sDeviceName = [self.peripheral.identifier UUIDString];
            [self.centralManager connectPeripheral:self.peripheral
                                           options:nil];
            connectTimer = [NSTimer scheduledTimerWithTimeInterval:10
                                                            target:self
                                                          selector:@selector(connectTime:)
                                                          userInfo:nil
                                                           repeats:YES];
        }
    }
    else {
        [self.centralManager cancelPeripheralConnection:self.peripheral];
        [self.buttonScanDevice setHidden:NO];
        [self.buttonConnect setHidden:YES];
        [self.buttonConnect setTitle:@"Connect"];
        [self.buttonInitialize setHidden:YES];
        [self.buttonStartMesure setHidden:YES];
        self.labelConnect.stringValue = @"Status : Disconnect";
        ConnectFlag = NO;
    }
}

- (IBAction)buttonInitializeTapped:(id)sender {
    [self.comboBoxSelectMode selectItemAtIndex:0];
    [self.comboBoxTransSpped selectItemAtIndex:1];
    [self.comboBoxAccelRange selectItemAtIndex:0];
    [self.comboBoxGyroRange selectItemAtIndex:0];
    [self MEME_ADN_SET_MODE];
}

- (IBAction)buttonStartMeasureTapped:(id)sender {
    if (DataRecvExec == NO) {
        DataRecvExec = YES;
        RecvCnt = 0;
        csvDataCnt = 0;
        artifact = NO;
        [self graphInit];
        [self csvDataHeaderSave];
        [self MEME_ADN_SET_MODE];
        [self MEME_ADN_SET_6AXIS_PARAMS];
        [self MEME_ADN_GET_DATA:0x01];
        [self.buttonStartMesure setTitle:@"Stop Measurement"];
        [self.buttonConnect setHidden:YES];
        [self.buttonFreeMarking setHidden:NO];
    }
    else {
        DataRecvExec = NO;
        [self MEME_ADN_GET_DATA:0x00];
        [self.buttonStartMesure setTitle:@"Start Measurement"];
        [self.buttonConnect setHidden:NO];
        [self.buttonFreeMarking setHidden:YES];
    }
}

- (IBAction)buttonFreeMarkingTapped:(id)sender {
    artifact = YES;
}

- (IBAction)buttonCheckFileTapped:(id)sender {
    NSError *error = nil;
    NSString *fileName;
    NSFileManager *filemanager = [NSFileManager defaultManager];
    NSArray *files = [filemanager contentsOfDirectoryAtPath:sFilePath error:&error];
    [self.fileList removeAllObjects];
    [self.comboBoxFileLists removeAllItems];
    if ([files count] > 0) {
        for (int i = 0; i<([files count]); i++)
        {
            fileName = [files objectAtIndex:i];
            [self.fileList addObject: fileName];
            [self.comboBoxFileLists insertItemWithObjectValue:fileName atIndex:[self.comboBoxFileLists numberOfItems]];
            [self.comboBoxFileLists selectItemAtIndex:([self.comboBoxFileLists numberOfItems]-1)];
        }
        rFilePath = [sFilePath stringByAppendingString:fileName];
        [self csvDataHeaderRead];
        [self.buttonFileReadStart setHidden:NO];
    }
}

- (IBAction)comboBoxFileListsChanged:(id)sender {
    [self csvFileCheck];
}

- (IBAction)buttonFileReadStartTapped:(id)sender {
    NSString *filename = [self.comboBoxFileLists stringValue];
    rFilePath = [sFilePath stringByAppendingString:filename];
    NSString *text = [NSString stringWithContentsOfFile:rFilePath encoding:NSUTF8StringEncoding error:nil];
    csvArray = [text componentsSeparatedByString:@"\n"];
    NSString *s = csvArray[0];
    if ([s hasPrefix:@"// Data mode  : Standard"]) { csvDataMode = 1; }
    else if ([s hasPrefix:@"// Data mode  : Full"]) { csvDataMode = 2; }

    s = csvArray[1];
    if ([s hasPrefix:@"// Transmission speed  : 100Hz"]) { csvSpeed = 0.01; }
    else { csvSpeed = 0.02; }
    
    NSInteger replaySpeed = ([self.comboBoxReplaySpeed indexOfSelectedItem] + 1);
    csvSpeed = csvSpeed / replaySpeed;
 
    [self.buttonFileReadStart setHidden:YES];
    [self.buttonFileReadPause setHidden:NO];
    [self.buttonFileReadStop setHidden:NO];
    
    [self graphInit];

    csvCnt = 6;
    csvTimer = [NSTimer scheduledTimerWithTimeInterval:csvSpeed
                                                target:self
                                              selector:@selector(csvTime:)
                                              userInfo:nil
                                               repeats:YES];
}

- (IBAction)buttonFileReadPauseTapped:(id)sender {
    if ([csvTimer isValid]) {
        [csvTimer invalidate];
        //self.buttonFileReadPause.stringValue = @"Resume";
        [self.buttonFileReadPause setTitle:@"Resume"];
        [self.buttonFileReadStart setHidden:YES];
        [self.buttonFileReadPause setHidden:NO];
        [self.buttonFileReadStop setHidden:NO];
    }
    else {
        csvTimer = [NSTimer scheduledTimerWithTimeInterval:csvSpeed
                                                    target:self
                                                  selector:@selector(csvTime:)
                                                  userInfo:nil
                                                   repeats:YES];
        //self.buttonFileReadPause.stringValue = @"Pause";
        [self.buttonFileReadPause setTitle:@"Pause"];
        [self.buttonFileReadStart setHidden:YES];
        [self.buttonFileReadPause setHidden:NO];
        [self.buttonFileReadStop setHidden:NO];
    }
    
}

- (IBAction)buttonFileReadStopTapped:(id)sender {
    if ([csvTimer isValid]) {
        [csvTimer invalidate];
    }
    [self.buttonFileReadStart setHidden:NO];
    [self.buttonFileReadPause setHidden:YES];
    [self.buttonFileReadStop setHidden:YES];
}

// =============================================================================
#pragma mark - MEME Connection

- (void)MEME_ADN_GET_DEV_INFO {
    for (int i=0; i<20; i++) { DataBuff[i] = 0x00; }
    DataBuff[0] = 0x14;
    DataBuff[1] = 0xA1;
    [DecEnc Encode:DataBuff];
    NSData *data = [NSData dataWithBytes:DataBuff length:20];
    [self.peripheral writeValue:data
              forCharacteristic:self.inputCharacteristic
                           type:CBCharacteristicWriteWithResponse];
}

- (void)MEME_ADN_GET_MODE {
    for (int i=0; i<20; i++) { DataBuff[i] = 0x00; }
    DataBuff[0] = 0x14;
    DataBuff[1] = 0xA3;
    [DecEnc Encode:DataBuff];
    NSData *data = [NSData dataWithBytes:DataBuff length:20];
    [self.peripheral writeValue:data
              forCharacteristic:self.inputCharacteristic
                           type:CBCharacteristicWriteWithResponse];
}

- (void)MEME_ADN_SET_MODE {
    for (int i=0; i<20; i++) { DataBuff[i] = 0x00; }
    DataBuff[0] = 0x14;
    DataBuff[1] = 0xA4;
    DataBuff[4] = ([self.comboBoxSelectMode indexOfSelectedItem] + 1);
    DataBuff[5] = ([self.comboBoxTransSpped indexOfSelectedItem] + 1);
    [DecEnc Encode:DataBuff];
    NSData *data = [NSData dataWithBytes:DataBuff length:20];
    [self.peripheral writeValue:data
              forCharacteristic:self.inputCharacteristic
                           type:CBCharacteristicWriteWithResponse];
}

- (void)MEME_ADN_GET_6AXIS_PARAMS {
    for (int i=0; i<20; i++) { DataBuff[i] = 0x00; }
    DataBuff[0] = 0x14;
    DataBuff[1] = 0xA9;
    [DecEnc Encode:DataBuff];
    NSData *data = [NSData dataWithBytes:DataBuff length:20];
    [self.peripheral writeValue:data
              forCharacteristic:self.inputCharacteristic
                           type:CBCharacteristicWriteWithResponse];
}


- (void)MEME_ADN_SET_6AXIS_PARAMS {
    for (int i=0; i<20; i++) { DataBuff[i] = 0x00; }
    DataBuff[0] = 0x14;
    DataBuff[1] = 0xAA;
    DataBuff[2] = ([self.comboBoxAccelRange indexOfSelectedItem]);
    DataBuff[3] = ([self.comboBoxGyroRange indexOfSelectedItem]);
    [DecEnc Encode:DataBuff];
    NSData *data = [NSData dataWithBytes:DataBuff length:20];
    [self.peripheral writeValue:data
              forCharacteristic:self.inputCharacteristic
                           type:CBCharacteristicWriteWithResponse];
}

- (void)MEME_ADN_GET_DATA:(int)state {
    for (int i=0; i<20; i++) { DataBuff[i] = 0x00; }
    DataBuff[0] = 0x14;
    DataBuff[1] = 0xA0;
    DataBuff[2] = state;
    [DecEnc Encode:DataBuff];
    NSData *data = [NSData dataWithBytes:DataBuff length:20];
    [self.peripheral writeValue:data
              forCharacteristic:self.inputCharacteristic
                           type:CBCharacteristicWriteWithResponse];
}


// =============================================================================
#pragma mark - Data analysis

- (void)DataAnalysis {
    int8_t battery;
    NSInteger cnt = ([self.recvData count] - 1);
    NSData *data = [self.recvData objectAtIndex:cnt];
    NSUInteger len = [data length];
    memcpy(DataBuff, [data bytes], len);
    [DecEnc Decode:DataBuff];
    
    int16_t accX, accY, accZ;
    int16_t gyroX, gyroY, gyroZ;
    int16_t eogL1, eogR1, eogL2, eogR2, eogH1, eogH2, eogV1, eogV2;
    int revision, minor, major;
    NSString* version;
    
    if (DataBuff[0] == 0x14) {
        switch (DataBuff[1]) {
            case 0x81:
                revision = DataBuff[4];
                minor = DataBuff[5];
                major = DataBuff[6];
                version = [NSString stringWithFormat:@"%d.%d.%d",major,minor,revision];
                self.labelMEMEversion.stringValue = version;
                [self MEME_ADN_GET_MODE];
                break;
            case 0x83:
                [self.scanIndicator stopAnimation:nil];
                [self.comboBoxSelectMode selectItemAtIndex:(DataBuff[4] - 1)];
                [self.comboBoxTransSpped selectItemAtIndex:(DataBuff[5] - 1)];
                [self MEME_ADN_GET_6AXIS_PARAMS];
                break;
            case 0x89:
                [self.scanIndicator stopAnimation:nil];
                [self.comboBoxAccelRange selectItemAtIndex:(DataBuff[2])];
                [self.comboBoxGyroRange selectItemAtIndex:(DataBuff[3])];
                self.labelConnect.stringValue = @"Status : Connected";
                ConnectFlag = YES;
                [self.buttonConnect setTitle:@"Disconnect"];
                [self.buttonScanDevice setHidden:YES];
                [self.buttonInitialize setHidden:NO];
                [self.buttonStartMesure setHidden:NO];
                if ([connectTimer isValid]) {
                    [connectTimer invalidate];
                }
                break;
            case 0x8F:
                break;
            case 0x98:
                battery = (int8_t)(DataBuff[3] >> 4);
                [self.levelBattery setIntValue:battery];
                accX = (int16_t)((DataBuff[5] << 8) | DataBuff[4]);
                accY = (int16_t)((DataBuff[7] << 8) | DataBuff[6]);
                accZ = (int16_t)((DataBuff[9] << 8) | DataBuff[8]);
                [self accelGraphUpdate:accX:accY:accZ];
                eogL1 = (int16_t)((DataBuff[11] << 8) | DataBuff[10]);
                eogR1 = (int16_t)((DataBuff[13] << 8) | DataBuff[12]);
                eogL2 = (int16_t)((DataBuff[15] << 8) | DataBuff[14]);
                eogR2 = (int16_t)((DataBuff[17] << 8) | DataBuff[16]);
                eogH1 = eogL1 - eogR1;
                eogV1 = (0 - ((eogL1 + eogR1) / 2));
                eogH2 = eogL2 - eogR2;
                eogV2 = (0 - ((eogL2 + eogR2) / 2));
                [self eogGraphUpdate:eogL1 :eogR1 :eogH1 :eogV1];
                [self csvDataStandardSet:accX :accY :accZ :eogL1 :eogR1 :eogL2 :eogR2 :eogH1 :eogH2 :eogV1 :eogV2];
                artifact = NO;
                break;
            case 0x99:
                battery = (int8_t)(DataBuff[3] >> 4);
                [self.levelBattery setIntValue:battery];
                accX = (int16_t)((DataBuff[5] << 8) | DataBuff[4]);
                accY = (int16_t)((DataBuff[7] << 8) | DataBuff[6]);
                accZ = (int16_t)((DataBuff[9] << 8) | DataBuff[8]);
                gyroX = (int16_t)((DataBuff[11] << 8) | DataBuff[10]);
                gyroY = (int16_t)((DataBuff[13] << 8) | DataBuff[12]);
                gyroZ = (int16_t)((DataBuff[15] << 8) | DataBuff[14]);
                [self accelGraphUpdate:accX:accY:accZ];
                [self gyroGraphUpdate:gyroX:gyroY:gyroZ];
                eogL1 = (int16_t)((DataBuff[17] << 8) | DataBuff[16]);
                eogR1 = (int16_t)((DataBuff[19] << 8) | DataBuff[18]);
                eogH1 = eogL1 - eogR1;
                eogV1 = (0 - ((eogL1 + eogR1) / 2));
                [self eogGraphUpdate:eogL1 :eogR1 :eogH1 :eogV1];
                [self csvDataFullSet:accX:accY:accZ:gyroX:gyroY:gyroZ:eogL1:eogR1:eogH1:eogV1];
                artifact = NO;
                break;
            case 0x9A:
                RecvCnt++;
                break;
                
            default:
                break;
        }
    }
    
}



// =============================================================================
#pragma mark -
#pragma mark Plot Data Source Methods

- (void)graphCreate {
    [self accelGraphCreate];
    [self gyroGraphCreate];
    [self eogGraphCreate];
}

- (void)graphInit {
    [self accelGraphInit];
    [self gyroGraphInit];
    [self eogGraphInit];
}

- (void)accelGraphCreate {
    CPTMutableLineStyle *graphlineStyle;
    CPTScatterPlot *scatterPlot;
    self.accelXPlotData = [NSMutableArray array];
    self.accelYPlotData = [NSMutableArray array];
    self.accelZPlotData = [NSMutableArray array];
    CPTGraphHostingView *hostingView =
    [[CPTGraphHostingView alloc] initWithFrame:CGRectMake(350, 450, 640, 300)];
    [self.view addSubview:hostingView];
    accelGraph = [[CPTXYGraph alloc] initWithFrame:hostingView.bounds];
    hostingView.hostedGraph = accelGraph;
    accelGraph.plotAreaFrame.borderLineStyle = nil;
    accelGraph.plotAreaFrame.cornerRadius    = 0.0f;
    accelGraph.plotAreaFrame.masksToBorder   = NO;
    accelGraph.paddingLeft   = 0.0f;
    accelGraph.paddingRight  = 0.0f;
    accelGraph.paddingTop    = 0.0f;
    accelGraph.paddingBottom = 0.0f;
    accelGraph.plotAreaFrame.paddingLeft   = 60.0f;
    accelGraph.plotAreaFrame.paddingTop    = 60.0f;
    accelGraph.plotAreaFrame.paddingRight  = 20.0f;
    accelGraph.plotAreaFrame.paddingBottom = 65.0f;
    accPlotSpace = (CPTXYPlotSpace *)accelGraph.defaultPlotSpace;
    accPlotSpace.yRange = [CPTPlotRange plotRangeWithLocation:[NSNumber numberWithInt:-36000] length:[NSNumber numberWithInt:72000]];
    accGraphXMin = -200;
    accGraphXMax = 300;
    accPlotSpace.xRange = [CPTPlotRange plotRangeWithLocation:[NSNumber numberWithInt:accGraphXMin] length:[NSNumber numberWithInt:accGraphXMax]];
    CPTMutableTextStyle *textStyle = [CPTMutableTextStyle textStyle];
    textStyle.color                = [CPTColor colorWithComponentRed:0.447f green:0.443f blue:0.443f alpha:1.0f];
    textStyle.fontSize             = 10.0f;
    textStyle.textAlignment        = CPTTextAlignmentCenter;
    CPTMutableLineStyle *lineStyle = [CPTMutableLineStyle lineStyle];
    lineStyle.lineColor            = [CPTColor colorWithComponentRed:0.788f green:0.792f blue:0.792f alpha:1.0f];
    lineStyle.lineWidth            = 1.0f;
    CPTXYAxisSet *axisSet         = (CPTXYAxisSet *)accelGraph.axisSet;

    CPTXYAxis *x                  = axisSet.xAxis;
    x.axisLineStyle               = lineStyle;
    x.majorTickLineStyle          = lineStyle;
    x.minorTickLineStyle          = lineStyle;
    x.majorIntervalLength         = [NSNumber numberWithInt:100];
    x.orthogonalPosition          = [NSNumber numberWithInt:-36000];
    x.titleTextStyle              = textStyle;
    x.titleOffset                 = 30.0f;
    x.labelTextStyle              = textStyle;

    CPTXYAxis *y                  = axisSet.yAxis;
    accXYAxisY                    = axisSet.yAxis;
    y.axisLineStyle               = lineStyle;
    y.majorTickLineStyle          = lineStyle;
    y.minorTickLineStyle          = lineStyle;
    y.majorIntervalLength         = [NSNumber numberWithInt:12000];
    y.orthogonalPosition          = [NSNumber numberWithInt:accGraphXMin];
    y.titleTextStyle              = textStyle;
    y.titleRotation               = M_PI*2;
    y.titleOffset                 = 30.0f;
    y.majorGridLineStyle          = lineStyle;
    y.labelTextStyle              = textStyle;
    
    scatterPlot                   = [[CPTScatterPlot alloc] init];
    scatterPlot.identifier        = accXData;
    scatterPlot.dataSource        = self;
    graphlineStyle                = [scatterPlot.dataLineStyle mutableCopy];
    graphlineStyle.lineWidth      = 2;
    graphlineStyle.lineColor      = [CPTColor colorWithComponentRed:1.000f green:0.000f blue:0.000f alpha:1.000f];
    scatterPlot.dataLineStyle     = graphlineStyle;
    [accelGraph addPlot:scatterPlot];

    scatterPlot                   = [[CPTScatterPlot alloc] init];
    scatterPlot.identifier        = accYData;
    scatterPlot.dataSource        = self;
    graphlineStyle                = [scatterPlot.dataLineStyle mutableCopy];
    graphlineStyle.lineWidth      = 2;
    graphlineStyle.lineColor      = [CPTColor colorWithComponentRed:0.000f green:1.000f blue:0.000f alpha:1.000f];
    scatterPlot.dataLineStyle     = graphlineStyle;
    [accelGraph addPlot:scatterPlot];

    scatterPlot                   = [[CPTScatterPlot alloc] init];
    scatterPlot.identifier        = accZData;
    scatterPlot.dataSource        = self;
    graphlineStyle                = [scatterPlot.dataLineStyle mutableCopy];
    graphlineStyle.lineWidth      = 2;
    graphlineStyle.lineColor      = [CPTColor colorWithComponentRed:0.000f green:0.000f blue:1.000f alpha:1.000f];
    scatterPlot.dataLineStyle     = graphlineStyle;
    [accelGraph addPlot:scatterPlot];
    
    accGraphCnt = 0;
}

- (void)accelGraphInit {
    accGraphCnt = 0;
    [self.accelXPlotData removeAllObjects];
    [self.accelYPlotData removeAllObjects];
    [self.accelZPlotData removeAllObjects];
    accGraphXMin = -200;
    accGraphXMax = 300;
    accPlotSpace.xRange = [CPTPlotRange plotRangeWithLocation:[NSNumber numberWithInt:accGraphXMin] length:[NSNumber numberWithInt:accGraphXMax]];
    accXYAxisY.orthogonalPosition = [NSNumber numberWithInt:accGraphXMin];
    [accelGraph reloadData];
}

- (void)accelGraphUpdate:(int16_t)x :(int16_t)y :(int16_t)z {
    if (accGraphCnt >= accGraphXMax) {
        [self.accelXPlotData removeObjectAtIndex:0];
        [self.accelYPlotData removeObjectAtIndex:0];
        [self.accelZPlotData removeObjectAtIndex:0];
    }
    
	[self.accelXPlotData addObject:[NSMutableDictionary dictionaryWithObjectsAndKeys:[NSNumber numberWithInt:(int)accGraphCnt], @"x", [NSNumber numberWithInt:(int)x], @"y", nil]];
    [self.accelYPlotData addObject:[NSMutableDictionary dictionaryWithObjectsAndKeys:[NSNumber numberWithInt:(int)accGraphCnt], @"x", [NSNumber numberWithInt:(int)y], @"y", nil]];
    [self.accelZPlotData addObject:[NSMutableDictionary dictionaryWithObjectsAndKeys:[NSNumber numberWithInt:(int)accGraphCnt], @"x", [NSNumber numberWithInt:(int)z], @"y", nil]];

    accGraphCnt++;
    accGraphXMin++;
    accPlotSpace.xRange = [CPTPlotRange plotRangeWithLocation:[NSNumber numberWithInt:accGraphXMin] length:[NSNumber numberWithInt:accGraphXMax]];
    accXYAxisY.orthogonalPosition = [NSNumber numberWithInt:accGraphXMin];
    [accelGraph reloadData];
}

- (void)gyroGraphCreate {
    CPTMutableLineStyle *graphlineStyle;
    CPTScatterPlot *scatterPlot;
    self.gyroXPlotData = [NSMutableArray array];
    self.gyroYPlotData = [NSMutableArray array];
    self.gyroZPlotData = [NSMutableArray array];
    CPTGraphHostingView *hostingView =
    [[CPTGraphHostingView alloc] initWithFrame:CGRectMake(350, 220, 640, 300)];
    [self.view addSubview:hostingView];
    gyroGraph = [[CPTXYGraph alloc] initWithFrame:hostingView.bounds];
    hostingView.hostedGraph = gyroGraph;
    gyroGraph.plotAreaFrame.borderLineStyle = nil;
    gyroGraph.plotAreaFrame.cornerRadius    = 0.0f;
    gyroGraph.plotAreaFrame.masksToBorder   = NO;
    gyroGraph.paddingLeft   = 0.0f;
    gyroGraph.paddingRight  = 0.0f;
    gyroGraph.paddingTop    = 0.0f;
    gyroGraph.paddingBottom = 0.0f;
    gyroGraph.plotAreaFrame.paddingLeft   = 60.0f;
    gyroGraph.plotAreaFrame.paddingTop    = 60.0f;
    gyroGraph.plotAreaFrame.paddingRight  = 20.0f;
    gyroGraph.plotAreaFrame.paddingBottom = 65.0f;
    gyroPlotSpace = (CPTXYPlotSpace *)gyroGraph.defaultPlotSpace;
    gyroPlotSpace.yRange = [CPTPlotRange plotRangeWithLocation:[NSNumber numberWithInt:-36000] length:[NSNumber numberWithInt:72000]];
    gyroPlotSpace.xRange = [CPTPlotRange plotRangeWithLocation:[NSNumber numberWithInt:0] length:[NSNumber numberWithInt:1000]];
    gyroGraphXMin = -200;
    gyroGraphXMax = 300;
    gyroPlotSpace.xRange = [CPTPlotRange plotRangeWithLocation:[NSNumber numberWithInt:gyroGraphXMin] length:[NSNumber numberWithInt:gyroGraphXMax]];
    
    CPTMutableTextStyle *textStyle = [CPTMutableTextStyle textStyle];
    textStyle.color                = [CPTColor colorWithComponentRed:0.447f green:0.443f blue:0.443f alpha:1.0f];
    textStyle.fontSize             = 10.0f;
    textStyle.textAlignment        = CPTTextAlignmentCenter;
    CPTMutableLineStyle *lineStyle = [CPTMutableLineStyle lineStyle];
    lineStyle.lineColor            = [CPTColor colorWithComponentRed:0.788f green:0.792f blue:0.792f alpha:1.0f];
    lineStyle.lineWidth            = 1.0f;
    CPTXYAxisSet *axisSet         = (CPTXYAxisSet *)gyroGraph.axisSet;
    CPTXYAxis *x                  = axisSet.xAxis;
    x.axisLineStyle               = lineStyle;
    x.majorTickLineStyle          = lineStyle;
    x.minorTickLineStyle          = lineStyle;
    x.majorIntervalLength         = [NSNumber numberWithInt:100];
    x.orthogonalPosition          = [NSNumber numberWithInt:-36000];
    x.titleTextStyle              = textStyle;
    x.titleOffset                 = 30.0f;
    x.labelTextStyle              = textStyle;
    
    CPTXYAxis *y                  = axisSet.yAxis;
    gyroXYAxisY                   = axisSet.yAxis;
    y.axisLineStyle               = lineStyle;
    y.majorTickLineStyle          = lineStyle;
    y.minorTickLineStyle          = lineStyle;
    y.majorIntervalLength         = [NSNumber numberWithInt:12000];
    y.orthogonalPosition          = [NSNumber numberWithInt:gyroGraphXMin];
    y.titleTextStyle              = textStyle;
    y.titleRotation               = M_PI*2;
    y.titleOffset                 = 30.0f;
    y.majorGridLineStyle          = lineStyle;
    y.labelTextStyle              = textStyle;
    
    scatterPlot                   = [[CPTScatterPlot alloc] init];
    scatterPlot.identifier        = gyroXData;
    scatterPlot.dataSource        = self;
    graphlineStyle                = [scatterPlot.dataLineStyle mutableCopy];
    graphlineStyle.lineWidth      = 2;
    graphlineStyle.lineColor      = [CPTColor colorWithComponentRed:1.000f green:0.000f blue:0.000f alpha:1.000f];
    scatterPlot.dataLineStyle     = graphlineStyle;
    [gyroGraph addPlot:scatterPlot];

    scatterPlot                   = [[CPTScatterPlot alloc] init];
    scatterPlot.identifier        = gyroYData;
    scatterPlot.dataSource        = self;
    graphlineStyle                = [scatterPlot.dataLineStyle mutableCopy];
    graphlineStyle.lineWidth      = 2;
    graphlineStyle.lineColor      = [CPTColor colorWithComponentRed:0.000f green:1.000f blue:0.000f alpha:1.000f];
    scatterPlot.dataLineStyle     = graphlineStyle;
    [gyroGraph addPlot:scatterPlot];

    scatterPlot                   = [[CPTScatterPlot alloc] init];
    scatterPlot.identifier        = gyroZData;
    scatterPlot.dataSource        = self;
    graphlineStyle                = [scatterPlot.dataLineStyle mutableCopy];
    graphlineStyle.lineWidth      = 2;
    graphlineStyle.lineColor      = [CPTColor colorWithComponentRed:0.000f green:0.000f blue:1.000f alpha:1.000f];
    scatterPlot.dataLineStyle     = graphlineStyle;
    [gyroGraph addPlot:scatterPlot];
    
    gyroGraphCnt = 0;
}

- (void)gyroGraphInit {
    gyroGraphCnt = 0;
    [self.gyroXPlotData removeAllObjects];
    [self.gyroYPlotData removeAllObjects];
    [self.gyroZPlotData removeAllObjects];
    gyroGraphXMin = -200;
    gyroGraphXMax = 300;
    gyroPlotSpace.xRange = [CPTPlotRange plotRangeWithLocation:[NSNumber numberWithInt:gyroGraphXMin] length:[NSNumber numberWithInt:gyroGraphXMax]];
    gyroXYAxisY.orthogonalPosition = [NSNumber numberWithInt:gyroGraphXMin];
    [gyroGraph reloadData];
}

- (void)gyroGraphUpdate :(int16_t)x :(int16_t)y :(int16_t)z {
    if (gyroGraphCnt >= gyroGraphXMax) {
        [self.gyroXPlotData removeObjectAtIndex:0];
        [self.gyroYPlotData removeObjectAtIndex:0];
        [self.gyroZPlotData removeObjectAtIndex:0];
    }
    [self.gyroXPlotData addObject:[NSMutableDictionary dictionaryWithObjectsAndKeys:[NSNumber numberWithInt:(int)gyroGraphCnt], @"x", [NSNumber numberWithInt:(int)x], @"y", nil]];
    [self.gyroYPlotData addObject:[NSMutableDictionary dictionaryWithObjectsAndKeys:[NSNumber numberWithInt:(int)gyroGraphCnt], @"x", [NSNumber numberWithInt:(int)y], @"y", nil]];
    [self.gyroZPlotData addObject:[NSMutableDictionary dictionaryWithObjectsAndKeys:[NSNumber numberWithInt:(int)gyroGraphCnt], @"x", [NSNumber numberWithInt:(int)z], @"y", nil]];
    
    gyroGraphCnt++;
    gyroGraphXMin++;
    gyroPlotSpace.xRange = [CPTPlotRange plotRangeWithLocation:[NSNumber numberWithInt:gyroGraphXMin] length:[NSNumber numberWithInt:gyroGraphXMax]];
    gyroXYAxisY.orthogonalPosition = [NSNumber numberWithInt:gyroGraphXMin];
    [gyroGraph reloadData];
}

- (void)eogGraphCreate {
    CPTMutableLineStyle *graphlineStyle;
    CPTScatterPlot *scatterPlot;
    self.eogL1PlotData = [NSMutableArray array];
    self.eogR1PlotData = [NSMutableArray array];
    self.eogH1PlotData = [NSMutableArray array];
    self.eogV1PlotData = [NSMutableArray array];
    CPTGraphHostingView *hostingView =
    [[CPTGraphHostingView alloc] initWithFrame:CGRectMake(350, -10, 640, 300)];
    [self.view addSubview:hostingView];
    eogGraph = [[CPTXYGraph alloc] initWithFrame:hostingView.bounds];
    hostingView.hostedGraph = eogGraph;
    eogGraph.plotAreaFrame.borderLineStyle = nil;
    eogGraph.plotAreaFrame.cornerRadius    = 0.0f;
    eogGraph.plotAreaFrame.masksToBorder   = NO;
    eogGraph.paddingLeft   = 0.0f;
    eogGraph.paddingRight  = 0.0f;
    eogGraph.paddingTop    = 0.0f;
    eogGraph.paddingBottom = 0.0f;
    eogGraph.plotAreaFrame.paddingLeft   = 60.0f;
    eogGraph.plotAreaFrame.paddingTop    = 60.0f;
    eogGraph.plotAreaFrame.paddingRight  = 20.0f;
    eogGraph.plotAreaFrame.paddingBottom = 65.0f;
    eogPlotSpace = (CPTXYPlotSpace *)eogGraph.defaultPlotSpace;
    eogPlotSpace.yRange = [CPTPlotRange plotRangeWithLocation:[NSNumber numberWithInt:-1200] length:[NSNumber numberWithInt:2400]];
    eogGraphXMin = -200;
    eogGraphXMax = 300;
    eogPlotSpace.xRange = [CPTPlotRange plotRangeWithLocation:[NSNumber numberWithInt:eogGraphXMin] length:[NSNumber numberWithInt:eogGraphXMax]];
    
    CPTMutableTextStyle *textStyle = [CPTMutableTextStyle textStyle];
    textStyle.color                = [CPTColor colorWithComponentRed:0.447f green:0.443f blue:0.443f alpha:1.0f];
    textStyle.fontSize             = 10.0f;
    textStyle.textAlignment        = CPTTextAlignmentCenter;
    CPTMutableLineStyle *lineStyle = [CPTMutableLineStyle lineStyle];
    lineStyle.lineColor            = [CPTColor colorWithComponentRed:0.788f green:0.792f blue:0.792f alpha:1.0f];
    lineStyle.lineWidth            = 1.0f;
    CPTXYAxisSet *axisSet         = (CPTXYAxisSet *)eogGraph.axisSet;
    CPTXYAxis *x                  = axisSet.xAxis;
    x.axisLineStyle               = lineStyle;
    x.majorTickLineStyle          = lineStyle;
    x.minorTickLineStyle          = lineStyle;
    x.majorIntervalLength         = [NSNumber numberWithInt:100];
    x.orthogonalPosition          = [NSNumber numberWithInt:-1200];
    x.titleTextStyle              = textStyle;
    x.titleOffset                 = 30.0f;
    x.labelTextStyle              = textStyle;
    CPTXYAxis *y                  = axisSet.yAxis;
    eogXYAxisY                    = axisSet.yAxis;
    y.axisLineStyle               = lineStyle;
    y.majorTickLineStyle          = lineStyle;
    y.minorTickLineStyle          = lineStyle;
    y.majorIntervalLength         = [NSNumber numberWithInt:400];
    y.orthogonalPosition          = [NSNumber numberWithInt:eogGraphXMin];
    y.titleTextStyle              = textStyle;
    y.titleRotation               = M_PI*2;
    y.titleOffset                 = 30.0f;
    y.majorGridLineStyle          = lineStyle;
    y.labelTextStyle              = textStyle;
    
    scatterPlot                   = [[CPTScatterPlot alloc] init];
    scatterPlot.identifier        = eogL1Data;
    scatterPlot.dataSource        = self;
    graphlineStyle                = [scatterPlot.dataLineStyle mutableCopy];
    graphlineStyle.lineWidth      = 2;
    graphlineStyle.lineColor      = [CPTColor colorWithComponentRed:1.000f green:0.000f blue:0.000f alpha:1.000f];
    scatterPlot.dataLineStyle     = graphlineStyle;
    [eogGraph addPlot:scatterPlot];
    
    scatterPlot                   = [[CPTScatterPlot alloc] init];
    scatterPlot.identifier        = eogR1Data;
    scatterPlot.dataSource        = self;
    graphlineStyle                = [scatterPlot.dataLineStyle mutableCopy];
    graphlineStyle.lineWidth      = 2;
    graphlineStyle.lineColor      = [CPTColor colorWithComponentRed:0.000f green:0.000f blue:1.000f alpha:1.000f];
    scatterPlot.dataLineStyle     = graphlineStyle;
    [eogGraph addPlot:scatterPlot];
    
    scatterPlot                   = [[CPTScatterPlot alloc] init];
    scatterPlot.identifier        = eogH1Data;
    scatterPlot.dataSource        = self;
    graphlineStyle                = [scatterPlot.dataLineStyle mutableCopy];
    graphlineStyle.lineWidth      = 2;
    graphlineStyle.lineColor      = [CPTColor colorWithComponentRed:1.000f green:0.000f blue:1.000f alpha:1.000f];
    scatterPlot.dataLineStyle     = graphlineStyle;
    [eogGraph addPlot:scatterPlot];
    
    scatterPlot                   = [[CPTScatterPlot alloc] init];
    scatterPlot.identifier        = eogV1Data;
    scatterPlot.dataSource        = self;
    graphlineStyle                = [scatterPlot.dataLineStyle mutableCopy];
    graphlineStyle.lineWidth      = 2;
    graphlineStyle.lineColor      = [CPTColor colorWithComponentRed:0.000f green:1.000f blue:1.000f alpha:1.000f];
    scatterPlot.dataLineStyle     = graphlineStyle;
    [eogGraph addPlot:scatterPlot];
    
    eogGraphCnt = 0;
}

- (void)eogGraphInit {
    eogGraphCnt = 0;
    [self.eogL1PlotData removeAllObjects];
    [self.eogR1PlotData removeAllObjects];
    [self.eogH1PlotData removeAllObjects];
    [self.eogV1PlotData removeAllObjects];
    eogGraphXMin = -200;
    eogGraphXMax = 300;
    eogPlotSpace.xRange = [CPTPlotRange plotRangeWithLocation:[NSNumber numberWithInt:eogGraphXMin] length:[NSNumber numberWithInt:eogGraphXMax]];
    eogXYAxisY.orthogonalPosition = [NSNumber numberWithInt:eogGraphXMin];
    [eogGraph reloadData];
}

- (void)eogGraphUpdate :(int16_t)l1 :(int16_t)r1 :(int16_t)h1 :(int16_t)v1 {
    if (eogGraphCnt >= eogGraphXMax) {
        [self.eogL1PlotData removeObjectAtIndex:0];
        [self.eogR1PlotData removeObjectAtIndex:0];
        [self.eogH1PlotData removeObjectAtIndex:0];
        [self.eogV1PlotData removeObjectAtIndex:0];
    }
    
    [self.eogL1PlotData addObject:[NSMutableDictionary dictionaryWithObjectsAndKeys:[NSNumber numberWithInt:(int)eogGraphCnt], @"x", [NSNumber numberWithInt:(int)l1], @"y", nil]];
    [self.eogR1PlotData addObject:[NSMutableDictionary dictionaryWithObjectsAndKeys:[NSNumber numberWithInt:(int)eogGraphCnt], @"x", [NSNumber numberWithInt:(int)r1], @"y", nil]];
    [self.eogH1PlotData addObject:[NSMutableDictionary dictionaryWithObjectsAndKeys:[NSNumber numberWithInt:(int)eogGraphCnt], @"x", [NSNumber numberWithInt:(int)h1], @"y", nil]];
    [self.eogV1PlotData addObject:[NSMutableDictionary dictionaryWithObjectsAndKeys:[NSNumber numberWithInt:(int)eogGraphCnt], @"x", [NSNumber numberWithInt:(int)v1], @"y", nil]];
    
    eogGraphCnt++;
    eogGraphXMin++;
    eogPlotSpace.xRange = [CPTPlotRange plotRangeWithLocation:[NSNumber numberWithInt:eogGraphXMin] length:[NSNumber numberWithInt:eogGraphXMax]];
    eogXYAxisY.orthogonalPosition = [NSNumber numberWithInt:eogGraphXMin];
    [eogGraph reloadData];
}
- (NSUInteger)numberOfRecordsForPlot:(CPTPlot *)plot
{
    NSUInteger numRecords = 0;
    NSString *identifier  = (NSString *)plot.identifier;
    if ([identifier isEqualToString:gyroXData]) {
        numRecords = self.gyroXPlotData.count;
        if (numRecords > gyroGraphXMax) { numRecords = gyroGraphXMax; }
    }
    else if ([identifier isEqualToString:gyroYData]) {
        numRecords = self.gyroYPlotData.count;
        if (numRecords > gyroGraphXMax) { numRecords = gyroGraphXMax; }
    }
    else if ([identifier isEqualToString:gyroZData]) {
        numRecords = self.gyroZPlotData.count;
        if (numRecords > gyroGraphXMax) { numRecords = gyroGraphXMax; }
    }
    else if([identifier isEqualToString:accXData]) {
        numRecords = self.accelXPlotData.count;
        if (numRecords > accGraphXMax) { numRecords = accGraphXMax; }
    }
    else if([identifier isEqualToString:accYData]) {
        numRecords = self.accelYPlotData.count;
        if (numRecords > accGraphXMax) { numRecords = accGraphXMax; }
    }
    else if([identifier isEqualToString:accZData]) {
        numRecords = self.accelZPlotData.count;
        if (numRecords > accGraphXMax) { numRecords = accGraphXMax; }
    }
    else if([identifier isEqualToString:eogL1Data]) {
        numRecords = self.eogL1PlotData.count;
        if (numRecords > eogGraphXMax) { numRecords = eogGraphXMax; }
    }
    else if([identifier isEqualToString:eogR1Data]) {
        numRecords = self.eogR1PlotData.count;
        if (numRecords > eogGraphXMax) { numRecords = eogGraphXMax; }
    }
    else if([identifier isEqualToString:eogH1Data]) {
        numRecords = self.eogH1PlotData.count;
        if (numRecords > eogGraphXMax) { numRecords = eogGraphXMax; }
    }
    else if([identifier isEqualToString:eogV1Data]) {
        numRecords = self.eogV1PlotData.count;
        if (numRecords > eogGraphXMax) { numRecords = eogGraphXMax; }
    }
    
    return numRecords;
}
- (NSNumber *)numberForPlot:(CPTPlot *)plot field:(NSUInteger)fieldEnum recordIndex:(NSUInteger)index
{
    NSNumber *num        = nil;
    NSString *identifier = (NSString *)plot.identifier;
    if ([identifier isEqualToString:accXData]) {
        switch (fieldEnum) {
            case CPTScatterPlotFieldX:
                num = [[self.accelXPlotData objectAtIndex:index] valueForKey:@"x"];
                break;
            case CPTScatterPlotFieldY:
                num = [[self.accelXPlotData objectAtIndex:index] valueForKey:@"y"];
                break;
            default:
                break;
        }
    }
    else if ([identifier isEqualToString:accYData]) {
        switch (fieldEnum) {
            case CPTScatterPlotFieldX:
                num = [[self.accelYPlotData objectAtIndex:index] valueForKey:@"x"];
                break;
            case CPTScatterPlotFieldY:
                num = [[self.accelYPlotData objectAtIndex:index] valueForKey:@"y"];
                break;
            default:
                break;
        }
    }
    else if ([identifier isEqualToString:accZData]) {
        switch (fieldEnum) {
            case CPTScatterPlotFieldX:
                num = [[self.accelZPlotData objectAtIndex:index] valueForKey:@"x"];
                break;
            case CPTScatterPlotFieldY:
                num = [[self.accelZPlotData objectAtIndex:index] valueForKey:@"y"];
                break;
            default:
                break;
        }
    }
    else if ([identifier isEqualToString:gyroXData]) {
        switch (fieldEnum) {
            case CPTScatterPlotFieldX:
                num = [[self.gyroXPlotData objectAtIndex:index] valueForKey:@"x"];
                break;
            case CPTScatterPlotFieldY:
                num = [[self.gyroXPlotData objectAtIndex:index] valueForKey:@"y"];
                break;
            default:
                break;
        }
    }
    else if ([identifier isEqualToString:gyroYData]) {
        switch (fieldEnum) {
            case CPTScatterPlotFieldX:
                num = [[self.gyroYPlotData objectAtIndex:index] valueForKey:@"x"];
                break;
            case CPTScatterPlotFieldY:
                num = [[self.gyroYPlotData objectAtIndex:index] valueForKey:@"y"];
                break;
            default:
                break;
        }
    }
    else if ([identifier isEqualToString:gyroZData]) {
        switch (fieldEnum) {
            case CPTScatterPlotFieldX:
                num = [[self.gyroZPlotData objectAtIndex:index] valueForKey:@"x"];
                break;
            case CPTScatterPlotFieldY:
                num = [[self.gyroZPlotData objectAtIndex:index] valueForKey:@"y"];
                break;
            default:
                break;
        }
    }
    else if ([identifier isEqualToString:eogL1Data]) {
        switch (fieldEnum) {
            case CPTScatterPlotFieldX:
                num = [[self.eogL1PlotData objectAtIndex:index] valueForKey:@"x"];
                break;
            case CPTScatterPlotFieldY:
                num = [[self.eogL1PlotData objectAtIndex:index] valueForKey:@"y"];
                break;
            default:
                break;
        }
    }
    else if ([identifier isEqualToString:eogR1Data]) {
        switch (fieldEnum) {
            case CPTScatterPlotFieldX:
                num = [[self.eogR1PlotData objectAtIndex:index] valueForKey:@"x"];
                break;
            case CPTScatterPlotFieldY:
                num = [[self.eogR1PlotData objectAtIndex:index] valueForKey:@"y"];
                break;
            default:
                break;
        }
    }
    else if ([identifier isEqualToString:eogH1Data]) {
        switch (fieldEnum) {
            case CPTScatterPlotFieldX:
                num = [[self.eogH1PlotData objectAtIndex:index] valueForKey:@"x"];
                break;
            case CPTScatterPlotFieldY:
                num = [[self.eogH1PlotData objectAtIndex:index] valueForKey:@"y"];
                break;
            default:
                break;
        }
    }
    else if ([identifier isEqualToString:eogV1Data]) {
        switch (fieldEnum) {
            case CPTScatterPlotFieldX:
                num = [[self.eogV1PlotData objectAtIndex:index] valueForKey:@"x"];
                break;
            case CPTScatterPlotFieldY:
                num = [[self.eogV1PlotData objectAtIndex:index] valueForKey:@"y"];
                break;
            default:
                break;
        }
    }
    
    return num;
}

// =============================================================================
#pragma mark Csv Data Save

- (void)csvDataHeaderSave {
    NSDate *date = [NSDate date];
    NSDateFormatter *dateFormatter = [[NSDateFormatter alloc] init];
    dateFormatter.dateFormat = @"yyyyMMddHHmmss";
    NSString *date24 = [dateFormatter stringFromDate:date];
    sFileName = [NSString stringWithFormat:@"%@_%@.csv",date24,sDeviceName];
    wFilePath = [sFilePath stringByAppendingPathComponent:sFileName];
    NSFileManager *fileManager = [NSFileManager defaultManager];
    BOOL result = [fileManager fileExistsAtPath:wFilePath];
    if(!result){
        result = [self createFile:wFilePath];
    }
    if(!result){
        return;
    }
    NSFileHandle *fileHandle = [NSFileHandle fileHandleForWritingAtPath:wFilePath];
    if(!wFilePath){
        return;
    }
    
    NSString *writeLine;
    NSData *writeData;
    NSInteger param = [self.comboBoxSelectMode indexOfSelectedItem];
    if (param == 0) {
        writeLine = @"// Data mode  : Standard\n";
    }
    else {
        writeLine = @"// Data mode  : Full\n";
    }
    writeData = [writeLine dataUsingEncoding:NSUTF8StringEncoding];
    [fileHandle seekToEndOfFile];
    [fileHandle writeData:writeData];
    
    param = [self.comboBoxTransSpped indexOfSelectedItem];
    if (param == 0) {
        writeLine = @"// Transmission speed  : 50Hz\n";
    }
    else {
        writeLine = @"// Transmission speed  : 100Hz\n";
    }
    writeData = [writeLine dataUsingEncoding:NSUTF8StringEncoding];
    [fileHandle seekToEndOfFile];
    [fileHandle writeData:writeData];

    param = [self.comboBoxAccelRange indexOfSelectedItem];
    if (param == 0) {
        writeLine = @"// Accelerometer sensor's range  : 2g\n";
    }
    else if (param == 1) {
        writeLine = @"// Accelerometer sensor's range  : 4g\n";
    }
    else if (param == 2) {
        writeLine = @"// Accelerometer sensor's range  : 8g\n";
    }
    else {
        writeLine = @"// Accelerometer sensor's range  : 16g\n";
    }
    writeData = [writeLine dataUsingEncoding:NSUTF8StringEncoding];
    [fileHandle seekToEndOfFile];
    [fileHandle writeData:writeData];

    param = [self.comboBoxGyroRange indexOfSelectedItem];
    if (param == 0) {
        writeLine = @"// Gyroscope sensor's range  : 250dps\n";
    }
    else if (param == 1) {
        writeLine = @"// Gyroscope sensor's range  : 500dps\n";
    }
    else if (param == 2) {
        writeLine = @"// Gyroscope sensor's range  : 1000dps\n";
    }
    else {
        writeLine = @"// Gyroscope sensor's range  : 2000dps\n";
    }
    writeData = [writeLine dataUsingEncoding:NSUTF8StringEncoding];
    [fileHandle seekToEndOfFile];
    [fileHandle writeData:writeData];
    
    writeLine = @"//\n";
    writeData = [writeLine dataUsingEncoding:NSUTF8StringEncoding];
    [fileHandle seekToEndOfFile];
    [fileHandle writeData:writeData];

    param = [self.comboBoxSelectMode indexOfSelectedItem];
    if (param == 0) {
        writeLine = @"//ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,EOG_L1,EOG_R1,EOG_L2,EOG_R2,EOG_H1,EOG_H2,EOG_V1,EOG_V2\n";
    }
    else {
        writeLine = @"//ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,GYRO_X,GYRO_Y,GYRO_Z,EOG_L,EOG_R,EOG_H,EOG_V\n";
    }
    writeData = [writeLine dataUsingEncoding:NSUTF8StringEncoding];
    [fileHandle seekToEndOfFile];
    [fileHandle writeData:writeData];
    [fileHandle synchronizeFile];
    [fileHandle closeFile];
}

- (BOOL)createFile:(NSString *)filePath
{
    return [[NSFileManager defaultManager] createFileAtPath:filePath contents:[NSData data] attributes:nil];
}

- (void)csvDataStandardSet :(int16_t)accX :(int16_t)accY :(int16_t)accZ :(int16_t)eogL1 :(int16_t)eogR1 :(int16_t)eogL2 :(int16_t)eogR2 :(int16_t)eogH1 :(int16_t)eogH2 :(int16_t)eogV1 :(int16_t)eogV2 {
    csvDataCnt++;
    NSDate *date = [NSDate date];
    NSDateFormatter *dateFormatter = [[NSDateFormatter alloc] init];
    dateFormatter.dateFormat = @"yyyy/MM/dd HH:mm:ss.SS";
    NSString *date24 = [dateFormatter stringFromDate:date];
    [csvData addObject:[NSArray arrayWithObjects:
                        [NSNumber numberWithBool:artifact],
                        [NSNumber numberWithInt:(int)csvDataCnt],
                        date24,
                        [NSNumber numberWithInt:(int16_t)accX],
                        [NSNumber numberWithInt:(int16_t)accY],
                        [NSNumber numberWithInt:(int16_t)accZ],
                        [NSNumber numberWithInt:(int16_t)eogL1],
                        [NSNumber numberWithInt:(int16_t)eogR1],
                        [NSNumber numberWithInt:(int16_t)eogL2],
                        [NSNumber numberWithInt:(int16_t)eogR2],
                        [NSNumber numberWithInt:(int16_t)eogH1],
                        [NSNumber numberWithInt:(int16_t)eogH2],
                        [NSNumber numberWithInt:(int16_t)eogV1],
                        [NSNumber numberWithInt:(int16_t)eogV2],
                        nil]];
    if ((csvDataCnt % 100) == 0) {
        [self csvDataWrite];
    }
    
    NSString *tcpData = [NSString stringWithFormat:@"%d,%d,%@,%d,%d,%d,%d,%d,%d,%d,%d,%d,%d,%d",artifact,csvDataCnt,date24,accX,accY,accZ,eogL1,eogR1,eogL2,eogR2,eogH1,eogH2,eogV1,eogV2];
    char *utf8chr = (char*)[tcpData cStringUsingEncoding:
                            NSUTF8StringEncoding];
    [self socketWrite:utf8chr];
}

- (void)csvDataFullSet :(int16_t)accX :(int16_t)accY :(int16_t)accZ :(int16_t)gyroX :(int16_t)gyroY :(int16_t)gyroZ :(int16_t)eogL :(int16_t)eogR :(int16_t)eogH :(int16_t)eogV {
    csvDataCnt++;
    NSDate *date = [NSDate date];
    NSDateFormatter *dateFormatter = [[NSDateFormatter alloc] init];
    dateFormatter.dateFormat = @"yyyy/MM/dd HH:mm:ss.SS";
    NSString *date24 = [dateFormatter stringFromDate:date];
    [csvData addObject:[NSArray arrayWithObjects:
                        [NSNumber numberWithBool:artifact],
                        [NSNumber numberWithInt:(int)csvDataCnt],
                        date24,
                        [NSNumber numberWithInt:(int16_t)accX],
                        [NSNumber numberWithInt:(int16_t)accY],
                        [NSNumber numberWithInt:(int16_t)accZ],
                        [NSNumber numberWithInt:(int16_t)gyroX],
                        [NSNumber numberWithInt:(int16_t)gyroY],
                        [NSNumber numberWithInt:(int16_t)gyroZ],
                        [NSNumber numberWithInt:(int16_t)eogL],
                        [NSNumber numberWithInt:(int16_t)eogR],
                        [NSNumber numberWithInt:(int16_t)eogH],
                        [NSNumber numberWithInt:(int16_t)eogV],
                        nil]];
    if ((csvDataCnt % 100) == 0) {
        [self csvDataWrite];
    }

    NSString *tcpData = [NSString stringWithFormat:@"%d,%d,%@,%d,%d,%d,%d,%d,%d,%d,%d,%d,%d",artifact,csvDataCnt,date24,accX,accY,accZ,gyroX,gyroY,gyroZ,eogL,eogR,eogH,eogV];
    tcpData = [tcpData stringByAppendingString:@"\n"];
    char *utf8chr = (char*)[tcpData cStringUsingEncoding:
                            NSUTF8StringEncoding];
    [self socketWrite:utf8chr];
}

- (void)csvDataWrite {
    int cnt = (int)csvData.count;
    NSFileHandle *fileHandle = [NSFileHandle fileHandleForWritingAtPath:wFilePath];
    if(!wFilePath){ return; }
    for (int i=0; i<cnt; i++){
        NSString *writeLine = [csvData[i] componentsJoinedByString:@","];
        writeLine = [writeLine stringByAppendingString:@"\n"];
        NSData *data = [writeLine dataUsingEncoding:NSUTF8StringEncoding];
        [fileHandle seekToEndOfFile];
        [fileHandle writeData:data];
    }
    [fileHandle synchronizeFile];
    [fileHandle closeFile];
    [csvData removeAllObjects];
}

- (void)csvFileCheck {
    NSString *filename = [self.comboBoxFileLists stringValue];
    if ([filename isEqualToString:@""]) { return; }
    rFilePath = [sFilePath stringByAppendingString:filename];
    [self csvDataHeaderRead];
}

- (void)csvDataHeaderRead {
    NSString *text = [NSString stringWithContentsOfFile:rFilePath encoding:NSUTF8StringEncoding error:nil];
    NSArray *lines = [text componentsSeparatedByString:@"\n"];
    NSInteger cnt = 0;
    NSArray *array;
    NSString *s;
    for (NSString *row in lines) {
        array = [row componentsSeparatedByString:@"// "];
        s = array[1];
        
        switch (cnt) {
            case 0:
                self.labelCSVFileDataMode.stringValue = s;
                break;
            case 1:
                self.labelCSVFileTrasSpeed.stringValue = s;
                break;
            case 2:
                self.labelCSVFileAccRange.stringValue = s;
                break;
            case 3:
                self.labelCSVFileGyroRange.stringValue = s;
                break;
            default:
                break;
        }
        if (cnt >= 3) { break; }
        cnt++;
    }
    s = lines[7];
    array = [s componentsSeparatedByString:@","];
    s = array[2];
    self.labelCSVFileStartTime.stringValue = s;
    s = lines[(lines.count - 2)];
    array = [s componentsSeparatedByString:@","];
    s = array[2];
    self.labelCSVFileEndTime.stringValue = s;
}

- (void)csvDataRead {
    NSInteger value[16];
    NSString *row = csvArray[csvCnt];
    NSArray *array = [row componentsSeparatedByString:@","];
    if (csvDataMode == 1) {
        for (int i=0; i<7; i++) {
            value[i] = [array[i+3] intValue];
        }
        value[7] = value[3] - value[4]; // Vh
        value[8] = (0 - ((value[3] + value[4]) / 2)); // Vv
        [self accelGraphUpdate:value[0]:value[1]:value[2]];
        [self eogGraphUpdate:value[3]:value[4]:value[7]:value[8]];
    }
    else if (csvDataMode == 2) {
        for (int i=0; i<8; i++) {
            value[i] = [array[i+3] intValue];
        }
        value[8] = value[6] - value[7]; // Vh
        value[9] = (0 - ((value[6] + value[7]) / 2)); // Vv
        [self accelGraphUpdate:value[0]:value[1]:value[2]];
        [self gyroGraphUpdate:value[3]:value[4]:value[5]];
        [self eogGraphUpdate:value[6]:value[7]:value[8]:value[9]];
    }
    else {
    }
    csvCnt++;
    if (csvCnt >= (csvArray.count - 1)) {
        [csvTimer invalidate];
        [self.buttonFileReadStart setHidden:NO];
        [self.buttonFileReadPause setHidden:YES];
        [self.buttonFileReadStop setHidden:YES];
    }
}


// =============================================================================
#pragma mark TCP/IP Data Send

- (void)checkIPAddress {
    NSHost *local = [NSHost currentHost];
    NSArray *addrs = [local addresses];
    for (int i=0; i<[addrs count]; i++) {
        NSString* addr = [addrs objectAtIndex:i];
        NSRange range = [addr rangeOfString:@"en0"];
        if (range.location != NSNotFound) {
            addr = [addrs objectAtIndex:(i+1)];
            self.labelIPAddress.stringValue = addr;
            break;
        }
    }
}

- (void)tcpThread {
    NSOperationQueue *queue = [[NSOperationQueue alloc] init];
    
    [queue addOperationWithBlock:^{
        [self socketOpen];
        self.labelTcpStatus.stringValue = @"Connect";
    }];
}

- (void)socketOpen {
    int sock0;
    struct sockaddr_in addr;
    struct sockaddr_in client;
    socklen_t len;
    
    NSString* portNo = self.labelPortNo.stringValue;
    int port = [portNo intValue];
    
    sock0 = socket(AF_INET, SOCK_STREAM, 0);
    addr.sin_family = AF_INET;
    addr.sin_port = htons(port);
    addr.sin_addr.s_addr = INADDR_ANY;
    addr.sin_len = sizeof(addr);
    
    bind(sock0, (struct sockaddr *)&addr, sizeof(addr));
    listen(sock0, 5);
    len = sizeof(client);
    tcpSock = accept(sock0, (struct sockaddr *)&client, &len);
}

- (void)socketWrite:(char *)buf {
    if (tcpSock != 0) {
        write(tcpSock, buf, (int)strlen(buf));
    }
}

@end
