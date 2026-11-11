import 'package:flutter/material.dart';

import '../../domain/task/task_button.dart';
import '../../domain/task/task_section.dart';
import '../../domain/task/tsd_task.dart';
import '../shell/screen_commands.dart';

/// Результат формы задачи: записанная строка или её удаление.
class TaskFormResult {
  const TaskFormResult.save(this.button) : deleted = false;

  const TaskFormResult.delete()
      : button = null,
        deleted = true;

  final TaskButton? button;
  final bool deleted;
}

/// Форма кнопки задачи. Повтор горячей клавиши оставляет форму открытой.
class TaskFormScreen extends StatefulWidget {
  const TaskFormScreen({
    super.key,
    required this.tasks,
    required this.buttons,
    this.current,
  });

  final List<TsdTask> tasks;
  final List<TaskButton> buttons;
  final TaskButton? current;

  @override
  State<TaskFormScreen> createState() => _TaskFormScreenState();
}

class _TaskFormScreenState extends State<TaskFormScreen> {
  late final TextEditingController _title;
  late final TextEditingController _hotkey;
  late final TextEditingController _order;
  late String _taskId;
  late TaskSection _section;
  late bool _journal;
  late bool _enabled;
  String? _error;

  @override
  void initState() {
    super.initState();
    final current = widget.current;
    _taskId = current?.taskId ?? widget.tasks.first.id;
    _section = current?.section ?? TaskSection.documents;
    _journal = current?.showJournal ?? false;
    _enabled = current?.enabled ?? true;
    _title = TextEditingController(text: current?.title ?? '');
    _hotkey = TextEditingController(text: current?.hotkey ?? '');
    _order = TextEditingController(text: current == null ? '0' : '${current.order}');
  }

  @override
  void dispose() {
    _title.dispose();
    _hotkey.dispose();
    _order.dispose();
    super.dispose();
  }

  bool _conflict(String hotkey) {
    final key = hotkey.toLowerCase();
    for (final button in widget.buttons) {
      if (button.taskId == _taskId) {
        continue;
      }
      if (button.hotkey.length == 1 && button.hotkey.toLowerCase() == key) {
        return true;
      }
    }
    return false;
  }

  void _ok() {
    final hotkey = _hotkey.text.trim();
    if (hotkey.isNotEmpty && _conflict(hotkey)) {
      setState(() => _error = 'Такая горячая клавиша уже задана');
      return;
    }

    Navigator.of(context).pop(
      TaskFormResult.save(
        TaskButton(
          taskId: _taskId,
          section: _section,
          title: _title.text.trim(),
          order: TaskButton.readOrder(_order.text),
          hotkey: hotkey,
          showJournal: _journal,
          enabled: _enabled,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        automaticallyImplyLeading: false,
        title: const Text('Форма задачи'),
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          DropdownButtonFormField<String>(
            initialValue: _taskId,
            isExpanded: true,
            decoration: const InputDecoration(labelText: 'Экран задачи ТСД'),
            items: [
              for (final task in widget.tasks)
                DropdownMenuItem(value: task.id, child: Text(task.defaultTitle)),
            ],
            onChanged: (value) {
              if (value == null) {
                return;
              }
              setState(() {
                _taskId = value;
                _error = null;
              });
            },
          ),
          TextField(
            controller: _title,
            decoration: const InputDecoration(labelText: 'Представление'),
          ),
          DropdownButtonFormField<TaskSection>(
            initialValue: _section,
            isExpanded: true,
            decoration: const InputDecoration(labelText: 'Экран'),
            items: const [
              DropdownMenuItem(value: TaskSection.documents, child: Text('Основной')),
              DropdownMenuItem(value: TaskSection.service, child: Text('Дополнительный')),
            ],
            onChanged: (value) {
              if (value == null) {
                return;
              }
              setState(() => _section = value);
            },
          ),
          CheckboxListTile(
            contentPadding: EdgeInsets.zero,
            title: const Text('Показывать журнал'),
            value: _journal,
            onChanged: (value) => setState(() => _journal = value ?? false),
          ),
          TextField(
            controller: _hotkey,
            maxLength: 1,
            decoration: const InputDecoration(
              labelText: 'Горячая клавиша',
              counterText: '',
            ),
            onChanged: (_) {
              if (_error != null) {
                setState(() => _error = null);
              }
            },
          ),
          TextField(
            controller: _order,
            keyboardType: TextInputType.number,
            decoration: const InputDecoration(labelText: 'Порядок'),
          ),
          if (_error != null) ...[
            const SizedBox(height: 12),
            Text(_error!),
          ],
        ],
      ),
      bottomNavigationBar: ScreenCommands(
        commands: [
          if (widget.current != null)
            ScreenCommand(
              label: 'Удалить',
              onPressed: () => Navigator.of(context).pop(const TaskFormResult.delete()),
            ),
          ScreenCommand(label: 'Закрыть', onPressed: () => Navigator.of(context).pop()),
          ScreenCommand(label: 'Ок', accent: true, onPressed: _ok),
        ],
      ),
    );
  }
}
