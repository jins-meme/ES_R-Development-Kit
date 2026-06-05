//
//  DecEnc.swift
//  MEME_Academic
//
//  Created by JINS ASSIST開発実機検証  on 2026/06/02.
//  Copyright © 2026 jins-jp. All rights reserved.
//

import Foundation

class DecEnc: NSObject {
    private static let key: [UInt8] = [
        0x39, 0xCC, 0x6D, 0xAB, 0x9E, 0x07, 0x1A, 0xDE, 0x67,
        0x49, 0x71, 0x9A, 0x5B, 0x69, 0x0F, 0x17, 0xC9, 0xB1
    ]
    
    class func encode(_ buf: UnsafeMutablePointer<UInt8>) {
        let keyLen = key.count
        let header0 = buf[0]
        let header1 = buf[1]
        
        for i in 0..<keyLen {
            let v = buf[2 + i]
            // オーバーフローを許容した加算
            let enc = (v ^ key[i]) &+ UInt8(i)
            buf[2 + i] = enc & 0xFF
        }
        buf[0] = header0
        buf[1] = header1
    }
    
    class func decode(_ buf: UnsafeMutablePointer<UInt8>) {
        let keyLen = key.count
        let header0 = buf[0]
        let header1 = buf[1]
        
        for i in 0..<keyLen {
            let v = buf[2 + i]
            // オーバーフローを許容した減算
            let subVal = v &- UInt8(i)
            let dec = subVal ^ key[i]
            buf[2 + i] = dec & 0xFF
        }
        buf[0] = header0
        buf[1] = header1
    }
}
