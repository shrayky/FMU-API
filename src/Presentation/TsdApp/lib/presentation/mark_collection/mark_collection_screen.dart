import 'dart:async';

import 'package:flutter/material.dart';

import '../../application/documents/document_loader.dart';
import '../../application/mark/mark_pick.dart';
import '../../application/mark/mark_uploader.dart';
import '../../application/message/message_center.dart';
import '../../domain/documents/document_load_result.dart';
import '../../domain/documents/document_store.dart';
import '../../domain/documents/scanned_document.dart';
import '../../domain/mark/mark_online.dart';
import '../../domain/message/app_message.dart';
import '../../domain/scan/barcode_kind.dart';
import '../../domain/scan/mark_code.dart';
import '../../infrastructure/documents/sqlite_document_store.dart';
import '../../infrastructure/mark/exchange_mark_online.dart';
import '../../infrastructure/scan/clipboard_scan_watcher.dart';
import '../../infrastructure/scan/intent_scan_channel.dart';
import '../../infrastructure/settings/app_settings.dart';
import '../../theme/webix_dark_theme.dart';
import '../scan/camera_scan_page.dart';
import '../scan/scan_session.dart';
import '../settings/exchange_probe_screen.dart';
import '../shell/camera_scan_button.dart';
import '../shell/shell_bottom_bar.dart';
import 'delete_mark_screen.dart';

/// Приглашение на шапке пустого сбора.
const markCollectionHint = 'Для начала подбора марок отсканируйте QR код на документе';

/// Подпись ожидания при загрузке документа.
const documentLoadCaption = 'Загрузка документа';

/// Подпись ожидания проверки марки на сервере.
const markCheckCaption = 'Проверка марки';

/// Скан пришёл раньше, чем экран прочитал состав документа.
const documentNotReady = 'Документ ещё загружается';

/// Отметка выгруженного документа в журнале.
const uploadMark = 'Выгружен';

/// Какие виды штрихкода читает камера: без документа — QR, с документом — марка и EAN-13.
List<BarcodeKind> collectionCameraKinds({required bool documentLoaded}) {
  return documentLoaded
      ? const [BarcodeKind.mark, BarcodeKind.ean13]
      : const [BarcodeKind.document];
}

/// Подпись ожидания выгрузки документа.
String uploadCaption(ScannedDocument document) => 'Выгружаю документ ${document.presentation}';

/// Экран сбора марок. Без документа шапка принимает штрихкод и грузит его с сервера.
class MarkCollectionScreen extends StatefulWidget {
  const MarkCollectionScreen({
    super.key,
    required this.settings,
    this.document,
    this.store,
    this.loader,
    this.uploader,
    this.online,
    this.clipboard,
    this.intent,
  });

  final AppSettings settings;

  /// Открытый документ: состав показывается сразу, штрихкод шапки загрузку не запускает.
  final ScannedDocument? document;

  final DocumentStore? store;
  final DocumentLoader? loader;
  final MarkUploader? uploader;

  /// Сетевые проверки марки: по умолчанию идут в Fmu-Api и на сервер обмена.
  final MarkOnline? online;

  final ClipboardScanWatcher? clipboard;
  final IntentScanChannel? intent;

  @override
  State<MarkCollectionScreen> createState() => _MarkCollectionScreenState();
}

class _MarkCollectionScreenState extends State<MarkCollectionScreen> with WidgetsBindingObserver {
  final _focus = FocusNode();
  final _messages = MessageCenter();
  late final ScanSession _session;
  late final DocumentStore _store;
  late final DocumentLoader _loader;
  late final MarkUploader _uploader;
  late final MarkOnline _online;
  ScannedDocument? _document;
  MarkPick? _pick;

  /// Состав читается асинхронно: скан ждёт его, а не теряется.
  Future<MarkPick?>? _pickReady;

  /// Строка, которую показать на вкладке «Позиция» после закрытия подбора.
  var _positionLine = -1;
  var _tab = 0;

  /// Скан уже обрабатывается: второй код не начинают, пока не записан первый.
  ///
  /// Проверка `Количество` смотрит «Подобрано», а оно меняется только после
  /// записи в SQLite, поэтому параллельные сканы пишут марки сверх состава.
  var _scanRunning = false;

  /// Марки документа не прочитались: подбор не открывается.
  MarkPickUnavailable? _pickFailure;

  bool get _cameraOnTab => widget.settings.cameraScanEnabled && (_tab == 0 || _tab == 2);

  /// Адресная строка ожидания: сервер обмена, иначе адрес Fmu-Api.
  String get _serverLine {
    final server = widget.settings.exchangeServer;
    return server?.endpoint ?? widget.settings.apiBaseUrl.trim();
  }

  /// Отсчёт окна ожидания: таймаут сервера обмена либо таймаут Fmu-Api.
  Duration? get _waitTimeout {
    final server = widget.settings.exchangeServer;
    if (server != null && server.timeoutSeconds > 0) {
      return Duration(seconds: server.timeoutSeconds);
    }
    return widget.settings.apiBaseUrl.trim().isEmpty ? null : ExchangeMarkOnline.fmuTimeout;
  }

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _store = widget.store ?? SqliteDocumentStore();
    _loader = widget.loader ?? DocumentLoader(store: _store);
    _uploader = widget.uploader ?? MarkUploader(store: _store);
    _online = widget.online ?? ExchangeMarkOnline(settings: widget.settings);
    _document = widget.document;
    _session = ScanSession(
      settings: widget.settings,
      clipboard: widget.clipboard,
      intent: widget.intent,
      onCode: _onCode,
    );
    _session.start();
    final document = _document;
    if (document != null) {
      _pickReady = _readComposition(document);
    }
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

  /// Обработка штрихкода: тринадцать цифр — товар, всё остальное — марка или QR документа.
  void _onCode(String code) {
    if (!mounted || code.isEmpty) {
      return;
    }

    final document = _document;
    final product = isProductBarcode(code);
    if (document == null) {
      if (barcodeKindOf(code) != BarcodeKind.document) {
        _rejectKind(code, const [BarcodeKind.document]);
        return;
      }
      unawaited(_loadDocument(code));
      return;
    }

    if (document.uploaded) {
      _messages.showSnack(
        context,
        const AppMessage(kind: MessageKind.warning, text: markDocumentUploaded),
      );
      return;
    }
    if (product) {
      unawaited(_runScan(() => _openPosition(code)));
      return;
    }
    unawaited(_runScan(() => _scanMark(code)));
  }

  /// Сканы идут по одному: подбор видит уже записанные марки, а не прежнее «Подобрано».
  Future<void> _runScan(Future<void> Function() scan) async {
    if (_scanRunning) {
      return;
    }

    _scanRunning = true;
    try {
      await scan();
    } finally {
      _scanRunning = false;
    }
  }

  void _rejectKind(String code, List<BarcodeKind> expected) {
    _messages.showSnack(context, _messages.barcodeWrongType(barcode: code, expected: expected));
  }

  /// Подбора нет: документ ещё читается или марки документа не прочитались.
  void _showNoPick() {
    final failure = _pickFailure;
    _messages.showSnack(
      context,
      failure == null
          ? const AppMessage(kind: MessageKind.warning, text: documentNotReady)
          : AppMessage(
              kind: MessageKind.warning,
              text: MarkPickUnavailable.text,
              data: {'document': _document?.id ?? '', 'error': '${failure.error}'},
            ),
    );
  }

  /// Штрихкод товара из тринадцати цифр только открывает позицию: марку он не пишет.
  Future<void> _openPosition(String code) async {
    final pick = await _readyPick();
    if (!mounted) {
      return;
    }
    if (pick == null) {
      _showNoPick();
      return;
    }

    final lookups = [MarkCode.normalize(code)];
    var found = const <String, String>{};
    try {
      found = await _store.nomenclatureUidsByBarcodes(lookups);
    } catch (_) {
      found = const {};
    }
    if (!mounted) {
      return;
    }

    String? uid;
    for (final lookup in lookups) {
      if (found[lookup] != null) {
        uid = found[lookup];
        break;
      }
    }

    if (uid == null) {
      _messages.showSnack(context, _messages.barcodeNotFound(barcode: code));
      return;
    }

    final index = pick.lineOf(uid);
    if (index < 0) {
      _messages.showSnack(context, _messages.barcodeNotInDocument(barcode: code));
      return;
    }

    setState(() {
      pick.selected = index;
      pick.productOpen = true;
      _positionLine = index;
      _tab = 2;
    });
    _focus.requestFocus();
  }

  /// Марка идёт по порядку подбора: проверки, строка состава, единицы и запись в `marks`.
  Future<void> _scanMark(String code) async {
    final pick = await _readyPick();
    if (!mounted) {
      return;
    }
    if (pick == null) {
      _showNoPick();
      return;
    }

    final result = await _withWait(
      pick,
      code,
      enabled: pick.needsServer,
      caption: markCheckCaption,
    );
    if (result == null || !mounted) {
      return;
    }

    setState(() {
      // Позицию показываем и после полного подбора строки: иначе результат скана не виден.
      if (result.changed || pick.productOpen) {
        _tab = 2;
        _positionLine = pick.selected;
      }
    });
    for (final text in result.messages) {
      _messages.showSnack(
        context,
        AppMessage(kind: MessageKind.warning, text: text, data: {'mark': code}),
      );
    }
    if (result.allPicked) {
      _messages.showSnack(
        context,
        const AppMessage(kind: MessageKind.success, text: markAllPicked),
      );
    }
    _focus.requestFocus();
  }

  /// Скан с проверками на сервере идёт через окно ожидания с отсчётом таймаута.
  Future<MarkScanResult?> _withWait(
    MarkPick pick,
    String code, {
    required bool enabled,
    required String caption,
  }) async {
    if (!enabled) {
      final result = await pick.scan(code);
      return mounted ? result : null;
    }

    _session.pause();
    final result = await Navigator.of(context).push<MarkScanResult>(
      MaterialPageRoute(
        builder: (_) => ExchangeProbeScreen<MarkScanResult>(
          addressLine: _serverLine,
          caption: caption,
          timeout: _waitTimeout,
          run: (cancellation) => pick.scan(code, cancellation: cancellation),
        ),
      ),
    );
    if (!mounted) {
      return null;
    }

    _session.resume();
    _focus.requestFocus();
    return result;
  }

  Future<void> _loadDocument(String barcode) async {
    final server = widget.settings.exchangeServer;
    _session.pause();
    final result = await Navigator.of(context).push<DocumentLoadResult>(
      MaterialPageRoute(
        builder: (_) => ExchangeProbeScreen<DocumentLoadResult>(
          addressLine: server?.endpoint ?? '',
          caption: documentLoadCaption,
          timeout: server == null ? null : Duration(seconds: server.timeoutSeconds),
          run: (cancellation) => _loader.load(widget.settings, barcode, cancellation),
        ),
      ),
    );
    if (!mounted) {
      return;
    }

    _session.resume();
    _focus.requestFocus();
    if (result == null) {
      return;
    }

    final document = result.document;
    if (document != null) {
      setState(() {
        _document = document;
        _tab = 1;
      });
      _pickReady = _readComposition(document);
      await _pickReady;
      return;
    }

    await _messages.show(
      context,
      result.blocked
          ? _messages.documentBlocked(
              error: result.error,
              barcode: barcode,
              endpoint: server?.endpoint ?? '',
            )
          : _messages.documentFailure(
              error: result.error,
              barcode: barcode,
              endpoint: server?.endpoint ?? '',
              details: result.details,
            ),
    );
  }

  /// Читает состав и подбор документа; скан ждёт этот результат через [_readyPick].
  Future<MarkPick?> _readComposition(ScannedDocument document) async {
    _pickFailure = null;
    var names = const <String, String>{};
    try {
      names = await _store.nomenclatureNames(document.nomenclatureUids.toSet());
    } catch (_) {
      names = const {};
    }
    if (!mounted) {
      return null;
    }

    final lines = documentComposition(document, names);
    final barcodes = await _barcodeCount();
    final MarkPick pick;
    try {
      pick = await MarkPick.open(
        document: document,
        composition: lines,
        settings: widget.settings,
        store: _store,
        online: _online,
        trace: _messages.trace,
      );
    } on MarkPickUnavailable catch (error) {
      _pickFailure = error;
      if (mounted) {
        _showNoPick();
      }
      return null;
    }
    if (!mounted) {
      return pick;
    }
    setState(() => _pick = pick);
    _messages.trace('Документ открыт', {
      'документ': document.presentation,
      'строк': lines.length,
      'штрихкодов': barcodes,
    });
    return pick;
  }

  /// Подбор документа: готовый или дожидается чтения состава, если скан пришёл раньше.
  Future<MarkPick?> _readyPick() async {
    final pick = _pick;
    if (pick != null) {
      return pick;
    }

    final ready = _pickReady;
    if (ready == null) {
      return null;
    }
    try {
      return await ready;
    } catch (_) {
      return null;
    }
  }

  Future<int> _barcodeCount() async {
    try {
      return await _store.barcodeCount();
    } catch (_) {
      return -1;
    }
  }

  /// Выгрузка отправляет пакет документа с марками и закрывает сбор при успехе.
  Future<void> _uploadDocument() async {
    final document = _document;
    if (document == null) {
      return;
    }

    final server = widget.settings.exchangeServer;
    _session.pause();
    final result = await Navigator.of(context).push<MarkUploadResult>(
      MaterialPageRoute(
        builder: (_) => ExchangeProbeScreen<MarkUploadResult>(
          addressLine: server?.endpoint ?? '',
          caption: uploadCaption(document),
          timeout: server == null ? null : Duration(seconds: server.timeoutSeconds),
          run: (cancellation) => _uploader.upload(widget.settings, document, cancellation),
        ),
      ),
    );
    if (!mounted) {
      return;
    }

    _session.resume();
    _focus.requestFocus();
    if (result == null) {
      return;
    }
    if (result.uploaded) {
      Navigator.of(context).pop();
      return;
    }

    await _messages.show(
      context,
      AppMessage(
        kind: result.blocked ? MessageKind.warning : MessageKind.failure,
        text: result.error,
        data: {
          'document': document.id,
          if (server != null) 'endpoint': server.endpoint,
        },
      ),
    );
  }

  Future<void> _openCamera() async {
    _session.pause();
    final code = await Navigator.of(context).push<String>(
      MaterialPageRoute(
        builder: (_) => CameraScanPage(
          kinds: collectionCameraKinds(documentLoaded: _document != null),
        ),
      ),
    );
    if (!mounted) {
      return;
    }

    _session.resume();
    _focus.requestFocus();
    if (code == null || code.isEmpty) {
      return;
    }
    _session.submit(code);
  }

  Future<void> _openDelete() async {
    _session.pause();
    final code = await Navigator.of(context).push<String>(
      MaterialPageRoute(builder: (_) => DeleteMarkScreen(settings: widget.settings)),
    );
    if (!mounted) {
      return;
    }

    _session.resume();
    _focus.requestFocus();
    if (code == null || code.isEmpty) {
      return;
    }

    final pick = _pick;
    if (pick == null) {
      return;
    }

    final result = await pick.delete(code);
    if (!mounted) {
      return;
    }
    setState(() {});
    if (result.changed) {
      return;
    }
    for (final text in result.messages) {
      _messages.showSnack(
        context,
        AppMessage(kind: MessageKind.warning, text: text, data: {'mark': code}),
      );
    }
  }

  void _selectTab(int tab) {
    setState(() => _tab = tab);
    _focus.requestFocus();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Сбор марок', style: TextStyle(color: WebixDarkTheme.accent)),
      ),
      body: Focus(
        focusNode: _focus,
        autofocus: true,
        descendantsAreFocusable: false,
        onKeyEvent: _session.onKey,
        child: ColoredBox(
          color: WebixDarkTheme.background,
          child: switch (_tab) {
            0 => _header(),
            1 => _composition(),
            2 => _position(),
            _ => _operations(),
          },
        ),
      ),
      floatingActionButton: _cameraOnTab
          ? CameraScanButton(heroTag: 'collection-camera', onPressed: _openCamera)
          : null,
      bottomNavigationBar: ShellBottomBar(
        items: [
          ShellBarItem(
            label: 'Шапка',
            icon: Icons.receipt_long_outlined,
            selected: _tab == 0,
            onTap: () => _selectTab(0),
          ),
          ShellBarItem(
            label: 'Состав',
            icon: Icons.table_chart_outlined,
            selected: _tab == 1,
            onTap: () => _selectTab(1),
          ),
          ShellBarItem(
            label: 'Позиция',
            icon: Icons.qr_code_2,
            selected: _tab == 2,
            onTap: () => _selectTab(2),
          ),
          ShellBarItem(
            label: 'Меню',
            icon: Icons.menu,
            selected: _tab == 3,
            onTap: () => _selectTab(3),
          ),
          ShellBarItem(
            label: 'Выход',
            icon: Icons.logout,
            onTap: () => Navigator.of(context).pop(),
          ),
        ],
      ),
    );
  }

  Widget _header() {
    final document = _document;
    if (document == null) {
      return const Padding(
        padding: EdgeInsets.all(24),
        child: Align(
          alignment: Alignment.topCenter,
          child: Text(markCollectionHint, textAlign: TextAlign.center),
        ),
      );
    }

    return ListView(
      padding: const EdgeInsets.all(24),
      children: [
        if (document.presentation.isNotEmpty)
          Text(document.presentation, style: const TextStyle(fontSize: 18)),
        if (document.number.isNotEmpty) Text('Номер: ${document.number}'),
        if (document.counterparty.isNotEmpty) Text('Контрагент: ${document.counterparty}'),
        Text('Дата: ${formatDocumentDate(document.scannedAt)}'),
      ],
    );
  }

  Widget _composition() {
    final lines = _pick?.lines ?? const <PickLine>[];
    return Column(
      children: [
        const _Columns(),
        Expanded(
          child: lines.isEmpty
              ? const Center(child: Text('Нет данных для отображения'))
              : ListView.separated(
                  itemCount: lines.length,
                  separatorBuilder: (_, _) => const Divider(height: 1, color: WebixDarkTheme.border),
                  itemBuilder: (context, index) => _CompositionRow(line: lines[index]),
                ),
        ),
        Align(
          alignment: Alignment.centerLeft,
          child: TextButton(onPressed: _openDelete, child: const Text('Удалить марку')),
        ),
      ],
    );
  }

  Widget _position() {
    final pick = _pick;
    if (pick == null || pick.lines.isEmpty) {
      return const Center(child: Text('Отсканируйте штрихкод товара'));
    }

    // Открытая позиция важнее: после скана с полным подбором показываем последнюю строку.
    final open = pick.productOpen;
    final index = open ? pick.selected : _positionLine;
    if (index < 0 || index >= pick.lines.length) {
      return const Center(child: Text('Отсканируйте штрихкод товара'));
    }

    final line = pick.lines[index];
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            children: [
              Expanded(child: _CountCard(title: 'Всего марок', value: formatQuantity(line.quantity))),
              const SizedBox(width: 8),
              Expanded(child: _CountCard(title: 'Подобрано', value: formatQuantity(line.picked))),
            ],
          ),
        ),
        Expanded(
          child: Center(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  line.name,
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontSize: 22,
                    color: line.complete ? WebixDarkTheme.ok : null,
                  ),
                ),
                if (!open && line.complete) ...[
                  const SizedBox(height: 8),
                  const Text('Подобрано полностью', style: TextStyle(color: WebixDarkTheme.ok)),
                ],
              ],
            ),
          ),
        ),
        Align(
          alignment: Alignment.centerLeft,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (open)
                TextButton(
                  onPressed: () => setState(() => pick.productOpen = false),
                  child: const Text('Закончить подбор'),
                ),
              TextButton(
                onPressed: () => setState(() => pick.packages = !pick.packages),
                child: Text(
                  pick.modeTitle,
                  style: const TextStyle(color: WebixDarkTheme.accent),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _operations() {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        const Text('Операции с документом', style: TextStyle(fontSize: 18)),
        Align(
          alignment: Alignment.centerLeft,
          child: TextButton(
            onPressed: _uploadDocument,
            child: const Text('Выгрузить документ'),
          ),
        ),
      ],
    );
  }
}

/// Подписи колонок состава: наименование, под ним количество и количество марок.
class _Columns extends StatelessWidget {
  const _Columns();

  static const _caption = TextStyle(color: WebixDarkTheme.muted, fontSize: 14);

  @override
  Widget build(BuildContext context) {
    return const Padding(
      padding: EdgeInsets.fromLTRB(16, 8, 16, 6),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Наименование', style: _caption),
          SizedBox(height: 2),
          _CountsRow(
            quantity: 'Количество',
            marks: 'Подобрано',
            quantityStyle: _caption,
            marksStyle: _caption,
          ),
        ],
      ),
    );
  }
}

/// Строка состава в две подстроки: наименование, затем количество и подобрано.
///
/// Полностью подобранная строка зелёная с галочкой, начатая — с синим числом.
class _CompositionRow extends StatelessWidget {
  const _CompositionRow({required this.line});

  final PickLine line;

  @override
  Widget build(BuildContext context) {
    final complete = line.complete;
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  line.name,
                  style: TextStyle(
                    fontSize: 18,
                    color: complete ? WebixDarkTheme.ok : null,
                  ),
                ),
              ),
              if (complete)
                const Icon(Icons.check_circle_outline, size: 18, color: WebixDarkTheme.ok),
            ],
          ),
          const SizedBox(height: 6),
          _CountsRow(
            quantity: formatQuantity(line.quantity),
            marks: formatQuantity(line.picked),
            quantityStyle: const TextStyle(color: WebixDarkTheme.muted, fontSize: 16),
            marksStyle: TextStyle(
              fontSize: 18,
              color: complete
                  ? WebixDarkTheme.ok
                  : (line.picked > 0 ? WebixDarkTheme.accent : null),
            ),
          ),
        ],
      ),
    );
  }
}

/// Две колонки чисел: количество и количество марок, обе по правому краю.
class _CountsRow extends StatelessWidget {
  const _CountsRow({
    required this.quantity,
    required this.marks,
    required this.quantityStyle,
    required this.marksStyle,
  });

  /// Ширина колонки марок: по ней же выравнивается заголовок.
  static const marksWidth = 156.0;
  static const gap = 12.0;

  final String quantity;
  final String marks;
  final TextStyle quantityStyle;
  final TextStyle marksStyle;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: Text(quantity, textAlign: TextAlign.right, style: quantityStyle),
        ),
        const SizedBox(width: gap),
        SizedBox(
          width: marksWidth,
          child: Text(marks, textAlign: TextAlign.right, style: marksStyle),
        ),
      ],
    );
  }
}

/// Карточка количества на экране позиции.
class _CountCard extends StatelessWidget {
  const _CountCard({required this.title, required this.value});

  final String title;
  final String value;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: WebixDarkTheme.surface,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              title,
              textAlign: TextAlign.center,
              style: const TextStyle(color: WebixDarkTheme.muted, fontSize: 14),
            ),
            const SizedBox(height: 4),
            Text(
              value,
              textAlign: TextAlign.center,
              style: const TextStyle(fontSize: 22),
            ),
          ],
        ),
      ),
    );
  }
}
