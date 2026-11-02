import 'dart:io';

import 'package:crypto/crypto.dart';
import 'package:package_info_plus/package_info_plus.dart';

import 'app_update.dart';
import 'app_update_client.dart';
import 'app_update_installer.dart';

/// Сравнивает сборку с нодой, сверяет сумму файла и открывает установщик.
class AppUpdateFlow {
  AppUpdateFlow({
    AppUpdateClient? client,
    AppUpdateInstaller? installer,
    this.currentBuildNumber,
  })  : _client = client ?? AppUpdateClient(),
        _installer = installer ?? AppUpdateInstaller();

  final AppUpdateClient _client;
  final AppUpdateInstaller _installer;
  final String? currentBuildNumber;

  Future<AppUpdate?> check(String baseUrl) async {
    final address = baseUrl.trim();
    if (address.isEmpty) {
      return null;
    }

    final abi = await _installer.abi();
    final remote = await _client.latest(baseUrl: address, abi: abi);
    if (remote == null) {
      return null;
    }

    final current = int.tryParse(currentBuildNumber ?? await _buildNumber()) ?? 0;
    if (!isNewerVersion(current, remote.versionCode)) {
      return null;
    }

    return remote;
  }

  Future<void> install(String baseUrl, AppUpdate update) async {
    final directory = await _installer.updatesDirectory();
    final file = await _client.download(
      baseUrl: baseUrl.trim(),
      update: update,
      directory: directory,
    );
    final actual = await _sha256(file);
    if (actual != update.sha256) {
      await file.delete();
      throw Exception('Контрольная сумма APK не совпала');
    }

    await _installer.install(file.path);
  }

  Future<String> _buildNumber() async {
    final info = await PackageInfo.fromPlatform();
    return info.buildNumber;
  }

  Future<String> _sha256(File file) async {
    final digest = await sha256.bind(file.openRead()).first;
    return digest.toString();
  }
}
