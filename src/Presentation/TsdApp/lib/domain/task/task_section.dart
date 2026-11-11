/// Раздел меню задач.
enum TaskSection {
  documents,
  service,
}

String taskSectionLabel(TaskSection section) {
  return switch (section) {
    TaskSection.documents => 'Основной',
    TaskSection.service => 'Дополнительный',
  };
}
