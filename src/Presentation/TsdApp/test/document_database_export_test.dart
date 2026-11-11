import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';
import 'package:tsd_app/domain/documents/document_packet.dart';
import 'package:tsd_app/infrastructure/documents/document_database_export.dart';
import 'package:tsd_app/infrastructure/documents/main_database.dart';
import 'package:tsd_app/infrastructure/documents/sqlite_document_store.dart';

import 'documents_support.dart';

void main() {
  sqfliteFfiInit();

  late Directory temp;
  late Database database;
  late SqliteDocumentStore store;
  late List<String> shared;

  setUp(() async {
    temp = Directory.systemTemp.createTempSync('main_db_export');
    final main = MainDatabase(factory: databaseFactoryFfi, path: '${temp.path}/main.db');
    database = await main.open();
    store = SqliteDocumentStore(open: () async => database);
    shared = <String>[];
  });

  tearDown(() async {
    await database.close();
    temp.deleteSync(recursive: true);
  });

  test('копия базы открывается и содержит документы', () async {
    await store.save(parseDocumentPacket(sampleDocumentResponse)!);

    final export = DocumentDatabaseExport(
      database: MainDatabase(factory: databaseFactoryFfi, path: '${temp.path}/main.db'),
      directory: () async => '${temp.path}/exports',
      share: (paths) async => shared.addAll(paths),
    );

    final path = await export.shareDatabase();

    expect(path, isNotNull);
    expect(File(path!).existsSync(), isTrue);
    expect(shared, contains(path));

    final copy = await databaseFactoryFfi.openDatabase(path);
    final rows = await copy.query('documents');
    expect(rows, hasLength(1));
    expect(rows.single['presentation'], samplePresentation);
    await copy.close();
  });

  test('без файла базы копировать нечего', () async {
    final export = DocumentDatabaseExport(
      database: MainDatabase(factory: databaseFactoryFfi, path: '${temp.path}/none.db'),
      directory: () async => '${temp.path}/exports',
      share: (paths) async => shared.addAll(paths),
    );

    expect(await export.shareDatabase(), isNull);
    expect(shared, isEmpty);
  });
}
