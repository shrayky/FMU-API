import 'package:sqflite/sqflite.dart';

/// База документов ТСД отдельным файлом `main.db`: журнал работы остаётся в `journal.db`.
class MainDatabase {
  MainDatabase({this.factory, this.path});

  /// Фабрика для тестов; на устройстве — системная SQLite из `sqflite`.
  final DatabaseFactory? factory;

  /// Путь к файлу базы; по умолчанию `main.db` в каталоге баз приложения.
  final String? path;

  /// Схема документов, марок и справочников: версия 3 добавила `marks`, `uploaded` и палету.
  static const version = 3;

  /// Тот же путь, что у [open]: по нему копируется файл базы.
  Future<String> filePath() async => path ?? '${await getDatabasesPath()}/main.db';

  Future<Database> open() async {
    final openFactory = factory ?? databaseFactory;
    return openFactory.openDatabase(
      await filePath(),
      options: OpenDatabaseOptions(
        version: version,
        onCreate: (database, _) async {
          await _createDocuments(database, 'documents');
          await _createReferences(database);
        },
        onUpgrade: (database, oldVersion, _) async {
          if (oldVersion < 3) {
            await _rebuildDocuments(database);
            await _rebuildPackSizes(database);
            await database.execute(_marksTable);
          }
        },
      ),
    );
  }

  static Future<void> _createDocuments(Database database, String table) async {
    await database.execute('''
      CREATE TABLE $table (
        id TEXT PRIMARY KEY,
        scanned_at INTEGER NOT NULL,
        number TEXT NOT NULL DEFAULT '',
        counterparty TEXT NOT NULL DEFAULT '',
        presentation TEXT NOT NULL DEFAULT '',
        erp_uid TEXT NOT NULL DEFAULT '',
        body TEXT NOT NULL DEFAULT '{}',
        uploaded INTEGER NOT NULL DEFAULT 0
      )
    ''');
    await _createDocumentIndexes(database);
  }

  static Future<void> _createDocumentIndexes(Database database) async {
    await database.execute('CREATE INDEX IF NOT EXISTS documents_scanned_at ON documents(scanned_at DESC)');
    await database.execute('CREATE INDEX IF NOT EXISTS documents_number ON documents(number)');
    await database.execute('CREATE INDEX IF NOT EXISTS documents_counterparty ON documents(counterparty)');
  }

  static Future<void> _createReferences(Database database) async {
    await database.execute('''
      CREATE TABLE nomenclature (
        uid TEXT PRIMARY KEY,
        body TEXT NOT NULL DEFAULT '{}'
      )
    ''');
    await database.execute('''
      CREATE TABLE barcodes (
        barcode TEXT PRIMARY KEY,
        nomenclature_uid TEXT NOT NULL DEFAULT '',
        body TEXT NOT NULL DEFAULT '{}'
      )
    ''');
    await database.execute(_packSizesTable);
    await database.execute(_marksTable);
  }

  /// Палета и упаковка одного товара — две строки, флаг `pallet` их различает.
  static const _packSizesTable = '''
    CREATE TABLE pack_sizes (
      nomenclature_uid TEXT NOT NULL,
      pallet INTEGER NOT NULL DEFAULT 0,
      body TEXT NOT NULL DEFAULT '{}',
      PRIMARY KEY (nomenclature_uid, pallet)
    )
  ''';

  /// Ключ марки — документ и текст марки; `units` — сколько единиц она закрыла.
  static const _marksTable = '''
    CREATE TABLE marks (
      document_id TEXT NOT NULL,
      mark TEXT NOT NULL,
      nomenclature_uid TEXT NOT NULL DEFAULT '',
      units INTEGER NOT NULL DEFAULT 1,
      PRIMARY KEY (document_id, mark)
    )
  ''';

  /// Пересобирает `documents` без поисковой копии и с отметкой выгрузки.
  static Future<void> _rebuildDocuments(Database database) async {
    await database.execute('DROP TABLE IF EXISTS documents_new');
    await database.execute('''
      CREATE TABLE documents_new (
        id TEXT PRIMARY KEY,
        scanned_at INTEGER NOT NULL,
        number TEXT NOT NULL DEFAULT '',
        counterparty TEXT NOT NULL DEFAULT '',
        presentation TEXT NOT NULL DEFAULT '',
        erp_uid TEXT NOT NULL DEFAULT '',
        body TEXT NOT NULL DEFAULT '{}',
        uploaded INTEGER NOT NULL DEFAULT 0
      )
    ''');
    await database.execute('''
      INSERT INTO documents_new (id, scanned_at, number, counterparty, presentation, erp_uid, body)
      SELECT id, scanned_at, number, counterparty, presentation, erp_uid, body FROM documents
    ''');
    await database.execute('DROP TABLE documents');
    await database.execute('ALTER TABLE documents_new RENAME TO documents');
    await _createDocumentIndexes(database);
  }

  /// Делит строку размера на упаковку и палету: старые строки остаются упаковкой.
  static Future<void> _rebuildPackSizes(Database database) async {
    await database.execute('DROP TABLE IF EXISTS pack_sizes_new');
    await database.execute('''
      CREATE TABLE pack_sizes_new (
        nomenclature_uid TEXT NOT NULL,
        pallet INTEGER NOT NULL DEFAULT 0,
        body TEXT NOT NULL DEFAULT '{}',
        PRIMARY KEY (nomenclature_uid, pallet)
      )
    ''');
    await database.execute('''
      INSERT INTO pack_sizes_new (nomenclature_uid, pallet, body)
      SELECT nomenclature_uid, 0, body FROM pack_sizes
    ''');
    await database.execute('DROP TABLE pack_sizes');
    await database.execute('ALTER TABLE pack_sizes_new RENAME TO pack_sizes');
  }
}
