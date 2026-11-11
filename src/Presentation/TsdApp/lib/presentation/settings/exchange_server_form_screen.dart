import 'package:flutter/material.dart';

import '../../application/exchange/exchange_protocols.dart';
import '../../application/message/message_center.dart';
import '../../domain/exchange/exchange_call_result.dart';
import '../../domain/exchange/exchange_protocol.dart';
import '../../domain/exchange/exchange_server.dart';
import '../shell/screen_commands.dart';
import 'exchange_probe_screen.dart';

/// Результат карточки сервера: запись или удаление существующей строки.
class ExchangeServerFormResult {
  const ExchangeServerFormResult.save(this.server) : deleted = false;

  const ExchangeServerFormResult.delete()
      : server = null,
        deleted = true;

  final ExchangeServer? server;
  final bool deleted;
}

/// Карточка сервера обмена. Без сервера создаёт строку, с сервером меняет её.
class ExchangeServerFormScreen extends StatefulWidget {
  const ExchangeServerFormScreen({
    super.key,
    this.server,
    this.initialProtocol = defaultExchangeProtocol,
    this.protocols,
    this.messages,
  });

  final ExchangeServer? server;
  final String initialProtocol;
  final ExchangeProtocol? Function(String name)? protocols;
  final MessageCenter? messages;

  @override
  State<ExchangeServerFormScreen> createState() => _ExchangeServerFormScreenState();
}

class _ExchangeServerFormScreenState extends State<ExchangeServerFormScreen> {
  late final TextEditingController _name;
  late final TextEditingController _host;
  late final TextEditingController _port;
  late final TextEditingController _publication;
  late final TextEditingController _user;
  late final TextEditingController _password;
  late final TextEditingController _timeout;
  late final MessageCenter _messages;
  var _secure = false;
  var _protocol = defaultExchangeProtocol;

  @override
  void initState() {
    super.initState();
    _messages = widget.messages ?? MessageCenter();
    final source = widget.server;
    _name = TextEditingController(text: source?.name ?? '');
    _host = TextEditingController(text: source?.host ?? '');
    _port = TextEditingController(text: source == null ? '' : '${source.port}');
    _publication = TextEditingController(text: source?.publication ?? '');
    _user = TextEditingController(text: source?.user ?? '');
    _password = TextEditingController(text: source?.password ?? '');
    _timeout = TextEditingController(text: source == null ? '' : '${source.timeoutSeconds}');
    _secure = source?.secure ?? false;
    _protocol = source?.protocol ?? widget.initialProtocol;
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
      id: widget.server?.id ?? DateTime.now().microsecondsSinceEpoch.toString(),
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
    final server = _server();
    if (server == null) {
      Navigator.of(context).pop();
      return;
    }
    Navigator.of(context).pop(ExchangeServerFormResult.save(server));
  }

  void _delete() {
    Navigator.of(context).pop(const ExchangeServerFormResult.delete());
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
      await _messages.show(context, _messages.exchangeBlocked(server, blocked));
      return;
    }

    final protocol = _protocolFor(server.protocol);
    if (protocol == null) {
      await _messages.show(
        context,
        _messages.exchangeBlocked(
          server,
          const ExchangeCallResult(success: false, error: exchangeProtocolUnknown),
        ),
      );
      return;
    }

    final result = await Navigator.of(context).push<ExchangeCallResult>(
      MaterialPageRoute(
        builder: (_) => ExchangeProbeScreen<ExchangeCallResult>(
          addressLine: server.endpoint,
          timeout: Duration(seconds: server.timeoutSeconds),
          run: (cancellation) => protocol.probe(server, cancellation),
        ),
      ),
    );
    if (!mounted || result == null) {
      return;
    }
    await _messages.show(context, _messages.exchangeProbe(server, result));
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        automaticallyImplyLeading: false,
        title: Text(widget.server == null ? 'Сервер обмена (создание)' : 'Сервер обмена'),
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
          if (widget.server != null) ScreenCommand(label: 'Удалить', onPressed: _delete),
          ScreenCommand(label: 'Проверить', accent: true, onPressed: _probe),
          ScreenCommand(label: 'Закрыть', onPressed: _close),
        ],
      ),
    );
  }
}
