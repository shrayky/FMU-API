import 'dart:async';

import 'package:flutter/material.dart';

import '../../application/message/message_center.dart';
import '../../domain/documents/document_store.dart';
import '../../domain/documents/scanned_document.dart';
import '../../domain/documents/stored_mark.dart';
import '../../domain/documents/write_off_api.dart';
import '../../domain/message/app_message.dart';
import '../../domain/message/failure_text.dart';
import '../../domain/scan/barcode_kind.dart';
import '../../domain/scan/mark_code.dart';
import '../../infrastructure/api/fmu_api_client.dart';
import '../../infrastructure/documents/sqlite_document_store.dart';
import '../../infrastructure/scan/clipboard_scan_watcher.dart';
import '../../infrastructure/scan/intent_scan_channel.dart';
import '../../infrastructure/settings/app_settings.dart';
import '../../infrastructure/settings/settings_store.dart';
import '../../theme/webix_dark_theme.dart';
import '../scan/camera_scan_page.dart';
import '../scan/scan_session.dart';
import '../shell/camera_scan_button.dart';
import '../shell/screen_commands.dart';

/// Подпись выгруженного документа на экране списания.
const writeOffUploadedMark = 'Выгружен';

/// В документе нечего выгружать.
const writeOffNoMarks = 'В документе нет ни одной марки';

/// Пустой адрес Fmu-Api: документ не создаётся.
const writeOffNoAddress = 'Укажите адрес Fmu-Api в настройках';

/// Повтор марки ловится по КИ без криптохвоста.
const writeOffDuplicateMark = 'Эта марка уже отсканирована!';

/// Сканировали не марку.
const writeOffNotMark = 'Это не марка';

/// Организация не выбрана: документ марки не принимает.
const writeOffNoOrganisation = 'Укажите организацию';

/// Выгруженный документ скан не пишет.
const writeOffUploadedLock = 'Документ уже выгружен';

/// Экран документа списания марок: тип, таблица товаров, скан и выгрузка.
class MarkWriteOffScreen extends StatefulWidget {
  const MarkWriteOffScreen({
    super.key,
    required this.settings,
    this.document,
    this.create = false,
    this.store,
    this.api,
    this.clipboard,
    this.intent,
  });

  final AppSettings settings;

  /// Открытый документ списания: новый создаётся только с флагом [create].
  final ScannedDocument? document;

  /// Кнопка меню и «+» журнала создают новый пустой документ.
  final bool create;

  final DocumentStore? store;
  final WriteOffApi? api;
  final ClipboardScanWatcher? clipboard;
  final IntentScanChannel? intent;

  @override
  State<MarkWriteOffScreen> createState() => _MarkWriteOffScreenState();
}

class _MarkWriteOffScreenState extends State<MarkWriteOffScreen> with WidgetsBindingObserver {
  final _focus = FocusNode();
  final _messages = MessageCenter();
  late final ScanSession _session;
  late final DocumentStore _store;
  WriteOffApi? _api;

  ScannedDocument? _document;
  var _reason = packingReason;
  var _marks = <StoredMark>[];
  var _organisations = <WriteOffOrganisation>[];
  WriteOffOrganisation? _organisation;
  var _missingOrganisation = '';

  var _scanRunning = false;
  var _uploading = false;
  var _editing = false;

  String get _address => widget.settings.apiBaseUrl.trim();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _store = widget.store ?? SqliteDocumentStore();
    _api = widget.api ?? (_address.isEmpty ? null : FmuApiClient(baseUrl: _address));
    _document = widget.document;
    _reason = widget.document?.reason ?? packingReason;
    _session = ScanSession(
      settings: widget.settings,
      clipboard: widget.clipboard,
      intent: widget.intent,
      onCode: _onCode,
    );
    _session.start();
    final document = _document;
    if (document != null) {
      unawaited(_loadDocument(document));
    } else if (widget.create && _address.isNotEmpty) {
      unawaited(_createDocument());
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

  /// Новый пустой документ: строку пишет только заполненный адрес Fmu-Api.
  Future<void> _createDocument() async {
    try {
      final number = await SettingsStore().takeWriteOffNumber(widget.settings.writeOffNumberPrefix);
      final document = await _store.createWriteOff(scannedAt: DateTime.now(), number: number);
      if (!mounted) {
        return;
      }
      setState(() => _document = document);
      await _loadDocument(document);
    } catch (error) {
      if (!mounted) {
        return;
      }
      _messages.showSnack(
        context,
        AppMessage(kind: MessageKind.failure, text: 'Документ не создан: $error'),
      );
    }
  }

  /// Марки и причина читаются из базы; сведения о товаре — только для новых строк.
  Future<void> _loadDocument(ScannedDocument document) async {
    var marks = <StoredMark>[];
    try {
      marks = await _store.marks(document.id);
    } catch (_) {
      marks = <StoredMark>[];
    }
    if (!mounted) {
      return;
    }

    setState(() {
      _document = document;
      _reason = document.reason;
      _marks = marks;
    });

    await _loadOrganisations(document);

    for (final mark in marks) {
      final needsName = mark.name.isEmpty;
      final needsPrice = _reason == packingReason && mark.price == null;
      if (needsName || needsPrice) {
        await _loadMarkInfo(mark);
      }
    }
  }

  /// Организации из настроек Fmu-Api: одна — сразу выбранная, несколько — по выбору оператора.
  Future<void> _loadOrganisations(ScannedDocument document) async {
    final api = _api;
    if (api == null) {
      return;
    }

    final organisations = await api.organisations();
    if (!mounted) {
      return;
    }

    final savedId = document.printGroupId;
    final savedInn = document.inn;
    final hadOrganisation = savedId > 0 || savedInn.isNotEmpty;
    var selected = savedId > 0 ? _findOrganisationById(organisations, savedId) : null;
    if (selected == null && savedId <= 0 && savedInn.isNotEmpty) {
      selected = _findOrganisationByInn(organisations, savedInn);
    }
    if (selected == null && !hadOrganisation && organisations.length == 1) {
      selected = organisations.first;
    }

    setState(() {
      _organisations = organisations;
      _organisation = selected;
      _missingOrganisation = selected == null ? document.organisationName : '';
      if (selected != null) {
        _document = _withOrganisation(document, selected);
      }
    });

    if (selected != null) {
      await _saveOrganisation(document, selected);
    }
  }

  /// Выбор оператора: печатная группа, ИНН и название сохраняются в теле документа.
  Future<void> _selectOrganisation(WriteOffOrganisation organisation) async {
    final document = _document;
    final current = _organisation;
    if (document == null ||
        (current != null && current.id == organisation.id && current.inn == organisation.inn)) {
      return;
    }

    await _saveOrganisation(document, organisation);
    if (!mounted) {
      return;
    }
    setState(() {
      _organisation = organisation;
      _document = _withOrganisation(document, organisation);
    });

    // Строки, отсканированные до выбора организации, дозаполняем названием и массой.
    for (final mark in [..._marks]) {
      if (mark.name.isEmpty) {
        await _loadMarkInfo(mark);
      }
    }
  }

  Future<void> _saveOrganisation(
    ScannedDocument document,
    WriteOffOrganisation organisation,
  ) async {
    if (document.printGroupId == organisation.id &&
        document.inn == organisation.inn &&
        document.organisationName == organisation.name) {
      return;
    }

    try {
      await _store.setOrganisation(
        document.id,
        inn: organisation.inn,
        name: organisation.name,
        printGroupId: organisation.id,
      );
    } catch (_) {}
  }

  ScannedDocument _withOrganisation(ScannedDocument document, WriteOffOrganisation organisation) {
    return ScannedDocument(
      id: document.id,
      scannedAt: document.scannedAt,
      number: document.number,
      counterparty: document.counterparty,
      presentation: document.presentation,
      erpUid: document.erpUid,
      body: {
        ...document.body,
        'inn': organisation.inn,
        'organisation': organisation.name,
        'printGroupId': organisation.id,
      },
      uploaded: document.uploaded,
      kind: document.kind,
    );
  }

  static WriteOffOrganisation? _findOrganisationById(
    List<WriteOffOrganisation> organisations,
    int id,
  ) {
    for (final organisation in organisations) {
      if (organisation.id == id) {
        return organisation;
      }
    }
    return null;
  }

  static WriteOffOrganisation? _findOrganisationByInn(
    List<WriteOffOrganisation> organisations,
    String inn,
  ) {
    for (final organisation in organisations) {
      if (organisation.inn == inn) {
        return organisation;
      }
    }
    return null;
  }

  void _onCode(String code) {
    if (!mounted || code.isEmpty || _editing) {
      return;
    }

    final document = _document;
    if (document == null) {
      _messages.showSnack(
        context,
        const AppMessage(kind: MessageKind.warning, text: writeOffNoAddress),
      );
      return;
    }
    if (document.uploaded) {
      _messages.showSnack(
        context,
        const AppMessage(kind: MessageKind.warning, text: writeOffUploadedLock),
      );
      return;
    }
    if (!MarkCode.looksLikeMark(code)) {
      _messages.showSnack(
        context,
        AppMessage(kind: MessageKind.warning, text: writeOffNotMark, data: {'barcode': code}),
      );
      return;
    }

    // Марка без организации не принимается: ИНН нужен для сведений КИ и акта.
    if (_organisation == null) {
      _messages.showSnack(
        context,
        const AppMessage(kind: MessageKind.warning, text: writeOffNoOrganisation),
      );
      return;
    }

    unawaited(_scanMark(code));
  }

  /// Сканы идут по одному: повтор видит уже записанную марку, а не прежний список.
  Future<void> _scanMark(String code) async {
    if (_scanRunning) {
      return;
    }

    final key = markCis(code);
    if (_marks.any((mark) => mark.cis == key)) {
      _messages.showSnack(
        context,
        AppMessage(kind: MessageKind.warning, text: writeOffDuplicateMark, data: {'mark': code}),
      );
      return;
    }

    _scanRunning = true;
    try {
      final document = _document!;
      var mark = StoredMark(mark: markStorageText(code), nomenclatureUid: '');
      await _store.addMark(document.id, mark);
      if (!mounted) {
        return;
      }
      setState(() => _marks = [..._marks, mark]);

      final info = await _markInfo(mark);
      if (info != null) {
        mark = _withInfo(mark, info);
        await _store.updateMark(document.id, mark);
        if (!mounted) {
          return;
        }
        setState(() => _replaceMark(mark));
      }

      if (_reason == packingReason) {
        await _requestPrice(mark, info?.gtin);
      }
    } catch (error) {
      if (!mounted) {
        return;
      }
      _messages.showSnack(
        context,
        AppMessage(kind: MessageKind.failure, text: 'Марка не записана: $error'),
      );
    } finally {
      _scanRunning = false;
    }
  }

  /// Название и масса марки из сведений КИ; без них строка остаётся с кодом марки.
  Future<WriteOffMarkInfo?> _markInfo(StoredMark mark) async {
    final api = _api;
    final organisation = _organisation;
    if (api == null || organisation == null) {
      return null;
    }

    return api.markInfo(inn: organisation.inn, mark: mark.mark);
  }

  /// Фасовка показывает цену из Frontol; истечение срока запрос не делает.
  Future<void> _requestPrice(StoredMark mark, String? markGtin) async {
    final api = _api;
    if (api == null || _reason != packingReason || mark.price != null) {
      return;
    }

    final gtin = (markGtin ?? '').isNotEmpty ? markGtin! : MarkCode.gtin(mark.mark);
    if (gtin.isEmpty) {
      return;
    }

    final price = await api.retailPriceByBarcode(gtin);
    if (!mounted || price == null) {
      return;
    }

    await _saveMark(mark.copyWith(price: price));
  }

  /// Пишет строку в базу и обновляет её в таблице.
  Future<void> _saveMark(StoredMark mark) async {
    final document = _document;
    if (document == null) {
      return;
    }

    try {
      await _store.updateMark(document.id, mark);
    } catch (_) {
      return;
    }
    if (!mounted) {
      return;
    }
    setState(() => _replaceMark(mark));
  }

  /// Цена для марок, у которых её ещё нет: смена типа и открытие документа.
  Future<void> _loadPrices() async {
    for (final mark in [..._marks]) {
      await _requestPrice(mark, null);
    }
  }

  void _replaceMark(StoredMark mark) {
    _marks = [for (final item in _marks) item.cis == mark.cis ? mark : item];
  }

  /// Название и масса из сведений КИ; уже сохранённое название не запрашивается.
  StoredMark _withInfo(StoredMark mark, WriteOffMarkInfo info) {
    final quantity = info.quantity;

    return mark.copyWith(
      name: info.name.isEmpty ? mark.name : info.name,
      quantity: quantity != null && quantity > 0 ? quantity : mark.quantity,
    );
  }

  /// Дозаполняет строку без названия или цены: сохранённые значения не перетираются.
  Future<void> _loadMarkInfo(StoredMark mark) async {
    var current = mark;

    if (current.name.isEmpty) {
      final info = await _markInfo(current);
      if (!mounted) {
        return;
      }

      if (info != null) {
        current = _withInfo(current, info);
        await _saveMark(current);
        if (!mounted) {
          return;
        }

        if (_reason == packingReason && current.price == null) {
          await _requestPrice(current, info.gtin);
        }
        return;
      }
    }

    if (_reason == packingReason && current.price == null) {
      await _requestPrice(current, null);
    }
  }

  Future<void> _setReason(String reason) async {
    final document = _document;
    if (document == null || reason == _reason) {
      return;
    }

    try {
      await _store.setReason(document.id, reason: reason);
    } catch (_) {
      return;
    }
    if (!mounted) {
      return;
    }
    setState(() => _reason = reason);
    if (reason == packingReason) {
      await _loadPrices();
    }
  }

  /// Оператор правит количество и цену строки: значения уходят в акт при выгрузке.
  Future<void> _editMark(StoredMark mark) async {
    final document = _document;
    if (document == null || document.uploaded) {
      return;
    }

    _editing = true;
    _session.pause();
    final values = await showDialog<_MarkValues>(
      context: context,
      builder: (_) => _MarkValuesDialog(
        title: mark.name,
        quantity: mark.quantity,
        price: mark.price,
        withPrice: _reason == packingReason,
      ),
    );
    if (!mounted) {
      return;
    }

    _editing = false;
    _session.resume();
    _focus.requestFocus();
    if (values == null) {
      return;
    }

    final updated = StoredMark(
      mark: mark.mark,
      nomenclatureUid: mark.nomenclatureUid,
      units: mark.units,
      quantity: values.quantity,
      price: values.price,
      name: mark.name,
    );
    try {
      await _store.updateMark(document.id, updated);
      if (!mounted) {
        return;
      }
      setState(() => _replaceMark(updated));
    } catch (error) {
      if (!mounted) {
        return;
      }
      _messages.showSnack(
        context,
        AppMessage(kind: MessageKind.failure, text: 'Строка не сохранена: $error'),
      );
    }
  }

  Future<void> _deleteMark(StoredMark mark) async {
    final document = _document;
    if (document == null || document.uploaded) {
      return;
    }

    try {
      await _store.removeMark(document.id, mark.mark);
      if (!mounted) {
        return;
      }
      setState(() {
        _marks = [for (final item in _marks) if (item.cis != mark.cis) item];
      });
    } catch (error) {
      if (!mounted) {
        return;
      }
      _messages.showSnack(
        context,
        AppMessage(kind: MessageKind.failure, text: 'Строка не удалена: $error'),
      );
    }
  }

  /// Выгрузка создаёт черновик; отметка выгрузки ставится только по ответу 200.
  Future<void> _upload() async {
    final document = _document;
    if (document == null || _uploading || document.uploaded) {
      return;
    }
    if (_marks.isEmpty) {
      _messages.showSnack(
        context,
        const AppMessage(kind: MessageKind.warning, text: writeOffNoMarks),
      );
      return;
    }

    final api = _api;
    if (api == null) {
      _messages.showSnack(
        context,
        const AppMessage(kind: MessageKind.warning, text: writeOffNoAddress),
      );
      return;
    }

    final organisation = _organisation;
    if (organisation == null) {
      _messages.showSnack(
        context,
        const AppMessage(kind: MessageKind.warning, text: writeOffNoOrganisation),
      );
      return;
    }

    setState(() => _uploading = true);
    try {
      final terminalId = await SettingsStore().terminalId();
      await api.createReceipt(
        inn: organisation.inn,
        reason: _reason,
        printGroupId: organisation.id,
        uid: document.id,
        number: document.number,
        terminalId: terminalId,
        lines: [
          for (final mark in _marks)
            WriteOffReceiptLine(
              mark: mark.mark,
              quantity: mark.quantity,
              price: _reason == packingReason ? mark.price : null,
            ),
        ],
      );
      await _store.setUploaded(document.id, uploaded: true);
      if (!mounted) {
        return;
      }
      setState(() {
        _document = ScannedDocument(
          id: document.id,
          scannedAt: document.scannedAt,
          number: document.number,
          counterparty: document.counterparty,
          presentation: document.presentation,
          erpUid: document.erpUid,
          body: {...document.body, 'reason': _reason},
          uploaded: true,
          kind: writeOffKind,
        );
      });
      _messages.showSnack(
        context,
        const AppMessage(kind: MessageKind.success, text: 'Документ выгружен'),
      );
    } catch (error) {
      if (!mounted) {
        return;
      }
      _messages.showSnack(
        context,
        AppMessage(
          kind: MessageKind.failure,
          text: _uploadError(error),
          data: {'document': document.id, 'error': '$error'},
        ),
      );
    } finally {
      if (mounted) {
        setState(() => _uploading = false);
      }
    }
  }

  /// Старый Fmu-Api не знает строк списания и отвечает, что марок нет.
  String _uploadError(Object error) {
    final raw = '$error';
    if (raw.contains('В документе нет ни одной марки')) {
      return 'Сервер не принял строки списания. Обновите Fmu-Api и повторите выгрузку';
    }

    return describeFailure(error).replaceFirst(RegExp(r'^Exception:\s*'), '');
  }

  Future<void> _openCamera() async {
    _session.pause();
    final code = await Navigator.of(context).push<String>(
      MaterialPageRoute(builder: (_) => CameraScanPage(kinds: const [BarcodeKind.mark])),
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

  @override
  Widget build(BuildContext context) {
    final document = _document;
    final uploaded = document?.uploaded ?? false;
    return Stack(
      children: [
        Scaffold(
          appBar: AppBar(
            title: const Text('Списание марок', style: TextStyle(color: WebixDarkTheme.accent)),
            actions: [
              if (document != null)
                Padding(
                  padding: const EdgeInsets.only(right: 16),
                  child: Center(
                    child: Text(
                      document.number.isEmpty
                          ? formatDocumentDate(document.scannedAt)
                          : '${document.number}  ${formatDocumentDate(document.scannedAt)}',
                      style: const TextStyle(color: WebixDarkTheme.muted, fontSize: 14),
                    ),
                  ),
                ),
            ],
          ),
          body: Focus(
            focusNode: _focus,
            autofocus: true,
            descendantsAreFocusable: false,
            onKeyEvent: _session.onKey,
            child: ColoredBox(color: WebixDarkTheme.background, child: _content()),
          ),
          floatingActionButton: widget.settings.cameraScanEnabled
              ? CameraScanButton(heroTag: 'write-off-camera', onPressed: _openCamera)
              : null,
          bottomNavigationBar: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (uploaded)
                const Material(
                  color: WebixDarkTheme.surface,
                  child: SizedBox(
                    width: double.infinity,
                    child: Padding(
                      padding: EdgeInsets.symmetric(vertical: 6),
                      child: Text(
                        writeOffUploadedMark,
                        textAlign: TextAlign.center,
                        style: TextStyle(color: WebixDarkTheme.ok),
                      ),
                    ),
                  ),
                ),
              ScreenCommands(
                commands: [
                  ScreenCommand(
                    label: 'Выгрузить',
                    accent: true,
                    onPressed: _uploading || uploaded ? null : _upload,
                  ),
                  ScreenCommand(label: 'Выход', onPressed: () => Navigator.of(context).pop()),
                ],
              ),
            ],
          ),
        ),
        if (_uploading) const _UploadProgress(),
      ],
    );
  }

  Widget _content() {
    final document = _document;
    if (document == null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Text(
            _address.isEmpty ? writeOffNoAddress : 'Документ не создан',
            textAlign: TextAlign.center,
          ),
        ),
      );
    }

    final showPrice = _reason == packingReason;
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
          child: Row(
            children: [
              const Text('Организация:'),
              const SizedBox(width: 12),
              Expanded(child: _organisationSelector()),
            ],
          ),
        ),
        if (_missingOrganisation.isNotEmpty)
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 4, 16, 0),
            child: Align(
              alignment: Alignment.centerLeft,
              child: Text(
                'Была: $_missingOrganisation',
                style: const TextStyle(color: WebixDarkTheme.muted, fontSize: 12),
              ),
            ),
          ),
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 4, 16, 4),
          child: Row(
            children: [
              const Text('Тип:'),
              const SizedBox(width: 12),
              _reasonSelector(),
            ],
          ),
        ),
        if (_marks.isNotEmpty) _columns(showPrice),
        Expanded(
          child: _marks.isEmpty
              ? Center(
                  child: Text(
                    document.uploaded ? writeOffUploadedLock : 'Отсканируйте марку',
                    textAlign: TextAlign.center,
                  ),
                )
              : ListView.separated(
                  padding: const EdgeInsets.fromLTRB(8, 0, 8, 16),
                  itemCount: _marks.length,
                  separatorBuilder: (_, _) => const Divider(height: 1, color: WebixDarkTheme.border),
                  itemBuilder: (context, index) {
                    final mark = _marks[index];
                    return _MarkRow(
                      mark: mark,
                      name: mark.name,
                      showPrice: showPrice,
                      locked: document.uploaded,
                      onEdit: () => unawaited(_editMark(mark)),
                      onDelete: () => unawaited(_deleteMark(mark)),
                    );
                  },
                ),
        ),
      ],
    );
  }

  Widget _columns(bool showPrice) {
    const caption = TextStyle(color: WebixDarkTheme.muted, fontSize: 12);
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 6),
      child: Row(
        children: [
          const Expanded(child: Text('Товар', style: caption)),
          const SizedBox(
            width: _MarkRow.quantityWidth,
            child: Text('Кол-во', textAlign: TextAlign.right, style: caption),
          ),
          if (showPrice)
            const SizedBox(
              width: _MarkRow.priceWidth,
              child: Text('Цена', textAlign: TextAlign.right, style: caption),
            ),
          const SizedBox(width: _MarkRow.actionWidth),
        ],
      ),
    );
  }

  Widget _organisationSelector() {
    final selected = _organisation;
    final items = [..._organisations];

    if (selected != null &&
        !items.any((organisation) => _organisationKey(organisation) == _organisationKey(selected))) {
      items.insert(0, selected);
    }

    return DropdownButton<String>(
      key: const Key('write-off-organisation'),
      isExpanded: true,
      value: selected == null ? null : _organisationKey(selected),
      underline: const SizedBox.shrink(),
      dropdownColor: WebixDarkTheme.surface,
      hint: const Text(
        writeOffNoOrganisation,
        overflow: TextOverflow.ellipsis,
      ),
      items: [
        for (final organisation in items)
          DropdownMenuItem(
            value: _organisationKey(organisation),
            child: Text(organisation.label, overflow: TextOverflow.ellipsis),
          ),
      ],
      onChanged: (key) {
        for (final organisation in items) {
          if (_organisationKey(organisation) == key) {
            unawaited(_selectOrganisation(organisation));
            return;
          }
        }
      },
    );
  }

  /// Ключ выбора: id печатной группы, а без него — ИНН.
  static String _organisationKey(WriteOffOrganisation organisation) {
    return organisation.id > 0 ? 'id:${organisation.id}' : 'inn:${organisation.inn}';
  }

  Widget _reasonSelector() {
    return DropdownButton<String>(
      key: const Key('write-off-reason'),
      value: _reason,
      underline: const SizedBox.shrink(),
      dropdownColor: WebixDarkTheme.surface,
      items: [
        for (final reason in const [packingReason, expirationReason])
          DropdownMenuItem(value: reason, child: Text(writeOffReasonLabel(reason))),
      ],
      onChanged: (reason) {
        if (reason != null) {
          unawaited(_setReason(reason));
        }
      },
    );
  }
}

/// Строка таблицы: название товара, количество, цена и удаление.
class _MarkRow extends StatelessWidget {
  const _MarkRow({
    required this.mark,
    required this.name,
    required this.showPrice,
    required this.locked,
    required this.onEdit,
    required this.onDelete,
  });

  static const quantityWidth = 64.0;
  static const priceWidth = 76.0;
  static const actionWidth = 48.0;

  final StoredMark mark;
  final String name;
  final bool showPrice;
  final bool locked;
  final VoidCallback onEdit;
  final VoidCallback onDelete;

  @override
  Widget build(BuildContext context) {
    final code = MarkCode.shortMark(mark.mark);
    final gtin = MarkCode.gtin(mark.mark);
    final title = name.isNotEmpty
        ? name
        : (gtin.isEmpty ? code : 'GTIN $gtin');
    return InkWell(
      onTap: locked ? null : onEdit,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(8, 8, 8, 8),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.center,
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(title, maxLines: 2, overflow: TextOverflow.ellipsis),
                  if (title != code)
                    Text(
                      code,
                      style: const TextStyle(color: WebixDarkTheme.muted, fontSize: 12),
                    ),
                ],
              ),
            ),
            SizedBox(
              width: quantityWidth,
              child: Text(
                formatQuantity(mark.quantity),
                textAlign: TextAlign.right,
              ),
            ),
            if (showPrice)
              SizedBox(
                width: priceWidth,
                child: Text(
                  mark.price == null ? '—' : formatPrice(mark.price!),
                  textAlign: TextAlign.right,
                ),
              ),
            SizedBox(
              width: actionWidth,
              child: locked
                  ? const SizedBox.shrink()
                  : IconButton(
                      onPressed: onDelete,
                      tooltip: 'Удалить марку',
                      icon: const Icon(Icons.delete_outline, size: 20),
                    ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Значения строки из диалога правки.
class _MarkValues {
  const _MarkValues({required this.quantity, this.price});

  final num quantity;
  final num? price;
}

/// Диалог правки количества и цены: цена нужна только фасовке.
class _MarkValuesDialog extends StatefulWidget {
  const _MarkValuesDialog({
    required this.title,
    required this.quantity,
    required this.price,
    required this.withPrice,
  });

  final String title;
  final num quantity;
  final num? price;
  final bool withPrice;

  @override
  State<_MarkValuesDialog> createState() => _MarkValuesDialogState();
}

class _MarkValuesDialogState extends State<_MarkValuesDialog> {
  late final TextEditingController _quantity = TextEditingController(
    text: formatQuantity(widget.quantity),
  );
  late final TextEditingController _price = TextEditingController(
    text: widget.price == null ? '' : formatPrice(widget.price!),
  );
  var _error = '';

  @override
  void dispose() {
    _quantity.dispose();
    _price.dispose();
    super.dispose();
  }

  void _save() {
    final quantity = _number(_quantity.text);
    if (quantity == null || quantity <= 0) {
      setState(() => _error = 'Укажите количество больше нуля');
      return;
    }

    final rawPrice = _price.text.trim();
    final price = widget.withPrice ? _number(rawPrice) : null;
    if (widget.withPrice && rawPrice.isNotEmpty && (price == null || price < 0)) {
      setState(() => _error = 'Цена должна быть числом');
      return;
    }

    Navigator.of(context).pop(_MarkValues(quantity: quantity, price: price));
  }

  static num? _number(String text) {
    final normalized = text.trim().replaceAll(',', '.');
    if (normalized.isEmpty) {
      return null;
    }
    return num.tryParse(normalized);
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      backgroundColor: WebixDarkTheme.dialogSurface,
      title: Text(widget.title.isEmpty ? 'Марка' : widget.title),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          TextField(
            key: const Key('write-off-quantity'),
            controller: _quantity,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: const InputDecoration(labelText: 'Количество'),
          ),
          if (widget.withPrice)
            TextField(
              key: const Key('write-off-price'),
              controller: _price,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: const InputDecoration(labelText: 'Цена'),
            ),
          if (_error.isNotEmpty)
            Padding(
              padding: const EdgeInsets.only(top: 12),
              child: Text(_error, style: const TextStyle(color: WebixDarkTheme.warn)),
            ),
        ],
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Отмена'),
        ),
        TextButton(onPressed: _save, child: const Text('Сохранить')),
      ],
    );
  }
}

/// Спиннер поверх экрана, пока идёт выгрузка: поля и кнопки недоступны.
class _UploadProgress extends StatelessWidget {
  const _UploadProgress();

  @override
  Widget build(BuildContext context) {
    return const Positioned.fill(
      child: Material(
        color: Color(0x99000000),
        child: Center(
          child: SizedBox(
            width: 48,
            height: 48,
            child: CircularProgressIndicator(strokeWidth: 4),
          ),
        ),
      ),
    );
  }
}

/// Цена без лишних нулей: целая — без дробной части, иначе два знака.
String formatPrice(num price) {
  if (price == price.truncateToDouble()) {
    return '${price.toInt()}';
  }
  return price.toStringAsFixed(2);
}
