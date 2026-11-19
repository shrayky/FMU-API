/// Запросы экрана списания марок: организации, сведения о марке, цена и черновик акта.
abstract interface class WriteOffApi {
  /// Организации из настроек Fmu-Api: ИНН и название.
  Future<List<WriteOffOrganisation>> organisations();

  /// Название товара и масса марки из Честного знака; `null` — сведений нет.
  Future<WriteOffMarkInfo?> markInfo({required String inn, required String mark});

  /// Розничная цена по штрихкоду; `null` — цены нет или база Frontol недоступна.
  Future<num?> retailPriceByBarcode(String barcode);

  /// Создаёт черновик списания с причиной и строками; ошибка — исключение с текстом сервера.
  ///
  /// [uid] документа ТСД не даёт повторной выгрузке создать второй акт.
  Future<void> createReceipt({
    required String inn,
    required String reason,
    required List<WriteOffReceiptLine> lines,
    int? printGroupId,
    String uid = '',
    String number = '',
    String terminalId = '',
  });
}

/// Организация из настроек Fmu-Api: печатная группа, ИНН, название и КПП.
class WriteOffOrganisation {
  const WriteOffOrganisation({
    required this.id,
    required this.inn,
    this.name = '',
    this.kpp = '',
  });

  /// Id печатной группы: у организаций с одним ИНН разные КПП.
  final int id;

  final String inn;
  final String name;
  final String kpp;

  /// Подпись выбора: название и ИНН, у обособленного подразделения ещё КПП.
  String get label {
    final head = name.isEmpty ? inn : '$name ($inn)';
    return kpp.isEmpty ? head : '$head, КПП $kpp';
  }
}


/// Сведения о марке из Честного знака для таблицы списания.
class WriteOffMarkInfo {
  const WriteOffMarkInfo({this.name = '', this.gtin = '', this.quantity});

  /// Название товара; пусто, если карточка товара не пришла.
  final String name;

  /// GTIN марки: по нему запрашивается цена во Frontol.
  final String gtin;

  /// Масса в килограммах; `null` — товар штучный и количество равно единице.
  final num? quantity;
}

/// Строка выгрузки: марка, количество и цена с экрана ТСД.
class WriteOffReceiptLine {
  const WriteOffReceiptLine({required this.mark, required this.quantity, this.price});

  final String mark;
  final num quantity;
  final num? price;
}
