import 'dart:io';

import 'package:flutter/services.dart';

import 'main_database.dart';

/// Копия базы документов для передачи: WAL сливается, файл уходит в системное «Поделиться».
class DocumentDatabaseExport {
  DocumentDatabaseExport({
    MainDatabase? database,
    Future<String> Function()? directory,
    Future<void> Function(List<String> paths)? share,
  })  : _database = database ?? MainDatabase(),
        _directory = directory ?? _exportsDirectory,
        _share = share ?? _shareFiles;

  static const channel = MethodChannel('ru.fmuapi.tsd/export');

  final MainDatabase _database;
  final Future<String> Function() _directory;
  final Future<void> Function(List<String> paths) _share;

  /// `null` — база документов ещё не создавалась, копировать нечего.
  Future<String?> shareDatabase() async {
    final source = File(await _database.filePath());
    if (!await source.exists()) {
      return null;
    }

    await _checkpoint();
    final directory = Directory(await _directory());
    await directory.create(recursive: true);
    final copy = await source.copy('${directory.path}/main.db');

    final files = <String>[copy.path];
    final wal = File('${source.path}-wal');
    if (await wal.exists() && await wal.length() > 0) {
      files.add((await wal.copy('${directory.path}/main.db-wal')).path);
    }

    await _share(files);
    return copy.path;
  }

  /// Сливает WAL в файл базы: иначе свежие документы остались бы в `-wal`.
  Future<void> _checkpoint() async {
    try {
      final database = await _database.open();
      await database.rawQuery('PRAGMA wal_checkpoint(TRUNCATE)');
    } catch (_) {
      return;
    }
  }

  static Future<String> _exportsDirectory() async {
    final path = await channel.invokeMethod<String>('exportsDirectory');
    if (path == null || path.isEmpty) {
      throw Exception('Не удалось получить каталог для копии базы');
    }
    return path;
  }

  static Future<void> _shareFiles(List<String> paths) {
    return channel.invokeMethod<void>('share', {
      'paths': paths,
      'subject': 'База документов ТСД',
      'type': 'application/octet-stream',
    });
  }
}
