//
//  ViewController.h
//  JINS_MEME_DataLogger
//
//  Created by RYOSAITO on 2016/06/20.
//  Copyright © 2016年 RYOSAITO. All rights reserved.
//

#import <Cocoa/Cocoa.h>
#import "CorePlot.h"
#import "DecEnc.h"

@interface ViewController : NSViewController<CPTPlotDataSource>
{
    @private
    CPTGraph *gyroGraph;
    CPTGraph *accelGraph;
    CPTGraph *eogGraph;
}

@property (readwrite, nonatomic) NSMutableArray *gyroXPlotData;
@property (readwrite, nonatomic) NSMutableArray *gyroYPlotData;
@property (readwrite, nonatomic) NSMutableArray *gyroZPlotData;
@property (readwrite, nonatomic) NSMutableArray *accelXPlotData;
@property (readwrite, nonatomic) NSMutableArray *accelYPlotData;
@property (readwrite, nonatomic) NSMutableArray *accelZPlotData;
@property (readwrite, nonatomic) NSMutableArray *eogL1PlotData;
@property (readwrite, nonatomic) NSMutableArray *eogR1PlotData;
@property (readwrite, nonatomic) NSMutableArray *eogH1PlotData;
@property (readwrite, nonatomic) NSMutableArray *eogV1PlotData;

@property (weak) IBOutlet NSProgressIndicator *scanIndicator;
@property (weak) IBOutlet NSTabView *tabViewMenu;
@property (weak) IBOutlet NSComboBox *comboBoxMEME;
@property (weak) IBOutlet NSComboBox *comboBoxSelectMode;
@property (weak) IBOutlet NSComboBox *comboBoxTransSpped;
@property (weak) IBOutlet NSComboBox *comboBoxAccelRange;
@property (weak) IBOutlet NSComboBox *comboBoxGyroRange;
@property (weak) IBOutlet NSButton *buttonScanDevice;
@property (weak) IBOutlet NSButton *buttonConnect;
@property (weak) IBOutlet NSButton *buttonInitialize;
@property (weak) IBOutlet NSButton *buttonStartMesure;
@property (weak) IBOutlet NSButton *buttonFreeMarking;
@property (weak) IBOutlet NSTextField *labelConnect;

- (IBAction)buttonScanDeviceTapped:(id)sender;
- (IBAction)buttonConnectTapped:(id)sender;
- (IBAction)buttonInitializeTapped:(id)sender;
- (IBAction)buttonStartMeasureTapped:(id)sender;
- (IBAction)buttonFreeMarkingTapped:(id)sender;

@property (weak) IBOutlet NSComboBox *comboBoxFileLists;
- (IBAction)buttonCheckFileTapped:(id)sender;
- (IBAction)comboBoxFileListsChanged:(id)sender;

@property (weak) IBOutlet NSButton *buttonFileReadStart;
@property (weak) IBOutlet NSButton *buttonFileReadPause;
@property (weak) IBOutlet NSButton *buttonFileReadStop;

- (IBAction)buttonFileReadStartTapped:(id)sender;
- (IBAction)buttonFileReadPauseTapped:(id)sender;
- (IBAction)buttonFileReadStopTapped:(id)sender;

@property (weak) IBOutlet NSTextField *labelCSVFileDataMode;
@property (weak) IBOutlet NSTextField *labelCSVFileTrasSpeed;
@property (weak) IBOutlet NSTextField *labelCSVFileAccRange;
@property (weak) IBOutlet NSTextField *labelCSVFileGyroRange;
@property (weak) IBOutlet NSTextField *labelCSVFileStartTime;
@property (weak) IBOutlet NSTextField *labelCSVFileEndTime;
@property (weak) IBOutlet NSComboBox *comboBoxReplaySpeed;

@property (weak) IBOutlet NSTextField *labelIPAddress;
@property (weak) IBOutlet NSTextField *labelPortNo;
@property (weak) IBOutlet NSTextField *labelTcpStatus;

@property (weak) IBOutlet NSLevelIndicator *levelBattery;
@property (weak) IBOutlet NSTextField *labelMEMEversion;



@end

