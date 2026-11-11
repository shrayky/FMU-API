/// Документ сбора марок, сохранённый в `main.db`. Один скан — одна строка журнала.
class ScannedDocument {
  const ScannedDocument({
    required this.id,
    required this.scannedAt,
    this.number = '',
    this.counterparty = '',
    this.presentation = '',
    this.erpUid = '',
    this.body = const {},
    this.uploaded = false,
  });

  final String id;

  /// Момент скана: срок хранения и колонка «Дата».
  final DateTime scannedAt;

  /// `Номер` документа, если он был в ответе.
  final String number;

  /// `Контрагент` документа, если он был в ответе.
  final String counterparty;

  /// `Представление` документа.
  final String presentation;

  /// `УИД` ERP.
  final String erpUid;

  /// JSON объекта документа: `Состав` и прочие поля как пришли.
  final Map<String, Object?> body;

  /// Отметка выгрузки: пакет ушёл на сервер обмена.
  final bool uploaded;

  /// `БезПроверкиВладельца`: при истине марки документа проверок владельца не проходят.
  bool get skipsOwnerCheck => flagOf(body['БезПроверкиВладельца']);

  /// Организация документа: её ИНН уходит в запрос токена True API.
  DocumentOrganization get organization => documentOrganization(body);

  /// Строки `Состав` без имён товаров: имена подставляет справочник `nomenclature`.
  List<Map<String, Object?>> get composition {
    final lines = body['Состав'];
    if (lines is! List) {
      return const [];
    }

    final rows = <Map<String, Object?>>[];
    for (final line in lines) {
      if (line is Map) {
        rows.add({for (final item in line.entries) '${item.key}': item.value});
      }
    }
    return rows;
  }

  /// УИД товаров строк состава: по ним читаются карточки `nomenclature`.
  List<String> get nomenclatureUids {
    final uids = <String>[];
    for (final line in composition) {
      final uid = textOf(line['Номенклатура']);
      if (uid.isNotEmpty && !uids.contains(uid)) {
        uids.add(uid);
      }
    }
    return uids;
  }
}

/// Строка состава документа с наименованием товара.
class CompositionLine {
  const CompositionLine({
    required this.nomenclatureUid,
    required this.name,
    required this.quantity,
    required this.marks,
  });

  final String nomenclatureUid;

  /// Наименование из `nomenclature`; пусто, если карточки товара нет.
  final String name;

  final num quantity;
  final num marks;
}

/// Строки состава с наименованиями из справочника товаров.
List<CompositionLine> documentComposition(ScannedDocument document, Map<String, String> names) {
  return [
    for (final line in document.composition)
      CompositionLine(
        nomenclatureUid: textOf(line['Номенклатура']),
        name: names[textOf(line['Номенклатура'])] ?? '',
        quantity: numberOf(line['Количество']),
        marks: numberOf(line['КоличествоМарок']),
      ),
  ];
}

/// Значение поля строкой: числа из ответа 1С приходят и числами, и строками.
String textOf(Object? value) {
  if (value == null) {
    return '';
  }

  return '$value'.trim();
}

/// Организация документа: наименование, ИНН и КПП для запросов к Fmu-Api.
class DocumentOrganization {
  const DocumentOrganization({this.name = '', this.inn = '', this.kpp = ''});

  final String name;
  final String inn;
  final String kpp;

  bool get isEmpty => name.isEmpty && inn.isEmpty && kpp.isEmpty;
}

/// Организация из документа: объект `Организация` либо поля `ИНН`, `КПП`, `Организация` рядом.
DocumentOrganization documentOrganization(Map<String, Object?> body) {
  final raw = body['Организация'] ?? body['организация'];
  final Map<String, Object?> card;
  if (raw is Map) {
    card = {for (final item in raw.entries) '${item.key}': item.value};
  } else {
    card = const {};
  }

  return DocumentOrganization(
    name: _firstText([
      card['Название'],
      card['Наименование'],
      card['name'],
      if (raw is String) raw,
      body['НаименованиеОрганизации'],
    ]),
    inn: _firstText([
      card['ИНН'],
      card['инн'],
      card['inn'],
      body['ИНН'],
      body['Инн'],
      body['ИННОрганизации'],
    ]),
    kpp: _firstText([
      card['КПП'],
      card['кпп'],
      card['kpp'],
      body['КПП'],
      body['Кпп'],
      body['КППОрганизации'],
    ]),
  );
}

String _firstText(List<Object?> values) {
  for (final value in values) {
    final text = textOf(value);
    if (text.isNotEmpty) {
      return text;
    }
  }
  return '';
}

/// Логическое поле 1С: приходит и `true`, и строкой `"true"`, и числом 1.
bool flagOf(Object? value) {
  if (value is bool) {
    return value;
  }
  if (value is num) {
    return value != 0;
  }

  final text = textOf(value).toLowerCase();
  return text == 'true' || text == 'истина' || text == 'да' || text == '1';
}

/// Значение поля числом; нечисловой текст считается нулём.
num numberOf(Object? value) {
  if (value is num) {
    return value;
  }
  if (value is String) {
    return num.tryParse(value.trim()) ?? 0;
  }
  return 0;
}

/// Количество для экрана: целое без дробной части.
String formatQuantity(num value) {
  if (value == value.truncateToDouble()) {
    return '${value.toInt()}';
  }
  return '$value';
}

/// Дата документа для журнала и шапки: день скана.
String formatDocumentDate(DateTime at) {
  final day = at.day.toString().padLeft(2, '0');
  final month = at.month.toString().padLeft(2, '0');
  return '$day.$month.${at.year}';
}

