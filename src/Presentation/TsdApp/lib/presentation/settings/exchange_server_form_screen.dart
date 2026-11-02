import 'package:flutter/material.dart';

import '../../application/exchange/exchange_protocols.dart';
import '../../domain/exchange/exchange_call_result.dart';
import '../../domain/exchange/exchange_protocol.dart';
import '../../domain/exchange/exchange_server.dart';
import '../../theme/webix_dark_theme.dart';
import '../shell/screen_commands.dart';
import 'exchange_probe_screen.dart';

/// Карточка создания сервера обмена.
class ExchangeServerFormScreen extends StatefulWidget {
  const ExchangeServerFormScreen({
    super.key,
    this.initialProtocol = defaultExchangeProtocol,
    this.protocols,
  });

  final String initialProtocol;
  final ExchangeProtocol? Function(String name)? protocols;

  @override
  State<ExchangeServerFormScreen> createState() => _ExchangeServerFormScreenState();
}

class _ExchangeServerFormScreenState extends State<ExchangeServerFormScreen> {
  final _name = TextEditingController();
  final _host = TextEditingController();
  final _port = TextEditingController();
  final _publication = TextEditingController();
  final _user = TextEditingController();
  final _password = TextEditingController();
  final _timeout = TextEditingController();
  var _secure = false;
  var _protocol = defaultExchangeProtocol;

  @override
  void initState() {
    super.initState();
    _protocol = widget.initialProtocol;
  }

  @override
  void dispose() {
    _name.dispose();
    _host.dispose();
    _port.dispose();
    _publication.dispose();
    _user.dispose();
    _password.dispose();
    _timeout.dispose();
    super.dispose();
  }

  ExchangeServer? _server() {
    final name = _name.text.trim();
    if (name.isEmpty) {
      return null;
    }

    return ExchangeServer(
      id: DateTime.now().microsecondsSinceEpoch.toString(),
      name: name,
      host: _host.text.trim(),
      port: int.tryParse(_port.text.trim()) ?? 0,
      secure: _secure,
      publication: _publication.text.trim(),
      user: _user.text.trim(),
      password: _password.text,
      timeoutSeconds: int.tryParse(_timeout.text.trim()) ?? 0,
      protocol: _protocol,
    );
  }

  void _close() {
    Navigator.of(context).pop(_server());
  }

  ExchangeProtocol? _protocolFor(String name) {
    final lookup = widget.protocols;
    if (lookup != null) {
      return lookup(name);
    }
    return const ExchangeProtocols().byName(name);
  }

  Future<void> _probe() async {
    final server = ExchangeServer(
      id: 'probe',
      name: _name.text.trim(),
      host: _host.text.trim(),
      port: int.tryParse(_port.text.trim()) ?? 0,
      secure: _secure,
      publication: _publication.text.trim(),
      user: _user.text.trim(),
      password: _password.text,
      timeoutSeconds: int.tryParse(_timeout.text.trim()) ?? 0,
      protocol: _protocol,
    );
    final blocked = probeBlocked(server);
    if (blocked != null) {
      await _showProbe(blocked);
      return;
    }

    final protocol = _protocolFor(server.protocol);
    if (protocol == null) {
      await _showProbe(const ExchangeCallResult(success: false, error: exchangeProtocolUnknown));
      return;
    }

    final result = await Navigator.of(context).push<ExchangeCallResult>(
      MaterialPageRoute(
        builder: (_) => ExchangeProbeScreen(
          addressLine: server.endpoint,
          probe: (cancellation) => protocol.probe(server, cancellation),
        ),
      ),
    );
    if (!mounted || result == null) {
      return;
    }
    await _showProbe(result);
  }

  Future<void> _showProbe(ExchangeCallResult result) {
    final text = result.success ? exchangeProbeSuccess : '$exchangeProbeFailedPrefix${result.error}';
    return showDialog<void>(
      context: context,
      builder: (dialogContext) {
        return AlertDialog(
          content: Text(text),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(dialogContext).pop(),
              child: const Text('Ок', style: TextStyle(color: WebixDarkTheme.accent)),
            ),
          ],
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        automaticallyImplyLeading: false,
        title: const Text('Сервер обмена (создание)'),
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          TextField(controller: _name, decoration: const InputDecoration(labelText: 'Наименование')),
          TextField(controller: _host, decoration: const InputDecoration(labelText: 'IP-адрес')),
          TextField(
            controller: _port,
            keyboardType: TextInputType.number,
            decoration: const InputDecoration(labelText: 'Порт'),
          ),
          SwitchListTile(
            contentPadding: EdgeInsets.zero,
            title: const Text('Безопасное соединение'),
            value: _secure,
            onChanged: (value) => setState(() => _secure = value),
          ),
          TextField(
            controller: _publication,
            decoration: const InputDecoration(labelText: 'Имя публикации базы'),
          ),
          TextField(controller: _user, decoration: const InputDecoration(labelText: 'Пользователь')),
          TextField(
            controller: _password,
            obscureText: true,
            decoration: const InputDecoration(labelText: 'Пароль'),
          ),
          TextField(
            controller: _timeout,
            keyboardType: TextInputType.number,
            decoration: const InputDecoration(labelText: 'Таймаут запроса (сек)'),
          ),
          DropdownButtonFormField<String>(
            initialValue: exchangeProtocolNames.contains(_protocol) ? _protocol : null,
            decoration: const InputDecoration(labelText: 'Протокол обмена'),
            items: [
              for (final name in exchangeProtocolNames) DropdownMenuItem(value: name, child: Text(name)),
            ],
            onChanged: (value) {
              if (value == null) {
                return;
              }
              setState(() => _protocol = value);
            },
          ),
        ],
      ),
      bottomNavigationBar: ScreenCommands(
        commands: [
          ScreenCommand(label: 'Проверить', accent: true, onPressed: _probe),
          ScreenCommand(label: 'Закрыть', onPressed: _close),
        ],
      ),
    );
  }
}
