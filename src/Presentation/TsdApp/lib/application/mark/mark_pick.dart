import '../../domain/documents/document_store.dart';
import '../../domain/documents/scanned_document.dart';
import '../../domain/documents/stored_mark.dart';
import '../../domain/exchange/exchange_cancellation.dart';
import '../../domain/mark/mark_online.dart';
import '../../infrastructure/settings/app_settings.dart';

/// Позиция не открыта: сначала сканируют штрихкод товара.
const markNeedsBarcode = 'Сначала нужно отсканировать штрихкод товара!';

/// Та же марка уже лежит в документе.
const markAlreadyScanned = 'Эта марка уже отсканирована!';

/// В режиме «Упаковки» штучная марка не подходит.
const markIndividualForbidden = 'Нельзя сканировать индивидуальные марки!';

/// Марка закрывает больше единиц, чем осталось по строке состава.
const markOverflow = 'С этой маркой вы отсканируете больше чем нужно!';

/// Марки нет в документе, удалять нечего.
const markNotInDocument = 'Такой марки нет в документе!';

/// Документ уже ушёл на сервер: подбор и удаление закрыты.
const markDocumentUploaded = 'Документ уже выгружен';

/// Все строки состава закрыты.
const markAllPicked = 'Все марки подобраны!';

/// Все марки упаковки уже есть в документе.
const markPackageAllPicked = 'Все марки упаковки уже подобраны';

/// Часть марок упаковки уже есть в документе: записаны только новые.
String markPackagePartlyPicked({required int skipped, required int added}) =>
    'Часть марок упаковки уже подобрана: пропущено $skipped, записано $added';

/// Режим «Единицы и упаковки»: индивидуальные марки тоже пишутся.
const unitsAndPackagesMode = 'Единицы и упаковки';

/// Режим «Упаковки»: штучные марки запрещены.
const packagesMode = 'Упаковки';

/// Имя API проверки марки, при котором запрашивается упаковка.
const trueApiCheckApi = 'True api';

/// Товар со штрихкодом не нашёлся в справочнике документов.
String markBarcodeUnknown(String barcode) => 'Товар со штрихкодом $barcode не найден';

/// Товар есть в справочнике, но его нет в составе этого документа.
String markNotInComposition({required String name, required String barcode}) =>
    'Товара $name со штрихкодом $barcode нет в составе этого документа!';

/// Штрихкод из марки указывает на товар, которого нет в справочнике.
String markForOtherProduct({required String name, required String barcode}) =>
    'Эта марка не для $name! Штрихкод $barcode';

/// Марки документа не прочитались: подбор не открывается.
///
/// С нулём в «Подобрано» скан прошёл бы проверку `Количество` и записал марки
/// сверх состава, поэтому документ остаётся закрытым до повторного открытия.
class MarkPickUnavailable implements Exception {
  const MarkPickUnavailable(this.error);

  /// Текст сбоя базы: пользователю показывается [text], подробность — в журнале.
  final Object error;

  static const text = 'Не удалось прочитать марки документа';

  @override
  String toString() => '$text: $error';
}

/// Строка состава в подборе: товар, план и число подобранных единиц.
class PickLine {
  PickLine({
    required this.nomenclatureUid,
    required this.name,
    required this.quantity,
    this.picked = 0,
  });

  final String nomenclatureUid;

  /// Наименование из справочника; пусто, если карточки товара нет.
  final String name;

  /// `Количество` строки состава: столько единиц нужно подобрать.
  final num quantity;

  /// Сумма `units` подобранных марок.
  num picked;

  bool get complete => picked >= quantity;
}

/// Что показать и что сделать экрану после скана.
class MarkScanResult {
  const MarkScanResult({
    this.messages = const [],
    this.changed = false,
    this.allPicked = false,
  });

  /// Тексты по порядку: экран показывает их один за другим.
  final List<String> messages;

  /// Марка записана в `marks` или удалена из неё.
  final bool changed;

  /// Все строки состава закрыты: экран открывает меню.
  final bool allPicked;
}

/// Подбор марок открытого документа: скан пишет в `marks`, позиция — выбранная строка состава.
class MarkPick {
  MarkPick._({
    required this.document,
    required this.settings,
    required this.store,
    required this.online,
    required this.lines,
    required this.packages,
    this.trace,
  });

  /// Документ с составом и уже подобранными марками.
  static Future<MarkPick> open({
    required ScannedDocument document,
    required List<CompositionLine> composition,
    required AppSettings settings,
    required DocumentStore store,
    required MarkOnline online,
    bool? packages,
    void Function(String text, Map<String, Object?> data)? trace,
  }) async {
    final pick = MarkPick._(
      document: document,
      settings: settings,
      store: store,
      online: online,
      trace: trace,
      lines: [
        for (final line in composition)
          PickLine(
            nomenclatureUid: line.nomenclatureUid,
            name: line.name,
            quantity: line.quantity,
          ),
      ],
      packages: packages ?? settings.registerPackageBarcode,
    );
    try {
      pick._applyMarks(await store.marks(document.id));
    } catch (error) {
      throw MarkPickUnavailable(error);
    }
    return pick;
  }

  final ScannedDocument document;
  final AppSettings settings;
  final DocumentStore store;
  final MarkOnline online;
  final List<PickLine> lines;
  final _marks = <StoredMark>[];

  /// Диагностика подбора: строки в журнал работы, без сообщений пользователю.
  final void Function(String text, Map<String, Object?> data)? trace;

  /// Режим позиции: «Упаковки» запрещает штучные марки.
  bool packages;

  /// Открытая строка состава; осмысленна, когда [productOpen].
  var selected = 0;

  /// Позиция открыта: марка пишется в её строку.
  var productOpen = false;

  /// Подпись кнопки переключения режима — текущий режим.
  String get modeTitle => packages ? packagesMode : unitsAndPackagesMode;

  /// Номер строки состава для товара по УИД; -1 — товара в документе нет.
  ///
  /// Сравнение то же, что у марок: 1С отдаёт УИД в разном регистре и с пробелами.
  int lineOf(String nomenclatureUid) =>
      lines.indexWhere((line) => _sameUid(line.nomenclatureUid, nomenclatureUid));

  /// Скан уйдёт на сервер: включены проверки марки или запрос упаковки True API.
  bool get needsServer {
    if (!document.skipsOwnerCheck && (settings.checkMarkOwner || settings.requestSelectionStatus)) {
      return true;
    }
    return packages && settings.requestPackageViaTrueApi && _usesTrueApi;
  }

  /// «API для проверки марки» равно `True api`; регистр и пробелы не важны.
  bool get _usesTrueApi =>
      settings.markCheckApi.trim().toLowerCase() == trueApiCheckApi.toLowerCase();

  /// Разбор скана марки по порядку `docs/tsd-mark-pick.md`.
  Future<MarkScanResult> scan(String code, {ExchangeCancellation? cancellation}) async {
    if (document.uploaded) {
      return const MarkScanResult(messages: [markDocumentUploaded]);
    }
    return _scan(
      code,
      skipOwner: false,
      packageLookup: true,
      cancellation: cancellation ?? ExchangeCancellation(),
    );
  }

  /// Удаление марки: `marks` находит строку по КИ без криптохвоста, позиция закрывается.
  Future<MarkScanResult> delete(String code) async {
    if (document.uploaded) {
      return const MarkScanResult(messages: [markDocumentUploaded]);
    }

    final text = markStorageText(code);
    if (text.isEmpty) {
      return const MarkScanResult();
    }

    final cis = markCis(text);
    final index = _marks.indexWhere((mark) => mark.cis == cis);
    var removed = false;
    try {
      removed = await store.removeMark(document.id, text);
    } catch (_) {
      removed = false;
    }
    if (!removed) {
      return const MarkScanResult(messages: [markNotInDocument]);
    }

    if (index >= 0) {
      final mark = _marks.removeAt(index);
      for (final line in lines) {
        if (_sameUid(line.nomenclatureUid, mark.nomenclatureUid)) {
          line.picked -= mark.units;
        }
      }
    }
    productOpen = false;
    return const MarkScanResult(changed: true);
  }

  void _applyMarks(List<StoredMark> stored) {
    for (final mark in stored) {
      _marks.add(mark);
      for (final line in lines) {
        if (_sameUid(line.nomenclatureUid, mark.nomenclatureUid)) {
          line.picked += mark.units;
        }
      }
    }
  }

  Future<MarkScanResult> _scan(
    String code, {
    required bool skipOwner,
    required bool packageLookup,
    required ExchangeCancellation cancellation,
  }) async {
    final text = markStorageText(code);
    if (text.isEmpty || cancellation.isCancelled) {
      return const MarkScanResult();
    }

    final kind = _packKind(text);
    final pallet = kind.pallet;
    final package = kind.package;
    final barcodes = _barcodesFromMark(text, package: package, pallet: pallet);
    _trace('Скан марки', {
      'марка': text,
      'палета': pallet,
      'упаковка': package,
      'штрихкоды': barcodes.join(', '),
      'строк': lines.length,
    });

    if (!skipOwner) {
      final owner = await _checkOwner(text, cancellation);
      if (owner != null) {
        return owner;
      }
    }

    if (settings.requestSelectionStatus && !document.skipsOwnerCheck) {
      final status = await online.status(text, cancellation);
      if (!status.passed) {
        return MarkScanResult(messages: [status.error]);
      }
    }

    final messages = <String>[];
    var barcode = '';
    if (barcodes.isNotEmpty) {
      final found = await _openByBarcodes(barcodes, messages);
      final error = found.error;
      if (error != null) {
        return error;
      }
      barcode = found.barcode;
    }

    if (!productOpen) {
      // Товар по штрихкоду из марки не нашёлся — говорим об этом, а не просим штрихкод.
      return MarkScanResult(messages: messages.isEmpty ? const [markNeedsBarcode] : messages);
    }

    final line = lines[selected.clamp(0, lines.length - 1)];

    final cis = markCis(text);
    if (_marks.any((mark) => mark.cis == cis)) {
      return const MarkScanResult(messages: [markAlreadyScanned]);
    }

    final units = await _units(line, barcode: barcode, pallet: pallet, package: package);

    // Упаковка `00…` разбирается на составляющие, пока True API отдаёт её историю.
    if (packageLookup && package && packages && settings.requestPackageViaTrueApi && _usesTrueApi) {
      final composed = await _packComposition(text, line, cancellation: cancellation);
      if (composed != null) {
        return composed;
      }
    }

    final substitute = packageLookup &&
        packages &&
        units == 1 &&
        settings.requestPackageViaTrueApi &&
        _usesTrueApi;
    _trace('Упаковка True API', {
      'режим': modeTitle,
      'единиц': units,
      'включена': settings.requestPackageViaTrueApi,
      'api': settings.markCheckApi,
      'запрос': substitute,
    });
    if (substitute) {
      final token = await online.token(cancellation, inn: document.organization.inn);
      if (!token.passed) {
        _trace('Токен True API не получен', {'ошибка': token.error});
        return MarkScanResult(messages: [token.error]);
      }

      final pack = await online.package(text, token.token, cancellation);
      _trace('Ответ True API об упаковке', {
        'упаковка': pack.package,
        'марок': pack.marks.length,
        'ошибка': pack.error,
      });
      if (!pack.passed) {
        return MarkScanResult(messages: [pack.error]);
      }
      if (pack.marks.isNotEmpty) {
        return _addComposition(pack.marks, line, cancellation: cancellation);
      }
      if (pack.hasPackage) {
        return _scan(
          pack.package,
          skipOwner: true,
          packageLookup: false,
          cancellation: cancellation,
        );
      }
    }

    if (packages && units == 1) {
      return const MarkScanResult(messages: [markIndividualForbidden]);
    }

    if (line.picked + units > line.quantity) {
      return const MarkScanResult(messages: [markOverflow]);
    }
    if (cancellation.isCancelled) {
      return const MarkScanResult();
    }

    final mark = StoredMark(mark: text, nomenclatureUid: line.nomenclatureUid, units: units);
    try {
      await store.addMark(document.id, mark);
    } catch (error) {
      return MarkScanResult(messages: ['Марка не записалась: $error']);
    }

    _marks.add(mark);
    line.picked += units;
    final allPicked = lines.every((item) => item.complete);
    if (line.complete) {
      productOpen = false;
    }
    return MarkScanResult(changed: true, allPicked: allPicked);
  }

  /// Состав упаковки `00…` по True API: пишет вложения вместо групповой строки.
  ///
  /// `null` — состава нет или True API недоступен: вызывающий пишет упаковку одной строкой.
  Future<MarkScanResult?> _packComposition(
    String text,
    PickLine line, {
    required ExchangeCancellation cancellation,
  }) async {
    final token = await online.token(cancellation, inn: document.organization.inn);
    if (!token.passed) {
      _trace('Токен True API не получен', {'ошибка': token.error});
      return null;
    }

    final pack = await online.composition(text, token.token, cancellation);
    _trace('Ответ True API о составе упаковки', {
      'упаковка': text,
      'марок': pack.marks.length,
      'ошибка': pack.error,
    });
    if (cancellation.isCancelled) {
      return const MarkScanResult();
    }
    if (!pack.passed || pack.marks.isEmpty) {
      return null;
    }

    return _addComposition(pack.marks, line, cancellation: cancellation);
  }

  /// Пишет марки состава по одной: уже подобранные пропускает, лишние не влезают.
  Future<MarkScanResult> _addComposition(
    List<String> codes,
    PickLine line, {
    required ExchangeCancellation cancellation,
  }) async {
    final accepted = <StoredMark>[];
    var acceptedUnits = 0;
    var skipped = 0;
    var overflow = false;
    for (final code in codes) {
      final text = markStorageText(code);
      if (text.isEmpty) {
        continue;
      }
      final cis = markCis(text);
      if (_marks.any((mark) => mark.cis == cis) || accepted.any((mark) => mark.cis == cis)) {
        skipped += 1;
        continue;
      }
      final units = await _compositionUnits(text, line);
      if (line.picked + acceptedUnits + units > line.quantity) {
        overflow = true;
        break;
      }
      accepted.add(StoredMark(mark: text, nomenclatureUid: line.nomenclatureUid, units: units));
      acceptedUnits += units;
    }

    final messages = <String>[];
    if (accepted.isEmpty) {
      return MarkScanResult(
        messages: [overflow || skipped == 0 ? markOverflow : markPackageAllPicked],
      );
    }
    if (skipped > 0) {
      messages.add(markPackagePartlyPicked(skipped: skipped, added: accepted.length));
    }
    if (overflow) {
      messages.add(markOverflow);
    }
    if (cancellation.isCancelled) {
      return const MarkScanResult();
    }

    for (final mark in accepted) {
      try {
        await store.addMark(document.id, mark);
      } catch (error) {
        return MarkScanResult(messages: ['Марка не записалась: $error']);
      }
      _marks.add(mark);
      line.picked += mark.units;
    }

    final allPicked = lines.every((item) => item.complete);
    if (line.complete) {
      productOpen = false;
    }
    return MarkScanResult(
      changed: true,
      allPicked: allPicked,
      messages: messages,
    );
  }

  /// Единиц в марке состава: упаковка берёт размер, штучная — коэффициент штрихкода.
  Future<int> _compositionUnits(String text, PickLine line) async {
    final kind = _packKind(text);
    final pallet = kind.pallet;
    final package = kind.package;
    final barcodes = _barcodesFromMark(text, package: package, pallet: pallet);
    return _units(
      line,
      barcode: barcodes.isEmpty ? '' : barcodes.first,
      pallet: pallet,
      package: package,
    );
  }

  Future<MarkScanResult?> _checkOwner(String mark, ExchangeCancellation cancellation) async {
    if (!settings.checkMarkOwner || document.skipsOwnerCheck) {
      return null;
    }

    final answer = await online.owner(mark, cancellation);
    return answer.passed ? null : MarkScanResult(messages: [answer.error]);
  }

  /// Штрихкод из марки ведёт к товару: строка состава открывается, даже если была открыта другая.
  Future<({String barcode, MarkScanResult? error})> _openByBarcodes(
    List<String> barcodes,
    List<String> messages,
  ) async {
    for (final barcode in barcodes) {
      final uid = await _barcodeUid(barcode);
      _trace('Поиск товара по штрихкоду', {'штрихкод': barcode, 'товар': uid ?? 'не найден'});
      if (uid == null) {
        continue;
      }

      final index = lineOf(uid);
      if (index < 0) {
        _trace('Товара нет в составе', {'товар': uid, 'строк': lines.length});
        return (
          barcode: barcode,
          error: MarkScanResult(
            messages: [markNotInComposition(name: await _nameOf(uid), barcode: barcode)],
          ),
        );
      }

      selected = index;
      productOpen = true;
      _trace('Позиция открыта', {'товар': lines[index].name, 'строка': index});
      return (barcode: barcode, error: null);
    }

    if (productOpen) {
      final line = lines[selected.clamp(0, lines.length - 1)];
      _trace('Марка не для открытой позиции', {
        'штрихкод': barcodes.first,
        'позиция': line.name,
      });
      return (
        barcode: barcodes.first,
        error: MarkScanResult(
          messages: [markForOtherProduct(name: line.name, barcode: barcodes.first)],
        ),
      );
    }

    messages.add(markBarcodeUnknown(barcodes.first));
    return (barcode: barcodes.first, error: null);
  }

  Future<int> _units(
    PickLine line, {
    required String barcode,
    required bool pallet,
    required bool package,
  }) async {
    if (pallet || package) {
      return await _size(line.nomenclatureUid, pallet: pallet) ?? 1;
    }
    return await _coefficient(barcode) ?? 1;
  }

  Future<int?> _size(String uid, {required bool pallet}) async {
    try {
      final units = await store.packSizeUnits(uid, pallet: pallet);
      return units?.toInt();
    } catch (_) {
      return null;
    }
  }

  Future<int?> _coefficient(String barcode) async {
    try {
      final units = await store.barcodeCoefficient(barcode);
      return units?.toInt();
    } catch (_) {
      return null;
    }
  }

  Future<String?> _barcodeUid(String barcode) async {
    try {
      return (await store.nomenclatureUidsByBarcodes([barcode]))[barcode];
    } catch (_) {
      return null;
    }
  }

  Future<String> _nameOf(String uid) async {
    try {
      return (await store.nomenclatureNames({uid}))[uid] ?? '';
    } catch (_) {
      return '';
    }
  }

  void _trace(String text, Map<String, Object?> data) => trace?.call(text, data);

  /// УИД в 1С приходят в разном регистре и с пробелами: сравниваем без них.
  static bool _sameUid(String left, String right) =>
      left.trim().toLowerCase() == right.trim().toLowerCase();

  /// Код с `000` — палета, с `00` — упаковка. Штучная марка длиной 29 палетой не считается:
  /// у сигарет GTIN сам начинается с нулей.
  static ({bool pallet, bool package}) _packKind(String text) {
    if (text.length == 29) {
      return (pallet: false, package: false);
    }

    final pallet = text.startsWith('000');
    return (pallet: pallet, package: !pallet && text.startsWith('00'));
  }

  /// Штрихкоды товара из марки: 13 цифр GTIN без первой, полный GTIN и он же без ведущих нулей.
  ///
  /// У кода `01` GTIN стоит с третьего символа, у штучной марки длиной 29 — с первого.
  /// Несколько нулей слева бывают у короткого штрихкода сигарет: в справочнике он лежит без них.
  /// Код из одних цифр длиной 12–14 считается самим штрихкодом товара.
  static List<String> _barcodesFromMark(
    String mark, {
    required bool package,
    bool pallet = false,
  }) {
    if (package || pallet) {
      return const [];
    }

    if (mark.startsWith('01') && mark.length >= 16) {
      return _barcodePair(mark.substring(3, 16), mark.substring(2, 16));
    }
    if (mark.length == 29) {
      return _barcodePair(mark.substring(1, 14), mark.substring(0, 14));
    }
    if (RegExp(r'^\d{12,14}$').hasMatch(mark)) {
      return _barcodePair(mark, mark);
    }
    return const [];
  }

  /// Сначала короткий ключ, затем полный GTIN. Если слева несколько нулей, третьим идёт код без них.
  static List<String> _barcodePair(String short, String full) {
    final codes = <String>[short];
    if (full != short) {
      codes.add(full);
    }
    final stripped = full.replaceFirst(RegExp(r'^0+'), '');
    if (stripped.isNotEmpty && !codes.contains(stripped)) {
      codes.add(stripped);
    }
    return codes;
  }
}
