//
//  AppDelegate.m
//  MEME_Academic
//
//  Created by D-CLUE on 2017/03/22.
//  Copyright © 2017年 jins-jp. All rights reserved.
//

#import "AppDelegate.h"

@interface AppDelegate ()

@end

@implementation AppDelegate

- (void)applicationDidFinishLaunching:(NSNotification *)aNotification {
    // Insert code here to initialize your application
}


- (void)applicationWillTerminate:(NSNotification *)aNotification {
    // Insert code here to tear down your application
}

// Dockにあるアイコンをクリックすると呼ばれる
// このアプリではwindowを再表示
- (BOOL)applicationShouldHandleReopen:(NSApplication *)sender hasVisibleWindows:(BOOL)flag {
    NSLog(@"hasVisibleWindows:%@",flag?@"YES":@"NO");
//    if (!flag) {
//        for (NSWindow *openWindow in sender.windows) {
//            [openWindow makeKeyAndOrderFront:self];
//        }
//    }
    // NSComboBoxを1つでも触っているとhasVisibleWindowsがYESになる？
    // NSComboBoxを1回でも触ると、該当のNSComboBoxWindowが再起動時にopenしてしまうので
    // classNameをNSWindowのものだけ開くように修正
    for (NSWindow *openWindow in sender.windows) {
        NSLog(@"openWindow:%@",openWindow);
        if ([[openWindow className] isEqualToString:@"NSWindow"]) {
            NSLog(@"is NSWindow");
            [openWindow makeKeyAndOrderFront:self];
        }
    }
    return YES;
}

@end
