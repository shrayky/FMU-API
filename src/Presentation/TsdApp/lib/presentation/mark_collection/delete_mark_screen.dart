import 'package:flutter/material.dart';

import '../../application/message/message_center.dart';
import '../../domain/message/app_message.dart';
import '../../domain/scan/barcode_kind.dart';
import '../../infrastructure/settings/app_settings.dart';
import '../../theme/webix_dark_theme.dart';
import '../scan/camera_scan_page.dart';
import '../scan/scan_session.dart';
import '../shell/camera_scan_button.dart';
import '../shell/screen_commands.dart';

/// Удаление марки из состава. Отмена возвращает без изменений.
class DeleteMarkScreen extends StatefulWidget {
  const DeleteMarkScreen({super.key, required this.settings});

  final AppSettings settings;

  @override
  State<DeleteMarkScreen> createState() => _DeleteMarkScreenState();
}

class _DeleteMarkScreenState extends State<DeleteMarkScreen> with WidgetsBindingObserver {
  final _focus = FocusNode();
  final _messages = MessageCenter();
  late final ScanSession _session;
  var _closed = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _session = ScanSession(settings: widget.settings, onCode: _done);
    _session.start();
    WidgetsBinding.instance.addPostFrameCallback((_) => _focus.requestFocus());
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _session.dispose();
    _focus.dispose();
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    _session.onLifecycle(state);
  }

  void _done(String code) {
    if (_closed || !mounted || code.isEmpty) {
      return;
    }

    if (isProductBarcode(code)) {
      _messages.showSnack(
        context,
        AppMessage(kind: MessageKind.warning, text: 'Это не марка', data: {'barcode': code}),
      );
      return;
    }

    _closed = true;
    Navigator.of(context).pop(code);
  }

  Future<void> _openCamera() async {
    _session.pause();
    final code = await Navigator.of(context).push<String>(
      MaterialPageRoute(builder: (_) => CameraScanPage(kinds: const [BarcodeKind.mark])),
    );
    if (!mounted) {
      return;
    }
    if (code == null || code.isEmpty) {
      _session.resume();
      _focus.requestFocus();
      return;
    }
    _session.submit(code);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        automaticallyImplyLeading: false,
        title: const Text('Удаление марки'),
      ),
      body: Focus(
        focusNode: _focus,
        autofocus: true,
        onKeyEvent: _session.onKey,
        child: const ColoredBox(
          color: WebixDarkTheme.background,
          child: Center(child: Text('Отсканируйте марку')),
        ),
      ),
      floatingActionButton: widget.settings.cameraScanEnabled
          ? CameraScanButton(heroTag: 'delete-mark-camera', onPressed: _openCamera)
          : null,
      bottomNavigationBar: ScreenCommands(
        commands: [
          ScreenCommand(
            label: 'Отмена',
            onPressed: () {
              if (_closed) {
                return;
              }
              _closed = true;
              Navigator.of(context).pop();
            },
          ),
        ],
      ),
    );
  }
}
