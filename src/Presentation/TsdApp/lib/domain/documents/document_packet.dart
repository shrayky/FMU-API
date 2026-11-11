import 'dart:convert';

import 'scanned_document.dart';

/// Документа в пакете нет: массив `Документы` пуст или отсутствует.
const documentMissing = 'В полученном пакете отсутствует документ!';

/// Документ и справочники из тела ответа ds-tsd на запрос документа по штрихкоду.
class DocumentPacket {
  const DocumentPacket({
    required this.document,
    this.nomenclature = const [],
    this.barcodes = const [],
    this.packSizes = const [],
  });

  /// Первый объект `Пакет.Данные.Документы`. `Состав` и `БезПроверкиВладельца` остаются в нём.
  final Map<String, Object?> document;

  final List<Map<String, Object?>> nomenclature;
  final List<Map<String, Object?>> barcodes;
  final List<Map<String, Object?>> packSizes;
}

/// Разбирает тело ответа. Флаги `Результат` не читаются: документ есть, когда непуст `Документы`.
///
/// Поля ищутся по всему ответу, как `ЗначениеПоляВСтруктуре` в 1С: `Документы`, `Справочники`
/// и организация встречаются и в `Пакет.Данные`, и рядом с ним.
DocumentPacket? parseDocumentPacket(String body) {
  final root = _json(body);
  final documents = _deepField(root, 'Документы');
  if (documents is! List || documents.isEmpty) {
    return null;
  }

  final document = _object(documents.first);
  if (document == null) {
    return null;
  }

  _applyOrganization(document, root);

  final references = _references(_deepField(root, 'Справочники'));
  return DocumentPacket(
    document: document,
    nomenclature: _objects(references['Номенклатура']),
    barcodes: _objects(references['Штрихкоды']),
    packSizes: _objects(references['РазмерыУпаковок']),
  );
}

/// Поле ответа на любом уровне вложенности; `null`, если его нет.
Object? _deepField(Object? value, String key, [int depth = 0]) {
  final object = _object(value);
  if (object == null || depth > 5) {
    return null;
  }
  if (object.containsKey(key)) {
    return object[key];
  }

  for (final item in object.values) {
    final found = _deepField(item, key, depth + 1);
    if (found != null) {
      return found;
    }
  }
  return null;
}

/// Организация из пакета переезжает в документ, если сам документ её не принёс.
void _applyOrganization(Map<String, Object?> document, Object? root) {
  if (!documentOrganization(document).isEmpty) {
    return;
  }

  final organization = _deepOrganization(root);
  if (organization.isEmpty) {
    return;
  }

  document['Организация'] = {
    'Название': organization.name,
    'ИНН': organization.inn,
    'КПП': organization.kpp,
  };
}

/// Организация на любом уровне ответа: объект `Организация` либо пара `ИНН` и `КПП`.
DocumentOrganization _deepOrganization(Object? value, [int depth = 0]) {
  final object = _object(value);
  if (object == null || depth > 5) {
    return const DocumentOrganization();
  }

  final organized = object.containsKey('Организация') ||
      object.containsKey('организация') ||
      (object.containsKey('ИНН') && object.containsKey('КПП'));
  if (organized) {
    final organization = documentOrganization(object);
    if (!organization.isEmpty) {
      return organization;
    }
  }

  for (final item in object.values) {
    final found = _deepOrganization(item, depth + 1);
    if (!found.isEmpty) {
      return found;
    }
  }
  return const DocumentOrganization();
}

/// `Справочники` приходят объектом с массивами, массивом `Ключ`/`Значение` или строкой JSON.
Map<String, Object?> _references(Object? value) {
  final parsed = value is String ? _json(value) : value;
  if (parsed is! List) {
    return _object(parsed) ?? const {};
  }

  final references = <String, Object?>{};
  for (final item in parsed) {
    final entry = _object(item);
    if (entry == null) {
      continue;
    }
    final key = '${entry['Ключ'] ?? entry['ключ'] ?? entry['Key'] ?? ''}'.trim();
    if (key.isEmpty) {
      continue;
    }
    references[key] = entry['Значение'] ?? entry['значение'] ?? entry['Value'];
  }
  return references;
}

Object? _json(String body) {
  try {
    return jsonDecode(body);
  } catch (_) {
    return null;
  }
}

/// `Пакет` в ответах 1С бывает и объектом, и строкой JSON.
Map<String, Object?>? _object(Object? value) {
  final parsed = value is String ? _json(value) : value;
  if (parsed is! Map) {
    return null;
  }

  return {for (final item in parsed.entries) '${item.key}': item.value};
}

List<Map<String, Object?>> _objects(Object? value) {
  final parsed = value is String ? _json(value) : value;
  if (parsed is! List) {
    return const [];
  }

  final items = <Map<String, Object?>>[];
  for (final item in parsed) {
    final object = _object(item);
    if (object != null) {
      items.add(object);
    }
  }
  return items;
}
