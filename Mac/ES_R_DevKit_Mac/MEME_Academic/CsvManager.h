//
//  CsvManager.h
//  MEME_Academic
//
//  Created by Celleus on 2022/09/06.
//  Copyright © 2022 jins-jp. All rights reserved.
//

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@interface CsvManager : NSObject

@property (nonatomic) NSString *saveDirectoryPath;
@property (nonatomic) NSString *saveFilePath;
@property (nonatomic) NSString *saveFileName;
@property (nonatomic) Boolean isSave;

- (void)reset;
- (BOOL)createWithDirectoryPath:(NSString *)directoryPath fileName:(NSString *)fileName firstData:(NSData *)firstData;
- (void)appendData:(NSData *)appendData;

@end

NS_ASSUME_NONNULL_END
