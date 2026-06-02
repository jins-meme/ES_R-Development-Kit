//
//  AppDelegate.swift
//  MEME_Academic
//
//  Created by D-CLUE on 2017/03/22.
//  Copyright © 2017年 jins-jp. All rights reserved.
//

import Cocoa

@main
class AppDelegate: NSObject, NSApplicationDelegate {

    func applicationDidFinishLaunching(_ aNotification: Notification) {
    }

    func applicationWillTerminate(_ aNotification: Notification) {
    }

    func applicationShouldHandleReopen(_ sender: NSApplication, hasVisibleWindows flag: Bool) -> Bool {
        NSLog("hasVisibleWindows:%@", flag ? "YES" : "NO")
        for openWindow in sender.windows {
            NSLog("openWindow:%@", openWindow)
            if String(describing: type(of: openWindow)) == "NSWindow" {
                NSLog("is NSWindow")
                openWindow.makeKeyAndOrderFront(self)
            }
        }
        return true
    }
}
