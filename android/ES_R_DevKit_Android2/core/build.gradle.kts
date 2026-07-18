plugins {
    id("com.android.library")
    id("org.jetbrains.kotlin.plugin.compose")
}

android {
    namespace = "com.jins_jp.meme.core"
    compileSdk = 37

    defaultConfig {
        minSdk = 31
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    buildFeatures {
        compose = true
    }

    lint {
        checkReleaseBuilds = false
        abortOnError = false
    }
}

kotlin {
    compilerOptions {
        jvmTarget.set(org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_17)
    }
}

dependencies {
    // BLE パケットの復号 jar は core が同梱する。アプリ側からも
    // DataEncryption.encode を直接呼ぶため api で公開する。
    api(fileTree("libs") { include("*.jar") })

    val composeBom = platform("androidx.compose:compose-bom:2024.12.01")
    implementation(composeBom)

    implementation("androidx.core:core-ktx:1.19.0")
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.11.0")

    // 共有 MainViewModel / MainScreen (core.ui.main) が使う。
    implementation("androidx.activity:activity-compose:1.13.0")
    implementation("androidx.lifecycle:lifecycle-viewmodel-ktx:2.11.0")
    implementation("androidx.lifecycle:lifecycle-viewmodel-compose:2.11.0")
    implementation("androidx.lifecycle:lifecycle-runtime-compose:2.11.0")

    implementation("androidx.compose.ui:ui")
    implementation("androidx.compose.material3:material3")
    implementation("androidx.compose.material:material-icons-extended")
    implementation("androidx.compose.runtime:runtime")
    implementation("androidx.compose.foundation:foundation")

    // OSS ライセンス一覧ビューア (SettingsDialog の "OSS Licenses" から起動する
    // OssLicensesMenuActivity)。ライセンスデータ本体は各アプリ側で
    // oss-licenses-plugin が生成し、この Activity が読み込む。
    implementation("com.google.android.gms:play-services-oss-licenses:17.1.0")
    // OssLicensesMenuActivity の親クラス AppCompatActivity を参照するために必要。
    implementation("androidx.appcompat:appcompat:1.7.0")

    testImplementation("junit:junit:4.13.2")
    testImplementation("org.jetbrains.kotlinx:kotlinx-coroutines-test:1.11.0")
    // PlaybackController.start(uri) へ渡す android.net.Uri のインスタンス生成にのみ使う。
    testImplementation("org.mockito:mockito-core:5.14.2")
}
