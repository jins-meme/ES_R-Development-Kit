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
