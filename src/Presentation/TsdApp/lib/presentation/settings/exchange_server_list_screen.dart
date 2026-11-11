import 'package:flutter/material.dart';

import '../../domain/exchange/exchange_server.dart';
import '../../theme/webix_dark_theme.dart';
import '../shell/screen_commands.dart';
import 'exchange_server_form_screen.dart';

/// Список серверов обмена. Строка выбирает сервер, значок открывает карточку.
class ExchangeServerListScreen extends StatefulWidget {
  const ExchangeServerListScreen({
    super.key,
    required this.servers,
    required this.onChanged,
  });

  final List<ExchangeServer> servers;
  final ValueChanged<List<ExchangeServer>> onChanged;

  @override
  State<ExchangeServerListScreen> createState() => _ExchangeServerListScreenState();
}

class _ExchangeServerListScreenState extends State<ExchangeServerListScreen> {
  late List<ExchangeServer> _servers;

  @override
  void initState() {
    super.initState();
    _servers = [...widget.servers];
  }

  Future<void> _open(ExchangeServer? current) async {
    final result = await Navigator.of(context).push<ExchangeServerFormResult>(
      MaterialPageRoute(builder: (_) => ExchangeServerFormScreen(server: current)),
    );
    if (result == null || !mounted) {
      return;
    }

    final next = [..._servers];
    if (result.deleted) {
      final id = current?.id;
      if (id == null) {
        return;
      }
      next.removeWhere((server) => server.id == id);
    } else {
      final server = result.server;
      if (server == null) {
        return;
      }
      final index = next.indexWhere((item) => item.id == server.id);
      if (index >= 0) {
        next[index] = server;
      } else {
        next.add(server);
      }
    }

    setState(() => _servers = next);
    widget.onChanged(next);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        automaticallyImplyLeading: false,
        title: const Text('Сервера обмена'),
      ),
      body: ListView.separated(
        itemCount: _servers.length,
        separatorBuilder: (_, _) => const Divider(height: 1, color: WebixDarkTheme.border),
        itemBuilder: (context, index) {
          final server = _servers[index];
          final endpoint = server.endpoint;
          return ListTile(
            title: Text(server.name),
            subtitle: endpoint.isEmpty
                ? null
                : Text(endpoint, style: const TextStyle(color: WebixDarkTheme.muted)),
            trailing: IconButton(
              tooltip: 'Изменить',
              onPressed: () => _open(server),
              icon: const Icon(Icons.edit_outlined, color: WebixDarkTheme.text),
            ),
            onTap: () => Navigator.of(context).pop(server.id),
          );
        },
      ),
      bottomNavigationBar: ScreenCommands(
        commands: [
          ScreenCommand(label: 'Создать', accent: true, onPressed: () => _open(null)),
          ScreenCommand(label: 'Закрыть', onPressed: () => Navigator.of(context).pop()),
        ],
      ),
    );
  }
}
