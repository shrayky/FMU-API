import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';
import 'package:tsd_app/domain/documents/document_packet.dart';
import 'package:tsd_app/domain/documents/stored_mark.dart';
import 'package:tsd_app/infrastructure/documents/main_database.dart';
import 'package:tsd_app/infrastructure/documents/sqlite_document_store.dart';

import 'documents_support.dart';

void main() {
  sqfliteFfiInit();

  late Database database;
  late SqliteDocumentStore store;
  late DocumentPacket packet;

  setUp(() async {
    database = await MainDatabase(factory: databaseFactoryFfi, path: inMemoryDatabasePath).open();
    store = SqliteDocumentStore(open: () async => database);
    packet = parseDocumentPacket(sampleDocumentResponse)!;
  });

  tearDown(() => database.close());

  test('пакет из примера вставляет строку документа и обновляет справочники', () async {
    await store.save(packet, scannedAt: DateTime(2026, 11, 5, 10, 30));

    final documents = await store.documents();
    final name = (await store.nomenclatureNames({sampleGoodsUid}))[sampleGoodsUid];

    expect(documents, hasLength(1));
    expect(documents.single.number, '123');
    expect(documents.single.counterparty, 'ООО Ромашка');
    expect(documents.single.presentation, samplePresentation);
    expect(documents.single.erpUid, sampleErpUid);
    expect(name, sampleGoodsName);

    final barcodes = await database.query('barcodes');
    expect(barcodes.single['barcode'], sampleBarcode);
    expect(barcodes.single['nomenclature_uid'], sampleGoodsUid);

    final packs = await database.query('pack_sizes');
    expect(packs.single['nomenclature_uid'], sampleGoodsUid);
    expect(jsonDecode('${packs.single['body']}')['ЕдиницВУпаковке'], 6);
  });

  test('Весовой сохраняется строкой, состав остаётся в теле', () async {
    final saved = await store.save(packet);

    final full = await store.document(saved.id);
    final body = jsonDecode('${(await database.query('documents')).single['body']}') as Map;

    expect(full?.body['Весовой'], 'false');
    expect(body['Весовой'], 'false');
    expect(body['Состав'], isA<List>());
  });

  test('список не читает тело, документ открывается по id', () async {
    final saved = await store.save(packet);

    final listed = (await store.documents()).single;
    final opened = await store.document(saved.id);

    expect(listed.body, isEmpty);
    expect(opened?.body['Состав'], isA<List>());
    expect(await store.document('нет такого'), isNull);
  });

  test('штрихкод справочника ведёт к карточке товара', () async {
    await store.save(packet);

    final found = await store.nomenclatureUidsByBarcodes([sampleBarcode, '0000000000000']);

    expect(found[sampleBarcode], sampleGoodsUid);
    expect(found.containsKey('0000000000000'), isFalse);
    expect(await store.nomenclatureUidsByBarcodes(const []), isEmpty);
  });

  test('повтор пакета добавляет второй документ и не плодит карточку товара', () async {
    await store.save(packet);
    await store.save(packet);

    final documents = await store.documents();
    final cards = await database.query('nomenclature');

    expect(documents, hasLength(2));
    expect(cards, hasLength(1));
  });

  test('свежие документы идут первыми', () async {
    final now = DateTime(2026, 11, 5, 12);

    await store.save(packet, scannedAt: now.subtract(const Duration(days: 2)));
    await store.save(packet, scannedAt: now);

    final documents = await store.documents();

    expect(documents.first.scannedAt, now);
    expect(documents.last.scannedAt, now.subtract(const Duration(days: 2)));
  });

  test('срок 30 дней удаляет документ и не удаляет справочник', () async {
    final now = DateTime.now();

    final old = await store.save(packet, scannedAt: now.subtract(const Duration(days: 40)));
    final fresh = await store.save(packet, scannedAt: now.subtract(const Duration(days: 2)));
    await store.addMark(old.id, const StoredMark(mark: '010460000000001521old', nomenclatureUid: sampleGoodsUid));
    await store.addMark(fresh.id, const StoredMark(mark: '010460000000001521new', nomenclatureUid: sampleGoodsUid));
    await store.trim(days: 30);

    final documents = await store.documents();
    expect(documents, hasLength(1));
    expect(
      documents.single.scannedAt.millisecondsSinceEpoch,
      now.subtract(const Duration(days: 2)).millisecondsSinceEpoch,
    );
    expect(await store.marks(old.id), isEmpty);
    expect(await store.marks(fresh.id), hasLength(1));
    expect((await database.query('nomenclature')), hasLength(1));
    expect((await database.query('barcodes')), hasLength(1));
    expect((await database.query('pack_sizes')), hasLength(1));
  });

  test('срок 0 не удаляет документ', () async {
    await store.save(packet, scannedAt: DateTime.now().subtract(const Duration(days: 400)));
    await store.trim(days: 0);

    expect(await store.documents(), hasLength(1));
  });

  test('поиск идёт по всей таблице без потолка строк', () async {
    for (var index = 0; index < 520; index += 1) {
      await _insert(
        database,
        'документ $index',
        counterparty: 'ООО Ромашка',
        at: DateTime(2026, 11, 5).add(Duration(minutes: index)),
      );
    }
    await _insert(
      database,
      'старый сбор',
      counterparty: 'ИП Петров',
      at: DateTime(2026, 9, 1),
    );

    expect(await store.documents(), hasLength(521));

    final found = await store.documents(query: 'петров');

    expect(found, hasLength(1));
    expect(found.single.presentation, 'старый сбор');
  });

  test('поиск не зависит от регистра, в том числе для кириллицы', () async {
    await store.save(packet);

    expect(await store.documents(query: 'РОМАШКА'), hasLength(1));
    expect(await store.documents(query: 'ромашка'), hasLength(1));
    expect(await store.documents(query: 'СБОР МАРОК'), hasLength(1));
    expect(await store.documents(query: '123'), hasLength(1));
    expect(await store.documents(query: 'петров'), isEmpty);
  });

  test('символы шаблона LIKE ищутся буквально', () async {
    await store.save(packet);

    expect(await store.documents(query: '%'), isEmpty);
    expect(await store.documents(query: '_'), isEmpty);
  });

  test('марки пишутся ключом документа и марки, повтор строку не плодит', () async {
    final saved = await store.save(packet);

    await store.addMark(
      saved.id,
      const StoredMark(mark: '010460000000001521a', nomenclatureUid: sampleGoodsUid, units: 6),
    );
    await store.addMark(
      saved.id,
      const StoredMark(mark: '010460000000001521a', nomenclatureUid: sampleGoodsUid, units: 6),
    );

    final marks = await store.marks(saved.id);
    expect(marks, hasLength(1));
    expect(marks.single.units, 6);
    expect(marks.single.nomenclatureUid, sampleGoodsUid);
  });

  test('удаление марки возвращает признак, а чужая марка его не даёт', () async {
    final saved = await store.save(packet);
    await store.addMark(saved.id, const StoredMark(mark: 'm1', nomenclatureUid: sampleGoodsUid));

    expect(await store.removeMark(saved.id, 'm1'), isTrue);
    expect(await store.removeMark(saved.id, 'm1'), isFalse);
    expect(await store.marks(saved.id), isEmpty);
  });

  test('отметка выгрузки читается журналом', () async {
    final saved = await store.save(packet);
    expect((await store.documents()).single.uploaded, isFalse);

    await store.setUploaded(saved.id, uploaded: true);

    expect((await store.documents()).single.uploaded, isTrue);
    expect((await store.document(saved.id))?.uploaded, isTrue);
  });

  test('палета и упаковка лежат разными строками размера', () async {
    await store.save(
      DocumentPacket(
        document: packet.document,
        packSizes: [
          {'Номенклатура': sampleGoodsUid, 'ЕдиницВУпаковке': 6, 'Паллета': false},
          {'Номенклатура': sampleGoodsUid, 'ЕдиницВУпаковке': 120, 'Паллета': true},
        ],
      ),
    );

    expect(await store.packSizeUnits(sampleGoodsUid, pallet: false), 6);
    expect(await store.packSizeUnits(sampleGoodsUid, pallet: true), 120);
    expect(await store.packSizeUnits('нет такого', pallet: false), isNull);
  });

  test('коэффициент берётся из карточки штрихкода', () async {
    await store.save(
      DocumentPacket(
        document: packet.document,
        barcodes: [
          {'Штрихкод': sampleBarcode, 'Номенклатура': sampleGoodsUid, 'Коэффициент': 12},
        ],
      ),
    );

    expect(await store.barcodeCoefficient(sampleBarcode), 12);
    expect(await store.barcodeCoefficient('0000000000000'), isNull);
  });

  test('количество штрихкодов справочника считается', () async {
    expect(await store.barcodeCount(), 0);

    await store.save(packet);

    expect(await store.barcodeCount(), 1);
  });

  test('в documents нет колонки search, зато есть uploaded', () async {
    final columns = await database.rawQuery('PRAGMA table_info(documents)');
    final names = columns.map((column) => '${column['name']}').toList();

    expect(names, contains('uploaded'));
    expect(names, isNot(contains('search')));
  });

  test('база версии 2 обновляется без потери документов и делит размеры упаковок', () async {
    final path = '${Directory.systemTemp.path}/main_v2_${DateTime.now().microsecondsSinceEpoch}.db';
    final old = await databaseFactoryFfi.openDatabase(
      path,
      options: OpenDatabaseOptions(
        version: 2,
        onCreate: (db, _) async {
          await db.execute('''
            CREATE TABLE documents (
              id TEXT PRIMARY KEY,
              scanned_at INTEGER NOT NULL,
              number TEXT NOT NULL DEFAULT '',
              counterparty TEXT NOT NULL DEFAULT '',
              presentation TEXT NOT NULL DEFAULT '',
              erp_uid TEXT NOT NULL DEFAULT '',
              body TEXT NOT NULL DEFAULT '{}',
              search TEXT NOT NULL DEFAULT ''
            )
          ''');
          await db.execute('CREATE TABLE nomenclature (uid TEXT PRIMARY KEY, body TEXT NOT NULL DEFAULT \'{}\')');
          await db.execute('''
            CREATE TABLE barcodes (
              barcode TEXT PRIMARY KEY,
              nomenclature_uid TEXT NOT NULL DEFAULT '',
              body TEXT NOT NULL DEFAULT '{}'
            )
          ''');
          await db.execute('CREATE TABLE pack_sizes (nomenclature_uid TEXT PRIMARY KEY, body TEXT NOT NULL DEFAULT \'{}\')');
        },
      ),
    );
    await old.insert('documents', {
      'id': 'old-1',
      'scanned_at': DateTime(2026, 11, 5).millisecondsSinceEpoch,
      'presentation': 'Старый сбор',
      'body': '{}',
      'search': 'старый сбор',
    });
    await old.insert('pack_sizes', {
      'nomenclature_uid': sampleGoodsUid,
      'body': jsonEncode({'Номенклатура': sampleGoodsUid, 'ЕдиницВУпаковке': 6}),
    });
    await old.close();

    final upgraded = await MainDatabase(factory: databaseFactoryFfi, path: path).open();
    addTearDown(() async {
      await upgraded.close();
      await File(path).delete();
    });

    final columns = (await upgraded.rawQuery('PRAGMA table_info(documents)'))
        .map((column) => '${column['name']}')
        .toList();
    final documents = await upgraded.query('documents');
    final packs = await upgraded.query('pack_sizes');

    expect(columns, contains('uploaded'));
    expect(columns, isNot(contains('search')));
    expect(documents, hasLength(1));
    expect(documents.single['presentation'], 'Старый сбор');
    expect(documents.single['uploaded'], 0);
    expect(packs, hasLength(1));
    expect(packs.single['pallet'], 0);
  });
}

Future<void> _insert(
  Database database,
  String presentation, {
  required String counterparty,
  required DateTime at,
}) async {
  final number = '${at.millisecondsSinceEpoch}';
  await database.insert('documents', {
    'id': 'doc-$number',
    'scanned_at': at.millisecondsSinceEpoch,
    'number': number,
    'counterparty': counterparty,
    'presentation': presentation,
    'erp_uid': 'erp-$number',
    'body': '{}',
  });
}
