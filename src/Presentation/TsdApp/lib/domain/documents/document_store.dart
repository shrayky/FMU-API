import 'document_packet.dart';
import 'scanned_document.dart';
import 'stored_mark.dart';

/// Хранение документов ТСД: сбор марок и списание, их марки и справочники пакета в основной базе приложения.
abstract interface class DocumentStore {
  /// Пишет справочники и документ одной транзакцией: половина пакета не остаётся.
  Future<ScannedDocument> save(DocumentPacket packet, {DateTime? scannedAt});

  /// Новый пустой документ списания: причина фасовка, марок нет.
  Future<ScannedDocument> createWriteOff({required DateTime scannedAt, String number = ''});

  /// Документы от новых к старым, отбор поисковой строкой по представлению, номеру и контрагенту.
  ///
  /// Пустой [kind] оставляет список сбора: вид сбора и строки без вида.
  /// Тело документа не читается: список журнала не разбирает JSON, а экран сбора берёт
  /// документ через [document].
  Future<List<ScannedDocument>> documents({String query = '', String kind = ''});

  /// Документ с телом `Состав`; `null`, если строки уже нет.
  Future<ScannedDocument?> document(String id);

  /// Пишет причину выбытия в тело документа, марки не трогает.
  Future<void> setReason(String documentId, {required String reason});

  /// Пишет выбранную организацию в тело документа: ИНН, название и печатную группу.
  Future<void> setOrganisation(
    String documentId, {
    required String inn,
    required String name,
    int printGroupId = 0,
  });

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

  /// Пишет марку. Повтор того же ключа `document_id` и `cis` строку не плодит,
  /// даже если у марки другой криптохвост.
  Future<void> addMark(String documentId, StoredMark mark);

  /// Меняет количество и цену марки, не сдвигая строку в журнале.
  Future<void> updateMark(String documentId, StoredMark mark);

  /// Удаляет марку документа по её КИ без хвоста; `false`, если такой марки не было.
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
