#import <Foundation/Foundation.h>
#import <stdio.h>
#import "DecEnc.h"

int main(int argc, const char * argv[]) {
    @autoreleasepool {
        uint8_t orig[18];
        for (int i = 0; i < 18; ++i) orig[i] = (uint8_t)(i + 1); // sample data

        printf("Original: ");
        for (int i = 0; i < 18; ++i) printf("%02X ", orig[i]);
        printf("\n");

        uint8_t buf[18];
        memcpy(buf, orig, 18);

        [DecEnc Encode:buf];

        printf("Encoded : ");
        for (int i = 0; i < 18; ++i) printf("%02X ", buf[i]);
        printf("\n");

        [DecEnc Decode:buf];

        printf("Decoded : ");
        for (int i = 0; i < 18; ++i) printf("%02X ", buf[i]);
        printf("\n");

        // verify
        int ok = 1;
        for (int i = 0; i < 18; ++i) if (buf[i] != orig[i]) { ok = 0; break; }
        printf("Verify  : %s\n", ok ? "OK" : "FAIL");
    }
    return 0;
}
