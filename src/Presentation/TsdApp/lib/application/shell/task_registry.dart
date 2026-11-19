import '../../domain/documents/document_store.dart';
import '../../domain/task/task_section.dart';
import '../../domain/task/tsd_task.dart';
import '../../infrastructure/settings/app_settings.dart';
import '../../presentation/mark_check/mark_check_screen.dart';
import '../../presentation/mark_collection/mark_collection_screen.dart';
import '../../presentation/mark_collection/mark_journal_screen.dart';
import '../../presentation/mark_write_off/mark_write_off_journal_screen.dart';
import '../../presentation/mark_write_off/mark_write_off_screen.dart';

/// Известные задачи ТСД и их экраны. Новая задача добавляется сюда, меню из-за неё не меняется.
class TaskRegistry {
  const TaskRegistry({this.store});

  /// Хранилище документов для тестов: на устройстве экран открывает его сам.
  final DocumentStore? store;

  List<TsdTask> tasks() {
    return [
      TsdTask(
        id: 'markCollection',
        defaultTitle: 'Сбор марок',
        defaultSection: TaskSection.documents,
        hasJournal: true,
        open: (AppSettings settings) => MarkCollectionScreen(settings: settings, store: store),
        openJournal: (AppSettings settings) => MarkJournalScreen(settings: settings, store: store),
      ),
      TsdTask(
        id: 'markCheck',
        defaultTitle: 'Проверка марки',
        defaultSection: TaskSection.documents,
        hasJournal: false,
        open: (AppSettings settings) => MarkCheckScreen(settings: settings),
      ),
      TsdTask(
        id: 'markWriteOff',
        defaultTitle: 'Списание марок',
        defaultSection: TaskSection.documents,
        hasJournal: true,
        open: (AppSettings settings) => MarkWriteOffScreen(
          settings: settings,
          create: true,
          store: store,
        ),
        openJournal: (AppSettings settings) => MarkWriteOffJournalScreen(
          settings: settings,
          store: store,
        ),
      ),
    ];
  }
}
