//
//  CsvManager.m
//  MEME_Academic
//
//  Created by Celleus on 2022/09/06.
//  Copyright © 2022 jins-jp. All rights reserved.
//

#import "CsvManager.h"

@implementation CsvManager

- (void)reset {
    self.isSave = NO;
    self.saveDirectoryPath = nil;
    self.saveFilePath = nil;
    self.saveFileName = nil;
}

- (BOOL)createWithDirectoryPath:(NSString *)directoryPath fileName:(NSString *)fileName firstData:(NSData *)firstData {
    self.saveDirectoryPath = directoryPath;
    NSLog(@"saveDirectoryPath:%@",self.saveDirectoryPath);
    self.saveFileName = fileName;
    NSLog(@"saveDirectoryPath:%@",self.saveFileName);
    self.saveFilePath = [directoryPath stringByAppendingPathComponent:fileName];
    NSLog(@"saveFilePath:%@",self.saveFilePath);
    self.isSave = [firstData writeToURL:[NSURL URLWithString:self.saveFilePath]  atomically:YES];
    return self.isSave;
}

- (void)appendData:(NSData *)appendData {
    
    NSFileHandle *fileHandle = [NSFileHandle fileHandleForWritingToURL:[NSURL URLWithString:self.saveFilePath] error:nil];
//    NSFileHandle *fileHandle = [NSFileHandle fileHandleForWritingAtPath:self.saveFilePath];
    
    // 書き込み位置をファイルの末尾に設定
    [fileHandle seekToEndOfFile];
    
    // ファイルにデータを書き込む
    [fileHandle writeData:appendData];

}

@end
