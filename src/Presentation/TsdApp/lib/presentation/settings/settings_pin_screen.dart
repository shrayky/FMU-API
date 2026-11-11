import 'package:flutter/material.dart';

import '../../theme/webix_dark_theme.dart';
import '../shell/screen_commands.dart';

/// Запрос пин-кода перед открытием настроек.
class SettingsPinScreen extends StatefulWidget {
  const SettingsPinScreen({super.key, required this.pin});

  final String pin;

  @override
  State<SettingsPinScreen> createState() => _SettingsPinScreenState();
}

class _SettingsPinScreenState extends State<SettingsPinScreen> {
  final _pin = TextEditingController();
  var _rejected = false;

  @override
  void dispose() {
    _pin.dispose();
    super.dispose();
  }

  void _submit() {
    if (_pin.text.trim() != widget.pin) {
      setState(() => _rejected = true);
      return;
    }
    Navigator.of(context).pop(true);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        automaticallyImplyLeading: false,
        title: const Text('Пин код настроек'),
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          TextField(
            controller: _pin,
            autofocus: true,
            obscureText: true,
            decoration: const InputDecoration(labelText: 'Пин код'),
            onChanged: (_) {
              if (_rejected) {
                setState(() => _rejected = false);
              }
            },
            onSubmitted: (_) => _submit(),
          ),
          if (_rejected) ...[
            const SizedBox(height: 12),
            const Text('Неверный пин код', style: TextStyle(color: WebixDarkTheme.text)),
          ],
        ],
      ),
      bottomNavigationBar: ScreenCommands(
        commands: [
          ScreenCommand(label: 'Закрыть', onPressed: () => Navigator.of(context).pop()),
          ScreenCommand(label: 'Ок', accent: true, onPressed: _submit),
        ],
      ),
    );
  }
}
