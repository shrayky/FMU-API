import '../../domain/task/task_section.dart';
import '../../domain/task/tsd_task.dart';
import '../../infrastructure/settings/app_settings.dart';
import '../../presentation/mark_check/mark_check_screen.dart';
import '../../presentation/mark_collection/mark_collection_screen.dart';
import '../../presentation/mark_collection/mark_journal_screen.dart';

/// Известные задачи ТСД и их экраны. Новая задача добавляется сюда, меню из-за неё не меняется.
class TaskRegistry {
  const TaskRegistry();

  List<TsdTask> tasks() {
    return [
      TsdTask(
        id: 'markCollection',
        defaultTitle: 'Сбор марок',
        defaultSection: TaskSection.documents,
        hasJournal: true,
        open: (AppSettings settings) => MarkCollectionScreen(settings: settings),
        openJournal: (AppSettings settings) => MarkJournalScreen(settings: settings),
      ),
      TsdTask(
        id: 'markCheck',
        defaultTitle: 'Проверка марки',
        defaultSection: TaskSection.documents,
        hasJournal: false,
        open: (AppSettings settings) => MarkCheckScreen(settings: settings),
      ),
    ];
  }
}
