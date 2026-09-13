# ТСД: проверка марки

Клиент `Fmu-Api` для Android ТСД. Главный экран без поля ввода: клавиатура (окончание CR `#13`, GS остаётся в коде), буфер обмена и broadcast-интент.

```bat
set PATH=D:\sdk\flutter\bin;%PATH%
cd src\Presentation\TsdApp
flutter test
flutter build apk
```

minSdk 24 (Android 7). 32-битный APK: `flutter build apk --release --target-platform android-arm`
