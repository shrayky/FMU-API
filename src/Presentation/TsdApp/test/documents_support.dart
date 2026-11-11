import 'dart:async';

import 'package:tsd_app/domain/documents/document_packet.dart';
import 'package:tsd_app/domain/documents/document_store.dart';
import 'package:tsd_app/domain/documents/scanned_document.dart';
import 'package:tsd_app/domain/documents/stored_mark.dart';
import 'package:tsd_app/domain/exchange/exchange_call_result.dart';
import 'package:tsd_app/domain/exchange/exchange_cancellation.dart';
import 'package:tsd_app/domain/exchange/exchange_protocol.dart';
import 'package:tsd_app/domain/exchange/exchange_server.dart';
import 'package:tsd_app/domain/mark/mark_online.dart';
import 'package:tsd_app/infrastructure/settings/app_settings.dart';

const sampleErpUid = 'erp-uid-1';
const sampleGoodsUid = 'goods-uid-1';
const sampleBarcode = '4600000000001';
const samplePresentation = 'Сбор марок № 123 от 05.11.2026';
const sampleGoodsName = 'Вода негазированная 0,5 л';

/// Тело ответа ds-tsd на запрос документа сбора: документ, строка состава и справочники.
const sampleDocumentResponse = '''
{
  "Результат": true,
  "Пакет": {
    "Данные": {
      "Документы": [
        {
          "УИД": "erp-uid-1",
          "Представление": "Сбор марок № 123 от 05.11.2026",
          "Тип": "СборМарок",
          "БезПроверкиВладельца": false,
          "Весовой": "false",
          "Номер": "123",
          "Контрагент": "ООО Ромашка",
          "Состав": [
            {
              "Номенклатура": "goods-uid-1",
              "Количество": 12,
              "Маркированный": true,
              "КоличествоМарок": 1,
              "Цена": 100.5,
              "Сумма": 1206,
              "ЗаказаноКоличество": 12,
              "ПодбораноПоЗаказуПолностью": false
            }
          ]
        }
      ],
      "Справочники": {
        "Номенклатура": [
          {"УИД": "goods-uid-1", "Наименование": "Вода негазированная 0,5 л"}
        ],
        "Штрихкоды": [
          {"Штрихкод": "4600000000001", "Номенклатура": "goods-uid-1"}
        ],
        "РазмерыУпаковок": [
          {"Номенклатура": "goods-uid-1", "ЕдиницВУпаковке": 6}
        ]
      }
    }
  }
}
''';

/// Пакет без документа: массив `Документы` пуст.
const emptyDocumentsResponse = '{"Результат": false, "Пакет": {"Данные": {"Документы": []}}}';

AppSettings settingsWithServer({
  String protocol = defaultExchangeProtocol,
  int retentionDays = 30,
  bool withServer = true,
  bool packages = false,
  bool checkOwner = false,
  bool selectionStatus = false,
}) {
  final server = ExchangeServer(
    id: 'srv-1',
    name: 'КД',
    host: '10.0.0.5',
    port: 80,
    publication: 'ideal1/hs',
    user: 'user',
    password: 'secret',
    timeoutSeconds: 10,
    protocol: protocol,
  );

  return AppSettings(
    exchangeServerId: withServer ? server.id : '',
    documentRetentionDays: retentionDays,
    registerPackageBarcode: packages,
    checkMarkOwner: checkOwner,
    requestSelectionStatus: selectionStatus,
    exchangeServers: withServer ? [server] : const [],
  );
}

/// Хранилище документов для тестов: помнит запросы и отдаёт заданные документы.
class FakeDocumentStore implements DocumentStore {
  final saved = <DocumentPacket>[];
  final trims = <int>[];
  final rows = <ScannedDocument>[];
  final queries = <String>[];
  final requestedUids = <Set<String>>[];
  final marksByDocument = <String, List<StoredMark>>{};
  final uploads = <(String, bool)>[];
  final packSizes = <String, num>{};
  final coefficients = <String, num>{};
  var names = <String, String>{};
  var barcodeToUid = <String, String>{};

  /// Гейт для теста: состав не читается, пока его не отпустят.
  Completer<void>? namesGate;

  /// Гейт записи марки: скан висит на записи в базу, пока его не отпустят.
  Completer<void>? addMarkGate;

  /// Сбой чтения марок: подбор документа не должен открываться.
  Object? marksError;

  /// Сбой записи документа.
  Object? saveError;

  /// Сбой очистки срока хранения.
  Object? trimError;

  /// Сколько первых вызовов `setUploaded` падает.
  var setUploadedFailures = 0;

  /// Ключ размера упаковки: УИД товара и палета, как `pack_sizes`.
  static String sizeKey(String uid, {required bool pallet}) => '$uid:${pallet ? 1 : 0}';

  @override
  Future<ScannedDocument> save(DocumentPacket packet, {DateTime? scannedAt}) async {
    final error = saveError;
    if (error != null) {
      throw error;
    }

    saved.add(packet);
    final document = ScannedDocument(
      id: 'doc-${saved.length}',
      scannedAt: scannedAt ?? DateTime(2026, 11, 5, 10, 30),
      number: textOf(packet.document['Номер']),
      counterparty: textOf(packet.document['Контрагент']),
      presentation: textOf(packet.document['Представление']),
      erpUid: textOf(packet.document['УИД']),
      body: packet.document,
    );
    rows.insert(0, document);
    return document;
  }

  @override
  Future<List<ScannedDocument>> documents({String query = ''}) async {
    queries.add(query);
    final text = query.trim().toLowerCase();
    return [
      for (final document in rows)
        if (text.isEmpty ||
            document.presentation.toLowerCase().contains(text) ||
            document.number.toLowerCase().contains(text) ||
            document.counterparty.toLowerCase().contains(text))
          ScannedDocument(
            id: document.id,
            scannedAt: document.scannedAt,
            number: document.number,
            counterparty: document.counterparty,
            presentation: document.presentation,
            erpUid: document.erpUid,
            uploaded: document.uploaded,
          ),
    ];
  }

  @override
  Future<ScannedDocument?> document(String id) async {
    for (final document in rows) {
      if (document.id == id) {
        return document;
      }
    }
    return null;
  }

  @override
  Future<void> trim({required int days}) async {
    trims.add(days);
    final error = trimError;
    if (error != null) {
      throw error;
    }
  }

  @override
  Future<Map<String, String>> nomenclatureNames(Set<String> uids) async {
    final gate = namesGate;
    if (gate != null) {
      await gate.future;
    }
    requestedUids.add(uids);
    return {
      for (final uid in uids)
        if (names[uid] != null) uid: names[uid]!,
    };
  }

  @override
  Future<Map<String, String>> nomenclatureUidsByBarcodes(Iterable<String> barcodes) async {
    return {
      for (final barcode in barcodes)
        if (barcodeToUid[barcode] != null) barcode: barcodeToUid[barcode]!,
    };
  }

  @override
  Future<int> barcodeCount() async => barcodeToUid.length;

  @override
  Future<List<StoredMark>> marks(String documentId) async {
    final error = marksError;
    if (error != null) {
      throw error;
    }

    return [...?marksByDocument[documentId]];
  }

  @override
  Future<void> addMark(String documentId, StoredMark mark) async {
    final gate = addMarkGate;
    if (gate != null) {
      await gate.future;
    }

    final marks = marksByDocument.putIfAbsent(documentId, () => []);
    marks.removeWhere((item) => item.mark == mark.mark);
    marks.add(mark);
  }

  @override
  Future<bool> removeMark(String documentId, String mark) async {
    final marks = marksByDocument[documentId];
    if (marks == null) {
      return false;
    }
    final before = marks.length;
    marks.removeWhere((item) => item.mark == mark);
    return marks.length != before;
  }

  @override
  Future<void> setUploaded(String documentId, {required bool uploaded}) async {
    uploads.add((documentId, uploaded));
    if (setUploadedFailures > 0) {
      setUploadedFailures -= 1;
      throw StateError('setUploaded');
    }
  }

  @override
  Future<num?> packSizeUnits(String nomenclatureUid, {required bool pallet}) async {
    return packSizes[sizeKey(nomenclatureUid, pallet: pallet)];
  }

  @override
  Future<num?> barcodeCoefficient(String barcode) async => coefficients[barcode];
}

/// Сетевые проверки марки для тестов: помнит вызовы и отдаёт заданные ответы.
class FakeMarkOnline implements MarkOnline {
  final owners = <String>[];
  final statuses = <String>[];
  final packages = <String>[];
  final tokenInns = <String>[];
  final packageByMark = <String, MarkPackageAnswer>{};
  var ownerAnswer = const MarkCheckAnswer.passed();
  var statusAnswer = const MarkStatusAnswer.passed();
  var tokenAnswer = const TrueApiTokenAnswer(token: 'token');
  var packageAnswer = const MarkPackageAnswer();
  var tokens = 0;

  /// Задержка владельца: по ней тест держит окно ожидания.
  Future<MarkCheckAnswer> Function(String mark)? onOwner;

  @override
  Future<MarkCheckAnswer> owner(String mark, ExchangeCancellation cancellation) async {
    owners.add(mark);
    final handler = onOwner;
    return handler == null ? ownerAnswer : handler(mark);
  }

  @override
  Future<MarkStatusAnswer> status(String mark, ExchangeCancellation cancellation) async {
    statuses.add(mark);
    return statusAnswer;
  }

  @override
  Future<TrueApiTokenAnswer> token(ExchangeCancellation cancellation, {String inn = ''}) async {
    tokens += 1;
    tokenInns.add(inn);
    return tokenAnswer;
  }

  @override
  Future<MarkPackageAnswer> package(
    String mark,
    String token,
    ExchangeCancellation cancellation,
  ) async {
    packages.add(mark);
    return packageByMark[mark] ?? packageAnswer;
  }
}

/// Протокол для тестов загрузки: отвечает на запрос документа, остальные команды не подключены.
class DocumentProtocol implements ExchangeProtocol {
  DocumentProtocol(this.onDocument, {this.onUpload});

  final Future<ExchangeCallResult> Function(String barcode) onDocument;
  final Future<ExchangeCallResult> Function(Map<String, Object?> packet)? onUpload;
  final barcodes = <String>[];
  final packets = <Map<String, Object?>>[];

  @override
  Future<ExchangeCallResult> documentByBarcode(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String command,
    required String barcode,
  }) {
    barcodes.add(barcode);
    return onDocument(barcode);
  }

  @override
  Future<ExchangeCallResult> probe(ExchangeServer server, ExchangeCancellation cancellation) async {
    return const ExchangeCallResult(success: false, error: exchangeCommandPending);
  }

  @override
  Future<ExchangeCallResult> markStatus(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String mark,
  }) async {
    return const ExchangeCallResult(success: false, error: exchangeCommandPending);
  }

  @override
  Future<ExchangeCallResult> trueApiToken(ExchangeServer server, ExchangeCancellation cancellation) async {
    return const ExchangeCallResult(success: false, error: exchangeCommandPending);
  }

  @override
  Future<ExchangeCallResult> loadGoods(ExchangeServer server, ExchangeCancellation cancellation) async {
    return const ExchangeCallResult(success: false, error: exchangeCommandPending);
  }

  @override
  Future<ExchangeCallResult> uploadDocument(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required Map<String, Object?> packet,
  }) async {
    packets.add(packet);
    final upload = onUpload;
    if (upload == null) {
      return const ExchangeCallResult(success: false, error: exchangeCommandPending);
    }
    return upload(packet);
  }

  @override
  Future<ExchangeCallResult> setPrice(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String nomenclature,
    required num price,
  }) async {
    return const ExchangeCallResult(success: false, error: exchangeCommandPending);
  }
}
