import '../task/task_button.dart';

/// Строка вкладки «Обмен». Сама загрузка номенклатуры этим срезом не выполняется.
class ExchangeVariant {
  const ExchangeVariant({
    required this.id,
    this.operation = nomenclature,
    this.order = 0,
    this.isDefault = false,
    this.hotkey = '',
  });

  static const nomenclature = 'Загрузка номенклатуры';

  final String id;
  final String operation;
  final int order;
  final bool isDefault;
  final String hotkey;

  Map<String, Object?> toJson() {
    return {
      'id': id,
      'operation': operation,
      'order': order,
      'isDefault': isDefault,
      'hotkey': hotkey,
    };
  }

  static ExchangeVariant? fromJson(Object? json) {
    if (json is! Map) {
      return null;
    }

    final id = json['id'];
    if (id is! String || id.isEmpty) {
      return null;
    }

    final operation = json['operation'];
    final hotkey = json['hotkey'];
    final isDefault = json['isDefault'];
    return ExchangeVariant(
      id: id,
      operation: operation is String && operation.isNotEmpty ? operation : nomenclature,
      order: TaskButton.readOrder(json['order']),
      isDefault: isDefault is bool ? isDefault : false,
      hotkey: hotkey is String ? hotkey : '',
    );
  }
}
