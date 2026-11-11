import 'package:flutter/widgets.dart';

import '../../infrastructure/settings/app_settings.dart';
import 'task_section.dart';

/// Задача ТСД. Экран задаёт реестр, меню задач его не выбирает само.
class TsdTask {
  const TsdTask({
    required this.id,
    required this.defaultTitle,
    required this.defaultSection,
    required this.hasJournal,
    required this.open,
    this.openJournal,
  });

  final String id;
  final String defaultTitle;
  final TaskSection defaultSection;
  final bool hasJournal;
  final Widget Function(AppSettings settings) open;
  final Widget Function(AppSettings settings)? openJournal;
}
