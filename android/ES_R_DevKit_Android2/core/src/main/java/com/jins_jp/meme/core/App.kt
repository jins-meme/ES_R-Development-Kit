package com.jins_jp.meme.core

import android.app.Application
import com.jins_jp.meme.core.ble.MemeBleRepository
import com.jins_jp.meme.core.ui.main.MeasurementSession

class App : Application() {
    val bleRepository: MemeBleRepository by lazy { MemeBleRepository(this) }

    // 計測と画面の状態はプロセスに 1 つ。Activity が破棄されても計測を続けるため、ここで持つ（[MeasurementSession]）。
    // 中で WebView を作るので、最初に触るのはメインスレッド(Activity / サービスのコールバック)に限る。
    val session: MeasurementSession by lazy { MeasurementSession(this, bleRepository) }

    override fun onCreate() {
        super.onCreate()
        instance = this
    }

    companion object {
        lateinit var instance: App
            private set
    }
}
