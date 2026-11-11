import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:tsd_app/application/shell/task_buttons.dart';
import 'package:tsd_app/application/shell/task_registry.dart';
import 'package:tsd_app/domain/task/task_button.dart';
import 'package:tsd_app/domain/task/task_section.dart';
import 'package:tsd_app/domain/task/tsd_task.dart';
import 'package:tsd_app/infrastructure/settings/app_settings.dart';

void main() {
  test('по умолчанию обе задачи в документах', () {
    final buttons = TaskButtons.resolve(const TaskRegistry().tasks(), null);

    expect(buttons.map((button) => button.taskId), ['markCollection', 'markCheck']);
    expect(buttons.every((button) => button.section == TaskSection.documents && button.enabled && button.order == 1), isTrue);
    expect(buttons.first.showJournal, isTrue);
    expect(buttons.first.title, 'Сбор марок');
    expect(buttons.last.showJournal, isFalse);
    expect(buttons.last.title, 'Проверка марки');
  });

  test('неизвестный код отбрасывается, удалённая задача не возвращается', () {
    final resolved = TaskButtons.resolve(const TaskRegistry().tasks(), [
      _button('ghost', title: 'Чужая'),
      _button('markCheck', title: 'Своя', section: TaskSection.service, order: 3, hotkey: 'a'),
    ]);

    expect(resolved.map((button) => button.taskId), ['markCheck']);
    expect(resolved.single.title, 'Своя');
    expect(resolved.single.section, TaskSection.service);
  });

  test('пустой сохранённый список не восстанавливает задачи', () {
    expect(TaskButtons.resolve(const TaskRegistry().tasks(), const []), isEmpty);
  });

  test('выключенные скрыты', () {
    final buttons = [
      for (final button in TaskButtons.resolve(const TaskRegistry().tasks(), null))
        _button(button.taskId, title: button.title, showJournal: button.showJournal, enabled: false),
    ];

    expect(TaskButtons.visible(buttons, TaskSection.documents), isEmpty);
    expect(TaskButtons.visible(buttons, TaskSection.service), isEmpty);
  });

  test('сортировка по порядку и подписи', () {
    final tasks = [
      _task('late', 'Яблоко'),
      _task('early', 'Банан'),
      _task('mid', 'Арбуз'),
    ];
    final visible = TaskButtons.visible([
      _button('late', title: 'Яблоко', order: 2),
      _button('early', title: 'Банан', order: 1),
      _button('mid', title: 'Арбуз', order: 1),
    ], TaskSection.documents);

    expect(tasks, isNotEmpty);
    expect(visible.map((button) => button.title), ['Арбуз', 'Банан', 'Яблоко']);
  });

  test('пустая подпись заменяется на подпись задачи', () {
    final resolved = TaskButtons.resolve(const TaskRegistry().tasks(), [
      _button('markCheck', title: '   '),
      _button('markCollection', title: 'Сбор'),
    ]);

    expect(resolved.firstWhere((button) => button.taskId == 'markCheck').title, 'Проверка марки');
    expect(resolved.firstWhere((button) => button.taskId == 'markCollection').title, 'Сбор');
  });

  test('журнал только при обоих флагах', () {
    final withJournal = _task('collection', 'Сбор', journal: true);
    final withoutJournal = _task('check', 'Проверка');
    final shown = _button('collection', showJournal: true);
    final hidden = _button('collection', showJournal: false);

    expect(TaskButtons.journalVisible(withJournal, shown), isTrue);
    expect(TaskButtons.journalVisible(withJournal, hidden), isFalse);
    expect(TaskButtons.journalVisible(withoutJournal, shown), isFalse);
  });

  test('горячая клавиша длиннее одного символа не срабатывает', () {
    final buttons = [
      _button('long', hotkey: 'ab'),
      _button('service', hotkey: 'a', section: TaskSection.service, order: 1, title: 'Сервис'),
      _button('documents', hotkey: 'A', order: 2, title: 'Документ'),
    ];

    expect(TaskButtons.hotkeyFires('ab'), isFalse);
    expect(TaskButtons.hotkeyFires('a'), isTrue);
    expect(TaskButtons.matchHotkey(buttons, 'a')?.taskId, 'documents');
    expect(TaskButtons.matchHotkey(buttons, 'ab'), isNull);
  });
}

TsdTask _task(String id, String title, {bool journal = false}) {
  return TsdTask(
    id: id,
    defaultTitle: title,
    defaultSection: TaskSection.documents,
    hasJournal: journal,
    open: _screen,
    openJournal: journal ? _screen : null,
  );
}

Widget _screen(AppSettings settings) => const SizedBox();

TaskButton _button(
  String taskId, {
  String title = '',
  TaskSection section = TaskSection.documents,
  int order = 1,
  String hotkey = '',
  bool showJournal = false,
  bool enabled = true,
}) {
  return TaskButton(
    taskId: taskId,
    section: section,
    title: title,
    order: order,
    hotkey: hotkey,
    showJournal: showJournal,
    enabled: enabled,
  );
}
