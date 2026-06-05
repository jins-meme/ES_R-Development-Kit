package com.jins_jp.meme.academic

import android.app.Application
import com.jins_jp.meme.academic.ble.MemeBleRepository

class App : Application() {
    val bleRepository: MemeBleRepository by lazy { MemeBleRepository(this) }

    override fun onCreate() {
        super.onCreate()
        instance = this
    }

    companion object {
        lateinit var instance: App
            private set
    }
}
