import 'package:flutter/services.dart';

/// Каталог загрузки, ABI устройства и запуск системного установщика.
class AppUpdateInstaller {
  AppUpdateInstaller({
    this.abiReader,
    this.directoryReader,
    this.installPackage,
  });

  static const channel = MethodChannel('ru.fmuapi.tsd/update');

  final Future<String> Function()? abiReader;
  final Future<String> Function()? directoryReader;
  final Future<void> Function(String path)? installPackage;

  Future<String> abi() {
    final reader = abiReader;
    if (reader != null) {
      return reader();
    }
    return _channelString('abi');
  }

  Future<String> updatesDirectory() {
    final reader = directoryReader;
    if (reader != null) {
      return reader();
    }
    return _channelString('updatesDirectory');
  }

  Future<void> install(String path) {
    final installer = installPackage;
    if (installer != null) {
      return installer(path);
    }
    return channel.invokeMethod<void>('install', {'path': path});
  }

  Future<String> _channelString(String method) async {
    final value = await channel.invokeMethod<String>(method);
    if (value == null || value.isEmpty) {
      throw Exception('Нет ответа установщика');
    }
    return value;
  }
}
