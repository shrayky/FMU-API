import 'document_packet.dart';
import 'scanned_document.dart';
import 'stored_mark.dart';

/// Хранение документов сбора марок, их марок и справочников пакета в основной базе приложения.
abstract interface class DocumentStore {
  /// Пишет справочники и документ одной транзакцией: половина пакета не остаётся.
  Future<ScannedDocument> save(DocumentPacket packet, {DateTime? scannedAt});

  /// Документы от новых к старым, отбор поисковой строкой по представлению, номеру и контрагенту.
  ///
  /// Тело документа не читается: список журнала не разбирает JSON, а экран сбора берёт
  /// документ через [document].
  Future<List<ScannedDocument>> documents({String query = ''});

  /// Документ с телом `Состав`; `null`, если строки уже нет.
  Future<ScannedDocument?> document(String id);

  /// Удаляет документы старше `days` дней вместе с их марками.
  ///
  /// Ноль и меньше не удаляют ничего, справочники не трогаются.
  Future<void> trim({required int days});

  /// Наименования товаров по УИД: строки состава показывают их.
  Future<Map<String, String>> nomenclatureNames(Set<String> uids);

  /// УИД товаров по штрихкодам справочника: по ним находится строка состава.
  Future<Map<String, String>> nomenclatureUidsByBarcodes(Iterable<String> barcodes);

  /// Сколько штрихкодов в справочнике: диагностика подбора марки.
  Future<int> barcodeCount();

  /// Марки документа в порядке подбора.
  Future<List<StoredMark>> marks(String documentId);

  /// Пишет марку. Повтор того же ключа `document_id` и `mark` строку не плодит.
  Future<void> addMark(String documentId, StoredMark mark);

  /// Удаляет марку документа; `false`, если такой марки в документе не было.
  Future<bool> removeMark(String documentId, String mark);

  /// Отметка выгрузки документа: журнал пишет по ней «Выгружен».
  Future<void> setUploaded(String documentId, {required bool uploaded});

  /// `ЕдиницВУпаковке` размера товара: `pallet` выбирает палету или упаковку.
  ///
  /// `null` — строки размера нет, вызывающий считает единицу.
  Future<num?> packSizeUnits(String nomenclatureUid, {required bool pallet});

  /// `Коэффициент` карточки штрихкода; `null` — карточки нет.
  Future<num?> barcodeCoefficient(String barcode);
}
