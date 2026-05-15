//
//  DecEnc.m
//  DecEnc
//
//  Created by GitHub Copilot on 2026/05/15.
//

#import "DecEnc.h"

@implementation DecEnc

+ (void)Encode:(uint8_t *)buf {
    const uint8_t key[18] = {0x39,0xCC,0x6D,0xAB,0x9E,0x07,0x1A,0xDE,0x67,0x49,0x71,0x9A,0x5B,0x69,0x0F,0x17,0xC9,0xB1};
    const size_t keyLen = sizeof(key) / sizeof(key[0]);
    for (size_t i = 0; i < keyLen; ++i) {
        uint8_t v = buf[i];
        uint8_t enc = (uint8_t)(0xFF & ((v ^ key[i]) + (uint8_t)i));
        buf[i] = enc;
    }
}

+ (void)Decode:(uint8_t *)buf {
    const uint8_t key[18] = {0x39,0xCC,0x6D,0xAB,0x9E,0x07,0x1A,0xDE,0x67,0x49,0x71,0x9A,0x5B,0x69,0x0F,0x17,0xC9,0xB1};
    const size_t keyLen = sizeof(key) / sizeof(key[0]);
    for (size_t i = 0; i < keyLen; ++i) {
        uint8_t v = buf[i];
        uint8_t dec = (uint8_t)(0xFF & (((uint8_t)(v - (uint8_t)i)) ^ key[i]));
        buf[i] = dec;
    }
}

@end
