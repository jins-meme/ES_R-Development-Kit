//
//  TCPSocket.h
//  MEME_Academic
//
//  Created by Celleus on 2022/09/13.
//  Copyright © 2022 jins-jp. All rights reserved.
//

#import <Foundation/Foundation.h>

@import CocoaAsyncSocket;
#import "GCDAsyncSocket.h" // for TCP
//#import "GCDAsyncUdpSocket.h" // for UDP

NS_ASSUME_NONNULL_BEGIN

@protocol tcpSocketDelegate<NSObject>

- (void)didAccept;
- (void)socketDidDisconnect:(GCDAsyncSocket *)sock withError:(NSError *)err ;

@end

@interface TCPSocket : NSObject <GCDAsyncSocketDelegate>

@property (nonatomic,strong) GCDAsyncSocket *socket;
@property (nonatomic,strong) NSString *headerString;

@property (nonatomic, assign) id<tcpSocketDelegate> delegate;

- (NSString *)start;
- (void)writeData:(NSString *)string;
- (void)writeHeader;
- (void)stop;
- (bool)isConnected;

@end

NS_ASSUME_NONNULL_END
