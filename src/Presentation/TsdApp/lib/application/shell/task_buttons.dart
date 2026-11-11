import '../../domain/task/task_button.dart';
import '../../domain/task/task_section.dart';
import '../../domain/task/tsd_task.dart';

/// Правила кнопок меню: значения по умолчанию, видимость, порядок и горячие клавиши.
class TaskButtons {
  static List<TaskButton> resolve(List<TsdTask> tasks, List<TaskButton>? stored) {
    if (stored == null) {
      return [for (final task in tasks) defaultFor(task)];
    }

    final known = {for (final task in tasks) task.id: task};
    final seen = <String>{};
    final result = <TaskButton>[];
    for (final button in stored) {
      final task = known[button.taskId];
      if (task == null || !seen.add(button.taskId)) {
        continue;
      }
      result.add(_filled(task, button));
    }
    return result;
  }

  static TaskButton defaultFor(TsdTask task) {
    return TaskButton(
      taskId: task.id,
      section: task.defaultSection,
      title: task.defaultTitle,
      order: 1,
      hotkey: '',
      showJournal: task.hasJournal,
      enabled: true,
    );
  }

  static List<TaskButton> visible(List<TaskButton> buttons, TaskSection section) {
    final items = buttons.where((button) => button.enabled && button.section == section).toList();
    items.sort((a, b) {
      final byOrder = a.order.compareTo(b.order);
      if (byOrder != 0) {
        return byOrder;
      }
      return a.title.compareTo(b.title);
    });
    return items;
  }

  static bool journalVisible(TsdTask task, TaskButton button) {
    return task.hasJournal && button.showJournal;
  }

  static bool hotkeyFires(String hotkey) => hotkey.length == 1;

  static TaskButton? matchHotkey(List<TaskButton> buttons, String character) {
    if (!hotkeyFires(character)) {
      return null;
    }

    final key = character.toLowerCase();
    final matches = buttons.where((button) {
      return button.enabled && hotkeyFires(button.hotkey) && button.hotkey.toLowerCase() == key;
    }).toList();
    if (matches.isEmpty) {
      return null;
    }

    matches.sort((a, b) {
      final bySection = _rank(a.section).compareTo(_rank(b.section));
      if (bySection != 0) {
        return bySection;
      }
      final byOrder = a.order.compareTo(b.order);
      if (byOrder != 0) {
        return byOrder;
      }
      return a.title.compareTo(b.title);
    });
    return matches.first;
  }

  static TaskButton _filled(TsdTask task, TaskButton button) {
    final title = button.title.trim();
    return TaskButton(
      taskId: task.id,
      section: button.section,
      title: title.isEmpty ? task.defaultTitle : title,
      order: button.order,
      hotkey: button.hotkey,
      showJournal: button.showJournal,
      enabled: button.enabled,
    );
  }

  static int _rank(TaskSection section) {
    return switch (section) {
      TaskSection.documents => 0,
      TaskSection.service => 1,
    };
  }
}
