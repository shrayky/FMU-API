import 'package:sqflite/sqflite.dart';

/// База журнала отдельным файлом: сбой основной базы не уносит журнал.
class JournalDatabase {
  JournalDatabase({this.factory, this.path});

  /// Фабрика для тестов; на устройстве — системная SQLite из `sqflite`.
  final DatabaseFactory? factory;

  /// Путь к файлу базы; по умолчанию `journal.db` в каталоге баз приложения.
  final String? path;

  Future<Database> open() async {
    final openFactory = factory ?? databaseFactory;
    return openFactory.openDatabase(
      path ?? '${await getDatabasesPath()}/journal.db',
      options: OpenDatabaseOptions(
        version: 1,
        onCreate: (database, version) async {
          await database.execute('''
            CREATE TABLE work_log (
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              at INTEGER NOT NULL,
              level TEXT NOT NULL,
              text TEXT NOT NULL,
              payload TEXT NOT NULL DEFAULT '{}'
            )
          ''');
          await database.execute('CREATE INDEX work_log_at ON work_log(at DESC)');
        },
      ),
    );
  }
}
