import java.util.Properties

plugins {
    id("com.android.application")
    // The Flutter Gradle Plugin must be applied after the Android and Kotlin Gradle plugins.
    id("dev.flutter.flutter-gradle-plugin")
}

val keyPropertiesFile = rootProject.file("key.properties")
val keyProperties = Properties()
if (keyPropertiesFile.exists())
    keyPropertiesFile.inputStream().use { keyProperties.load(it) }

fun requireKeyProperty(name: String): String {
    val value = keyProperties.getProperty(name)?.trim().orEmpty()
    if (value.isEmpty())
        throw GradleException("В android/key.properties не заполнено $name")
    return value
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
        if (keyPropertiesFile.exists()) {
            create("release") {
                keyAlias = requireKeyProperty("keyAlias")
                keyPassword = requireKeyProperty("keyPassword")
                storeFile = file(requireKeyProperty("storeFile"))
                storePassword = requireKeyProperty("storePassword")
                enableV1Signing = true
                enableV2Signing = true
            }
        }
    }

    buildTypes {
        release {
            signingConfig = if (keyPropertiesFile.exists())
                signingConfigs.getByName("release")
            else
                signingConfigs.getByName("debug")
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

gradle.taskGraph.whenReady {
    val releaseRequested = allTasks.any { task -> task.name.contains("Release") }
    if (releaseRequested && !keyPropertiesFile.exists()) {
        throw GradleException(
            "Для release нужен android/key.properties. Образец: android/key.properties.example.")
    }
}
