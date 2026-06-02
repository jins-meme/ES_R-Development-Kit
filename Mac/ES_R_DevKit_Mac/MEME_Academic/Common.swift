//
//  Common.swift
//  MEME_Academic
//
//  Created by Celleus on 2022/09/05.
//  Copyright © 2022 jins-jp. All rights reserved.
//

import Foundation
import Darwin

class Common: NSObject {

    class func setUserDefaults(_ value: Any?, forKey key: String, appGroups: String? = nil) {
        let userDefaults: UserDefaults
        if let appGroups = appGroups {
            userDefaults = UserDefaults(suiteName: appGroups) ?? UserDefaults.standard
        } else {
            userDefaults = UserDefaults.standard
        }

        if let value = value {
            do {
                let data = try NSKeyedArchiver.archivedData(withRootObject: value, requiringSecureCoding: false)
                userDefaults.set(data, forKey: key)
            } catch {
                NSLog("UserDefaults archive error: %@", error.localizedDescription)
                return
            }
        } else {
            userDefaults.removeObject(forKey: key)
        }

        userDefaults.synchronize()
    }

    class func getUserDefaults(forKey key: String, appGroups: String? = nil) -> Any? {
        let userDefaults: UserDefaults
        if let appGroups = appGroups {
            userDefaults = UserDefaults(suiteName: appGroups) ?? UserDefaults.standard
        } else {
            userDefaults = UserDefaults.standard
        }

        guard let data = userDefaults.object(forKey: key) as? Data else {
            return nil
        }

        let classes: [AnyClass] = [
            NSString.self, NSNumber.self, NSData.self, NSDate.self,
            NSArray.self, NSDictionary.self, NSMutableArray.self, NSMutableDictionary.self
        ]
        do {
            return try NSKeyedUnarchiver.unarchivedObject(ofClasses: classes, from: data)
        } catch {
            NSLog("UserDefaults unarchive error: %@", error.localizedDescription)
            return nil
        }
    }

    class func getIPAddress() -> String {
        var addresses: [String] = []
        var interfaces: UnsafeMutablePointer<ifaddrs>?

        guard getifaddrs(&interfaces) == 0, let firstAddr = interfaces else {
            return ""
        }

        var ptr: UnsafeMutablePointer<ifaddrs>? = firstAddr
        while ptr != nil {
            guard let cur = ptr?.pointee else { break }
            if let sa = cur.ifa_addr, sa.pointee.sa_family == UInt8(AF_INET) {
                var hostname = [CChar](repeating: 0, count: Int(NI_MAXHOST))
                if getnameinfo(sa, socklen_t(cur.ifa_addr.pointee.sa_len),
                               &hostname, socklen_t(hostname.count),
                               nil, 0, NI_NUMERICHOST) == 0 {
                    let bytes = hostname.prefix(while: { $0 != 0 }).map { UInt8(bitPattern: $0) }
                    let address = String(decoding: bytes, as: UTF8.self)
                    NSLog("address:%@", address)
                    if !address.isEmpty && address != "127.0.0.1" {
                        addresses.append(address)
                    }
                }
            }
            ptr = cur.ifa_next
        }
        freeifaddrs(interfaces)

        return addresses.first ?? ""
    }
}
