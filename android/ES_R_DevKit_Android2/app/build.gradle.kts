import java.util.Properties

plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.plugin.compose")
    // core の SettingsDialog "OSS Licenses" が表示するライセンス一覧データを生成する。
    id("com.google.android.gms.oss-licenses-plugin")
}

// 本番リリース署名情報は local.properties (gitignore対象) か環境変数から読む。
// どちらにも無ければ null のまま → release は debug キー署名にフォールバックする。
val localProperties = Properties().apply {
    val localPropertiesFile = rootProject.file("local.properties")
    if (localPropertiesFile.exists()) {
        localPropertiesFile.inputStream().use { load(it) }
    }
}
fun releaseSigningProp(key: String): String? =
    localProperties.getProperty(key) ?: System.getenv(key)

val releaseStoreFile = releaseSigningProp("RELEASE_STORE_FILE")

android {
    namespace = "com.jins_jp.meme.academic"
    compileSdk = 37

    defaultConfig {
        applicationId = "com.jins_jp.meme.academic"
        minSdk = 31
        targetSdk = 37
        versionCode = 13
        versionName = "3.0.5"
    }

    signingConfigs {
        if (releaseStoreFile != null) {
            create("release") {
                storeFile = file(releaseStoreFile)
                storePassword = releaseSigningProp("RELEASE_STORE_PASSWORD")
                keyAlias = releaseSigningProp("RELEASE_KEY_ALIAS")
                keyPassword = releaseSigningProp("RELEASE_KEY_PASSWORD")
            }
        }
    }

    buildTypes {
        release {
            isMinifyEnabled = true
            isShrinkResources = true
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
            // 本番keystore情報(local.properties/環境変数)があればそれで署名、
            // 無い開発者のマシンでは従来どおり debug キーにフォールバックする。
            signingConfig = if (releaseStoreFile != null) {
                signingConfigs.getByName("release")
            } else {
                signingConfigs.getByName("debug")
            }
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    buildFeatures {
        compose = true
        buildConfig = true
    }

    lint {
        checkReleaseBuilds = false
        abortOnError = false
    }

    packaging {
        resources {
            excludes += setOf(
                "META-INF/DEPENDENCIES", "META-INF/LICENSE", "META-INF/LICENSE.txt",
                "META-INF/license.txt", "META-INF/NOTICE", "META-INF/NOTICE.txt",
                "META-INF/notice.txt", "META-INF/ASL2.0", "META-INF/*.kotlin_module"
            )
        }
    }
}

kotlin {
    compilerOptions {
        jvmTarget.set(org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_17)
    }
}

dependencies {
    implementation(project(":core"))

    val composeBom = platform("androidx.compose:compose-bom:2024.12.01")
    implementation(composeBom)

    implementation("androidx.core:core-ktx:1.19.0")
    implementation("androidx.activity:activity-compose:1.13.0")
    implementation("androidx.lifecycle:lifecycle-runtime-ktx:2.11.0")
    implementation("androidx.lifecycle:lifecycle-viewmodel-ktx:2.11.0")
    implementation("androidx.lifecycle:lifecycle-viewmodel-compose:2.11.0")
    implementation("androidx.lifecycle:lifecycle-runtime-compose:2.11.0")

    implementation("androidx.compose.ui:ui")
    implementation("androidx.compose.ui:ui-tooling-preview")
    implementation("androidx.compose.material3:material3")
    implementation("androidx.compose.material:material-icons-extended")
    implementation("androidx.compose.runtime:runtime")
    implementation("androidx.compose.foundation:foundation")

    implementation("androidx.navigation:navigation-compose:2.9.8")

    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.11.0")

    debugImplementation("androidx.compose.ui:ui-tooling")
    debugImplementation("androidx.compose.ui:ui-test-manifest")

    testImplementation("junit:junit:4.13.2")
}
