//
//  TCPSocket.m
//  MEME_Academic
//
//  Created by Celleus on 2022/09/13.
//  Copyright © 2022 jins-jp. All rights reserved.
//

#import "TCPSocket.h"
#import "UserSetting.h"
#import <Network/Network.h>

@interface TCPSocket ()

@property (nonatomic, strong, nullable) nw_listener_t listener;
@property (nonatomic, strong, nullable) nw_connection_t connection;
@property (nonatomic, strong) dispatch_queue_t queue;
@property (nonatomic, assign) BOOL connected;
@property (nonatomic, assign) BOOL manuallyStopped;

- (void)startConnection:(nw_connection_t)connection API_AVAILABLE(macos(10.14));
- (void)sendData:(NSData *)data API_AVAILABLE(macos(10.14));
- (void)receiveDataFromConnection:(nw_connection_t)connection API_AVAILABLE(macos(10.14));
- (void)notifyDisconnectWithError:(nullable NSError *)error API_AVAILABLE(macos(10.14));
- (nullable NSError *)errorFromNWError:(nullable nw_error_t)error API_AVAILABLE(macos(10.14));

@end

@implementation TCPSocket

- (instancetype)init {
    self = [super init];
    if (self) {
        _queue = dispatch_get_main_queue();
    }
    return self;
}

- (NSString *)start {
    [self stop];
    self.manuallyStopped = NO;

    if (@available(macOS 10.14, *)) {
        uint16_t port = [[UserSetting getLocalPort] intValue];
        NSLog(@"portport:%d",port);

        nw_parameters_t parameters = nw_parameters_create_secure_tcp(NW_PARAMETERS_DISABLE_PROTOCOL,
                                                                     NW_PARAMETERS_DEFAULT_CONFIGURATION);
        NSString *portString = [NSString stringWithFormat:@"%hu", port];
        self.listener = nw_listener_create_with_port(portString.UTF8String, parameters);

        if (self.listener == nil) {
            NSLog(@"Error in nw_listener_create_with_port");
            return @"Listen Error";
        }

        __weak typeof(self) weakSelf = self;
        nw_listener_set_queue(self.listener, self.queue);
        nw_listener_set_state_changed_handler(self.listener, ^(nw_listener_state_t state, nw_error_t  _Nullable error) {
            __strong typeof(weakSelf) self = weakSelf;
            if (self == nil) {
                return;
            }

            if (state == nw_listener_state_failed) {
                NSLog(@"nw_listener_state_failed: %@", [self errorFromNWError:error]);
                [self notifyDisconnectWithError:[self errorFromNWError:error]];
            }
        });

        nw_listener_set_new_connection_handler(self.listener, ^(nw_connection_t newConnection) {
            __strong typeof(weakSelf) self = weakSelf;
            if (self == nil) {
                nw_connection_cancel(newConnection);
                return;
            }

            if (self.connection != nil) {
                nw_connection_cancel(newConnection);
                return;
            }

            self.connection = newConnection;
            [self startConnection:newConnection];

            nw_listener_cancel(self.listener);
            self.listener = nil;
        });

        nw_listener_start(self.listener);
        return @"Listen";
    } else {
        NSLog(@"Network.framework requires macOS 10.14 or newer");
        return @"Listen Error";
    }
}

- (void)writeData:(NSString *)string {
    NSData *data = [string dataUsingEncoding:NSUTF8StringEncoding];
    if (@available(macOS 10.14, *)) {
        [self sendData:data];
    }
}

- (void)writeHeader {
    NSData *writeData = [self.headerString dataUsingEncoding:NSUTF8StringEncoding];
    if (@available(macOS 10.14, *)) {
        [self sendData:writeData];
    }
}

- (void)stop {
    self.manuallyStopped = YES;
    self.connected = NO;

    if (@available(macOS 10.14, *)) {
        if (self.listener != nil) {
            nw_listener_cancel(self.listener);
            self.listener = nil;
        }

        if (self.connection != nil) {
            nw_connection_cancel(self.connection);
            self.connection = nil;
        }
    }
}

- (bool)isConnected {
    return self.connected;
}

- (void)startConnection:(nw_connection_t)connection {
    __weak typeof(self) weakSelf = self;

    nw_connection_set_queue(connection, self.queue);
    nw_connection_set_state_changed_handler(connection, ^(nw_connection_state_t state, nw_error_t  _Nullable error) {
        __strong typeof(weakSelf) self = weakSelf;
        if (self == nil || self.connection != connection) {
            return;
        }

        switch (state) {
            case nw_connection_state_ready:
                NSLog(@"Accepted new Network.framework connection");
                self.connected = YES;
                if (self.delegate) {
                    [self.delegate didAccept];
                }
                [self writeHeader];
                [self receiveDataFromConnection:connection];
                break;

            case nw_connection_state_failed:
                NSLog(@"nw_connection_state_failed: %@", [self errorFromNWError:error]);
                [self notifyDisconnectWithError:[self errorFromNWError:error]];
                break;

            case nw_connection_state_cancelled:
                [self notifyDisconnectWithError:nil];
                break;

            default:
                break;
        }
    });

    nw_connection_start(connection);
}

- (void)sendData:(NSData *)data {
    if (self.connection == nil || data.length == 0) {
        return;
    }

    NSData *dataCopy = [data copy];
    dispatch_data_t dispatchData = dispatch_data_create(dataCopy.bytes, dataCopy.length, self.queue, ^{
        (void)dataCopy;
    });

    __weak typeof(self) weakSelf = self;
    nw_connection_send(self.connection,
                       dispatchData,
                       NW_CONNECTION_DEFAULT_MESSAGE_CONTEXT,
                       true,
                       ^(nw_error_t  _Nullable error) {
        __strong typeof(weakSelf) self = weakSelf;
        if (self == nil || error == nil) {
            return;
        }

        NSLog(@"nw_connection_send error: %@", [self errorFromNWError:error]);
        [self notifyDisconnectWithError:[self errorFromNWError:error]];
    });
}

- (void)receiveDataFromConnection:(nw_connection_t)connection {
    __weak typeof(self) weakSelf = self;
    nw_connection_receive(connection, 1, 65536, ^(dispatch_data_t  _Nullable content,
                                                 nw_content_context_t  _Nullable context,
                                                 bool isComplete,
                                                 nw_error_t  _Nullable error) {
        __strong typeof(weakSelf) self = weakSelf;
        if (self == nil || self.connection != connection) {
            return;
        }

        if (error != nil) {
            NSLog(@"nw_connection_receive error: %@", [self errorFromNWError:error]);
            [self notifyDisconnectWithError:[self errorFromNWError:error]];
            return;
        }

        if (isComplete) {
            [self notifyDisconnectWithError:nil];
            return;
        }

        [self receiveDataFromConnection:connection];
    });
}

- (void)notifyDisconnectWithError:(NSError *)error {
    if (self.manuallyStopped) {
        return;
    }

    self.connected = NO;

    if (self.listener != nil) {
        nw_listener_cancel(self.listener);
        self.listener = nil;
    }

    if (self.connection != nil) {
        nw_connection_cancel(self.connection);
        self.connection = nil;
    }

    if (self.delegate) {
        [self.delegate socketDidDisconnectWithError:error];
    }
}

- (NSError *)errorFromNWError:(nw_error_t)error {
    if (error == nil) {
        return nil;
    }

    nw_error_domain_t domain = nw_error_get_error_domain(error);
    int code = nw_error_get_error_code(error);
    NSString *errorDomain = @"Network.framework";

    if (domain == nw_error_domain_posix) {
        errorDomain = NSPOSIXErrorDomain;
    } else if (domain == nw_error_domain_dns) {
        errorDomain = @"Network.framework.DNS";
    } else if (domain == nw_error_domain_tls) {
        errorDomain = @"Network.framework.TLS";
    }

    return [NSError errorWithDomain:errorDomain code:code userInfo:nil];
}

@end
