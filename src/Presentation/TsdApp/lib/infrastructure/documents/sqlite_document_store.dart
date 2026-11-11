import 'dart:convert';

import 'package:sqflite/sqflite.dart';

import '../../domain/documents/document_packet.dart';
import '../../domain/documents/document_store.dart';
import '../../domain/documents/scanned_document.dart';
import '../../domain/documents/stored_mark.dart';
import 'main_database.dart';

/// Документы сбора марок в `main.db`: таблица `documents`, марки `marks` и справочники пакета.
class SqliteDocumentStore implements DocumentStore {
  SqliteDocumentStore({Future<Database> Function()? open}) : _open = open ?? MainDatabase().open;

  final Future<Database> Function() _open;
  Future<Database>? _database;
  var _sequence = 0;

  @override
  Future<ScannedDocument> save(DocumentPacket packet, {DateTime? scannedAt}) async {
    final database = await _db();
    final at = scannedAt ?? DateTime.now();
    final document = ScannedDocument(
      id: '${at.microsecondsSinceEpoch}-${_sequence++}',
      scannedAt: at,
      number: textOf(packet.document['Номер']),
      counterparty: textOf(packet.document['Контрагент']),
      presentation: textOf(packet.document['Представление']),
      erpUid: textOf(packet.document['УИД']),
      body: packet.document,
    );

    await database.transaction((txn) async {
      await _writeNomenclature(txn, packet.nomenclature);
      await _writeBarcodes(txn, packet.barcodes);
      await _writePackSizes(txn, packet.packSizes);
      await txn.insert('documents', {
        'id': document.id,
        'scanned_at': at.millisecondsSinceEpoch,
        'number': document.number,
        'counterparty': document.counterparty,
        'presentation': document.presentation,
        'erp_uid': document.erpUid,
        'body': jsonEncode(packet.document),
      });
    });
    return document;
  }

  @override
  Future<List<ScannedDocument>> documents({String query = ''}) async {
    final database = await _db();
    final rows = await database.query(
      'documents',
      columns: const [
        'id',
        'scanned_at',
        'number',
        'counterparty',
        'presentation',
        'erp_uid',
        'uploaded',
      ],
      orderBy: 'scanned_at DESC',
    );
    final text = query.trim().toLowerCase();
    return [
      for (final row in rows)
        if (text.isEmpty || _matchesSearch(row, text)) _document(row),
    ];
  }

  @override
  Future<ScannedDocument?> document(String id) async {
    final database = await _db();
    final rows = await database.query('documents', where: 'id = ?', whereArgs: [id], limit: 1);
    return rows.isEmpty ? null : _document(rows.first);
  }

  @override
  Future<void> trim({required int days}) async {
    if (days <= 0) {
      return;
    }

    final database = await _db();
    final border = DateTime.now().subtract(Duration(days: days)).millisecondsSinceEpoch;
    await database.transaction((txn) async {
      await txn.delete(
        'marks',
        where: 'document_id IN (SELECT id FROM documents WHERE scanned_at < ?)',
        whereArgs: [border],
      );
      await txn.delete('documents', where: 'scanned_at < ?', whereArgs: [border]);
    });
  }

  @override
  Future<Map<String, String>> nomenclatureNames(Set<String> uids) async {
    if (uids.isEmpty) {
      return const {};
    }

    final database = await _db();
    final placeholders = List.filled(uids.length, '?').join(', ');
    final rows = await database.query(
      'nomenclature',
      columns: ['uid', 'body'],
      where: 'uid IN ($placeholders)',
      whereArgs: uids.toList(),
    );
    return {
      for (final row in rows) '${row['uid'] ?? ''}': nomenclatureName(_body(row)),
    };
  }

  @override
  Future<Map<String, String>> nomenclatureUidsByBarcodes(Iterable<String> barcodes) async {
    final values = barcodes.toList();
    if (values.isEmpty) {
      return const {};
    }

    final database = await _db();
    final placeholders = List.filled(values.length, '?').join(', ');
    final rows = await database.query(
      'barcodes',
      columns: ['barcode', 'nomenclature_uid'],
      where: 'barcode IN ($placeholders)',
      whereArgs: values,
    );
    return {
      for (final row in rows) '${row['barcode'] ?? ''}': '${row['nomenclature_uid'] ?? ''}',
    };
  }

  @override
  Future<int> barcodeCount() async {
    final database = await _db();
    final rows = await database.rawQuery('SELECT COUNT(*) AS total FROM barcodes');
    return (rows.first['total'] as int?) ?? 0;
  }

  @override
  Future<List<StoredMark>> marks(String documentId) async {
    final database = await _db();
    final rows = await database.query(
      'marks',
      columns: const ['mark', 'nomenclature_uid', 'units'],
      where: 'document_id = ?',
      whereArgs: [documentId],
      orderBy: 'rowid',
    );
    return [
      for (final row in rows)
        StoredMark(
          mark: '${row['mark'] ?? ''}',
          nomenclatureUid: '${row['nomenclature_uid'] ?? ''}',
          units: (row['units'] as int?) ?? 1,
        ),
    ];
  }

  @override
  Future<void> addMark(String documentId, StoredMark mark) async {
    final database = await _db();
    await database.insert(
      'marks',
      {
        'document_id': documentId,
        'mark': mark.mark,
        'nomenclature_uid': mark.nomenclatureUid,
        'units': mark.units,
      },
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }

  @override
  Future<bool> removeMark(String documentId, String mark) async {
    final database = await _db();
    final removed = await database.delete(
      'marks',
      where: 'document_id = ? AND mark = ?',
      whereArgs: [documentId, mark],
    );
    return removed > 0;
  }

  @override
  Future<void> setUploaded(String documentId, {required bool uploaded}) async {
    final database = await _db();
    await database.update(
      'documents',
      {'uploaded': uploaded ? 1 : 0},
      where: 'id = ?',
      whereArgs: [documentId],
    );
  }

  @override
  Future<num?> packSizeUnits(String nomenclatureUid, {required bool pallet}) async {
    if (nomenclatureUid.isEmpty) {
      return null;
    }

    final database = await _db();
    final rows = await database.query(
      'pack_sizes',
      columns: const ['body'],
      where: 'nomenclature_uid = ? AND pallet = ?',
      whereArgs: [nomenclatureUid, pallet ? 1 : 0],
      limit: 1,
    );
    if (rows.isEmpty) {
      return null;
    }
    return _positive(_body(rows.first)['ЕдиницВУпаковке']);
  }

  @override
  Future<num?> barcodeCoefficient(String barcode) async {
    if (barcode.isEmpty) {
      return null;
    }

    final database = await _db();
    final rows = await database.query(
      'barcodes',
      columns: const ['body'],
      where: 'barcode = ?',
      whereArgs: [barcode],
      limit: 1,
    );
    if (rows.isEmpty) {
      return null;
    }
    return _positive(_body(rows.first)['Коэффициент']);
  }

  Future<Database> _db() => _database ??= _open();

  /// Поиск идёт в Dart: SQLite `LIKE` не складывает кириллицу, а `%` и `_` — обычные буквы.
  static bool _matchesSearch(Map<String, Object?> row, String text) {
    for (final key in const ['presentation', 'number', 'counterparty']) {
      if ('${row[key] ?? ''}'.toLowerCase().contains(text)) {
        return true;
      }
    }
    return false;
  }

  static num? _positive(Object? value) {
    if (value == null) {
      return null;
    }
    final units = numberOf(value);
    return units > 0 ? units : null;
  }

  static Future<void> _writeNomenclature(Transaction txn, List<Map<String, Object?>> cards) async {
    for (final card in cards) {
      final uid = textOf(card['УИД']);
      if (uid.isEmpty) {
        continue;
      }
      await txn.insert(
        'nomenclature',
        {'uid': uid, 'body': jsonEncode(card)},
        conflictAlgorithm: ConflictAlgorithm.replace,
      );
    }
  }

  static Future<void> _writeBarcodes(Transaction txn, List<Map<String, Object?>> cards) async {
    for (final card in cards) {
      final barcode = textOf(card['Штрихкод']);
      if (barcode.isEmpty) {
        continue;
      }
      await txn.insert(
        'barcodes',
        {
          'barcode': barcode,
          'nomenclature_uid': textOf(card['Номенклатура']),
          'body': jsonEncode(card),
        },
        conflictAlgorithm: ConflictAlgorithm.replace,
      );
    }
  }

  static Future<void> _writePackSizes(Transaction txn, List<Map<String, Object?>> cards) async {
    for (final card in cards) {
      final uid = textOf(card['Номенклатура']);
      if (uid.isEmpty) {
        continue;
      }
      await txn.insert(
        'pack_sizes',
        {
          'nomenclature_uid': uid,
          'pallet': flagOf(card['Паллета']) ? 1 : 0,
          'body': jsonEncode(card),
        },
        conflictAlgorithm: ConflictAlgorithm.replace,
      );
    }
  }

  static ScannedDocument _document(Map<String, Object?> row) {
    return ScannedDocument(
      id: '${row['id'] ?? ''}',
      scannedAt: DateTime.fromMillisecondsSinceEpoch(row['scanned_at'] as int? ?? 0),
      number: '${row['number'] ?? ''}',
      counterparty: '${row['counterparty'] ?? ''}',
      presentation: '${row['presentation'] ?? ''}',
      erpUid: '${row['erp_uid'] ?? ''}',
      body: _body(row),
      uploaded: (row['uploaded'] as int? ?? 0) != 0,
    );
  }

  static Map<String, Object?> _body(Map<String, Object?> row) {
    final raw = '${row['body'] ?? ''}';
    if (raw.trim().isEmpty) {
      return const {};
    }

    try {
      final decoded = jsonDecode(raw);
      if (decoded is Map) {
        return {for (final item in decoded.entries) '${item.key}': item.value};
      }
    } catch (_) {
      return const {};
    }
    return const {};
  }
}

/// Наименование товара из карточки `nomenclature`; неизвестные ключи дают пустую строку.
String nomenclatureName(Map<String, Object?> card) {
  for (final key in const ['Наименование', 'Представление', 'Название', 'name']) {
    final value = textOf(card[key]);
    if (value.isNotEmpty) {
      return value;
    }
  }
  return '';
}
