//
//  TCPSocket.m
//  MEME_Academic
//
//  Created by Celleus on 2022/09/13.
//  Copyright © 2022 jins-jp. All rights reserved.
//

#import "TCPSocket.h"
#import "UserSetting.h"

@implementation TCPSocket

- (NSString *)start {
    self.socket = [[GCDAsyncSocket alloc] initWithDelegate:self delegateQueue:dispatch_get_main_queue()];
//    NSString *host = @"192.168.0.12";
    uint16_t port = [[UserSetting getLocalPort] intValue];
    NSLog(@"portport:%d",port);
    NSError *error = nil;
    
    if (![self.socket acceptOnPort:port error:&error]){
        NSLog(@"Error in acceptOnPort:error: -> %@", error);
        return @"Listen Error";
    }
    else {
        return @"Listen";
    }
}

- (void)writeData:(NSString *)string {
    NSData *data = [string dataUsingEncoding:NSUTF8StringEncoding];
    [self.socket writeData:data withTimeout:-1 tag:0];
}

- (void)writeHeader {
    NSData *writeData = [self.headerString dataUsingEncoding:NSUTF8StringEncoding];
    [self.socket writeData:writeData withTimeout:-1 tag:1];
}

- (void)stop {
    [self.socket setDelegate:nil delegateQueue:NULL];
    [self.socket disconnect];
    self.socket = nil;
}

- (bool)isConnected {
    return [self.socket isConnected];
}

- (void)socket:(GCDAsyncSocket *)sock didAcceptNewSocket:(GCDAsyncSocket *)newSocket {
    NSLog(@"Accepted new socket from %@:%hu", [newSocket connectedHost], [newSocket connectedPort]);

    if (self.delegate) {
        [self.delegate didAccept];
    }
    
    self.socket = newSocket;
    [self writeHeader];
}

- (void)socketDidDisconnect:(GCDAsyncSocket *)sock withError:(NSError *)err {
    if (self.delegate) {
        [self.delegate socketDidDisconnect:(GCDAsyncSocket *)sock withError:(NSError *)err ];
    }
}

- (void)socket:(GCDAsyncSocket *)sock didConnectToHost:(NSString *)host port:(UInt16)port {
    NSLog(@"socket:didConnectToHost:%@ port:%hu", host, port);
//    NSString *requestStrFrmt = @"HEAD / HTTP/1.0\r\nHost: %@\r\n\r\n";
//    NSString *requestStr = [NSString stringWithFormat:requestStrFrmt, host];
//    NSData *requestData = [requestStr dataUsingEncoding:NSUTF8StringEncoding];
//    [socket writeData:requestData withTimeout:-1.0 tag:0];
//    NSLog(@"Sending HTTP Request:\n%@", requestStr);
//    [socket readDataToData:[GCDAsyncSocket CRLFData] withTimeout:-1.0 tag:0];
}
 
 
- (void)socket:(GCDAsyncSocket *)sock didWriteDataWithTag:(long)tag {
    NSLog(@"socket:didWriteDataWithTag:");
}
 
 
- (void)socket:(GCDAsyncSocket *)sock didReadData:(NSData *)data withTag:(long)tag {
    NSLog(@"socket:didReadData:withTag:");
//    NSString *httpResponse = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
}

@end
