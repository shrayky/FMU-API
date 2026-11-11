import 'package:flutter/material.dart';

import '../../domain/exchange/exchange_variant.dart';
import '../../domain/task/task_button.dart';
import '../shell/screen_commands.dart';

/// Результат формы обмена: записанная строка или её удаление.
class ExchangeVariantFormResult {
  const ExchangeVariantFormResult.save(this.variant) : deleted = false;

  const ExchangeVariantFormResult.delete()
      : variant = null,
        deleted = true;

  final ExchangeVariant? variant;
  final bool deleted;
}

/// Форма варианта обмена. Загрузка номенклатуры отсюда не запускается.
class ExchangeVariantFormScreen extends StatefulWidget {
  const ExchangeVariantFormScreen({super.key, this.current});

  final ExchangeVariant? current;

  @override
  State<ExchangeVariantFormScreen> createState() => _ExchangeVariantFormScreenState();
}

class _ExchangeVariantFormScreenState extends State<ExchangeVariantFormScreen> {
  late final TextEditingController _order;
  late final TextEditingController _hotkey;
  late bool _isDefault;

  @override
  void initState() {
    super.initState();
    final current = widget.current;
    _order = TextEditingController(text: current == null ? '0' : '${current.order}');
    _hotkey = TextEditingController(text: current == null ? '0' : current.hotkey);
    _isDefault = current?.isDefault ?? false;
  }

  @override
  void dispose() {
    _order.dispose();
    _hotkey.dispose();
    super.dispose();
  }

  void _ok() {
    final current = widget.current;
    Navigator.of(context).pop(
      ExchangeVariantFormResult.save(
        ExchangeVariant(
          id: current?.id ?? DateTime.now().microsecondsSinceEpoch.toString(),
          operation: ExchangeVariant.nomenclature,
          order: TaskButton.readOrder(_order.text),
          isDefault: _isDefault,
          hotkey: _hotkey.text.trim(),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        automaticallyImplyLeading: false,
        title: const Text('Форма варианта обмена'),
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          DropdownButtonFormField<String>(
            initialValue: ExchangeVariant.nomenclature,
            isExpanded: true,
            decoration: const InputDecoration(labelText: 'Операция'),
            items: const [
              DropdownMenuItem(
                value: ExchangeVariant.nomenclature,
                child: Text(ExchangeVariant.nomenclature),
              ),
            ],
            onChanged: (_) {},
          ),
          TextField(
            controller: _order,
            keyboardType: TextInputType.number,
            decoration: const InputDecoration(labelText: 'Порядок'),
          ),
          SwitchListTile(
            contentPadding: EdgeInsets.zero,
            title: const Text('По умолчанию'),
            value: _isDefault,
            onChanged: (value) => setState(() => _isDefault = value),
          ),
          TextField(
            controller: _hotkey,
            decoration: const InputDecoration(labelText: 'Горячая клавиша'),
          ),
        ],
      ),
      bottomNavigationBar: ScreenCommands(
        commands: [
          if (widget.current != null)
            ScreenCommand(
              label: 'Удалить',
              onPressed: () => Navigator.of(context).pop(const ExchangeVariantFormResult.delete()),
            ),
          ScreenCommand(label: 'Закрыть', onPressed: () => Navigator.of(context).pop()),
          ScreenCommand(label: 'Ок', accent: true, onPressed: _ok),
        ],
      ),
    );
  }
}
