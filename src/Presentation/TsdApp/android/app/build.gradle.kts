plugins {
    id("com.android.application")
    // The Flutter Gradle Plugin must be applied after the Android and Kotlin Gradle plugins.
    id("dev.flutter.flutter-gradle-plugin")
}

android {
    namespace = "ru.fmuapi.tsd_app"
    compileSdk = flutter.compileSdkVersion

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    defaultConfig {
        applicationId = "ru.fmuapi.tsd_app"
        minSdk = 24
        targetSdk = flutter.targetSdkVersion
        versionCode = flutter.versionCode
        versionName = flutter.versionName
    }

    signingConfigs {
        getByName("debug") {
            enableV1Signing = true
            enableV2Signing = true
        }
    }

    buildTypes {
        release {
            signingConfig = signingConfigs.getByName("debug")
        }
    }

    packaging {
        jniLibs {
            useLegacyPackaging = true
        }
    }
}

kotlin {
    compilerOptions {
        jvmTarget = org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_17
    }
}

flutter {
    source = "../.."
}

afterEvaluate {
    tasks.named("assembleRelease") {
        doLast {
            val apk = file("build/outputs/apk/release/app-release.apk")
            if (!apk.exists()) {
                return@doLast
            }

            val script = file("${project.projectDir}/../resign-v1.ps1")
            val process = ProcessBuilder(
                "powershell",
                "-NoProfile",
                "-ExecutionPolicy", "Bypass",
                "-File",
                script.absolutePath,
                apk.absolutePath,
            ).inheritIO().start()
            if (process.waitFor() != 0) {
                throw GradleException("Не удалось переподписать APK схемой v1")
            }
        }
    }
}
