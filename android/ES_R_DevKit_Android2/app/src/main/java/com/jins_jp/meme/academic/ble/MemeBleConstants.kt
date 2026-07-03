package com.jins_jp.meme.academic.ble

import java.util.UUID

object MemeBleConstants {
    val SERVICE_UUID: UUID = UUID.fromString("D6F25BD1-5B54-4360-96D8-7AA62E04C7EF")
    val RX_CHAR_UUID: UUID = UUID.fromString("D6F25BD4-5B54-4360-96D8-7AA62E04C7EF")
    val TX_CHAR_UUID: UUID = UUID.fromString("D6F25BD2-5B54-4360-96D8-7AA62E04C7EF")
    val CCCD_UUID: UUID = UUID.fromString("00002902-0000-1000-8000-00805f9b34fb")

    /**
     * 接続済み／ボンディング済み端末を MEME と判定するためのデバイス名パターン。
     * スキャンはサービス UUID フィルタで確実に判定できるが、既に接続済みの端末は広告を
     * 出しておらず、かつ BLE の GATT サービス UUID は device.uuids(SDP キャッシュ)には
     * 基本載らないため、公式 SDK 同様に「名前」で判定する。ES_R 実機名は "ESRG2_0"〜
     * "ESRG2_5"(JINS MEME SDK の "JINSG2_[0-5]" に対応する ES_R 版)。
     */
    val DEVICE_NAME_REGEX = Regex("ESRG2_[0-5]$")

    const val SCAN_TIMEOUT_MS = 8_000L

    // Protocol opcodes
    const val DATA_LENGTH: Byte = 0x14
    const val ADN_START_STOP_SEND: Byte = 0xA0.toByte()
    const val ADN_GET_DEV_INFO: Byte = 0xA1.toByte()
    const val ADN_GET_MODE: Byte = 0xA3.toByte()
    const val ADN_SET_MODE: Byte = 0xA4.toByte()
    const val ADN_CLR_PARAMS: Byte = 0xA8.toByte()
    const val ADN_GET_6AXIS_PARAMS: Byte = 0xA9.toByte()
    const val ADN_SET_6AXIS_PARAMS: Byte = 0xAA.toByte()

    const val AUP_REPORT_DEV_INFO: Byte = 0x81.toByte()
    const val AUP_REPORT_MODE: Byte = 0x83.toByte()
    const val AUP_REPORT_6AXIS_PARAMS: Byte = 0x89.toByte()
    const val AUP_REPORT_RESP: Byte = 0x8F.toByte()
    const val AUP_REPORT_ACADEMIA1: Byte = 0x98.toByte()
    const val AUP_REPORT_ACADEMIA2: Byte = 0x99.toByte()
    const val AUP_REPORT_ACADEMIA3: Byte = 0x9A.toByte()
}
