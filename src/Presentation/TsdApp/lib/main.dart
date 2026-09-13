import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import 'presentation/home/home_screen.dart';
import 'theme/webix_dark_theme.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
    statusBarColor: WebixDarkTheme.surface,
    statusBarIconBrightness: Brightness.light,
    systemNavigationBarColor: WebixDarkTheme.background,
  ));
  runApp(const TsdApp());
}

/// Клиент проверки марки для ТСД.
class TsdApp extends StatelessWidget {
  const TsdApp({super.key, this.home = const HomeScreen()});

  final Widget home;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Fmu-Api: Проверка марки',
      debugShowCheckedModeBanner: false,
      theme: WebixDarkTheme.data(),
      home: home,
    );
  }
}
