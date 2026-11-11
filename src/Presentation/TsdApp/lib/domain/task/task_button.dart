import 'task_section.dart';

/// Сохранённая кнопка задачи: раздел, подпись, порядок и горячая клавиша.
class TaskButton {
  const TaskButton({
    required this.taskId,
    required this.section,
    required this.title,
    required this.order,
    required this.hotkey,
    required this.showJournal,
    required this.enabled,
  });

  final String taskId;
  final TaskSection section;
  final String title;
  final int order;
  final String hotkey;
  final bool showJournal;
  final bool enabled;

  static int readOrder(Object? value) {
    if (value is int) {
      return value;
    }
    if (value is num) {
      return value.toInt();
    }
    if (value is String) {
      final parsed = int.tryParse(value.trim());
      if (parsed != null) {
        return parsed;
      }
    }
    return 1;
  }

  static TaskSection readSection(Object? value) {
    if (value == TaskSection.service.name) {
      return TaskSection.service;
    }
    return TaskSection.documents;
  }

  Map<String, Object?> toJson() {
    return {
      'taskId': taskId,
      'section': section.name,
      'title': title,
      'order': order,
      'hotkey': hotkey,
      'showJournal': showJournal,
      'enabled': enabled,
    };
  }

  static TaskButton? fromJson(Object? json) {
    if (json is! Map) {
      return null;
    }

    final id = json['taskId'];
    if (id is! String || id.isEmpty) {
      return null;
    }

    final title = json['title'];
    final hotkey = json['hotkey'];
    final showJournal = json['showJournal'];
    final enabled = json['enabled'];
    return TaskButton(
      taskId: id,
      section: readSection(json['section']),
      title: title is String ? title : '',
      order: readOrder(json['order']),
      hotkey: hotkey is String ? hotkey : '',
      showJournal: showJournal is bool ? showJournal : false,
      enabled: enabled is bool ? enabled : true,
    );
  }
}
