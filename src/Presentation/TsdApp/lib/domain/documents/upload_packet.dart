import 'dart:convert';

import 'scanned_document.dart';
import 'stored_mark.dart';

/// Команда выгрузки документа сбора марок в пакете.
const markUploadCommand = 'СборМарок';

/// Склад в пакете выгрузки: 1С принимает GUID-заглушку.
const markUploadWarehouse = '00000000-0000-0000-0000-000000000000';

/// Начало текста неудачной выгрузки: дальше идёт причина.
const uploadFailedPrefix = 'Обмен закончился неудачей по причиние: ';

/// Ответ без пакета: сервер не прислал разбираемое тело.
const uploadNoPacket = 'Обмен закончился неудачей! Не получен пакет ответа';

/// Пакет пришёл, но успеха в нём нет и текста ошибки тоже нет.
const uploadFailed = 'Обмен закончился неудачей!';

/// Пакет выгрузки документа сбора марок.
Map<String, Object?> markUploadPacket(ScannedDocument document, List<StoredMark> marks) {
  return {
    'Команда': markUploadCommand,
    'Склад': markUploadWarehouse,
    'Данные': {
      'УИДИС': document.erpUid,
      'Тип': markUploadCommand,
      'ТипДокументаErp': textOf(document.body['ТипДокументаErp']),
      'Состав': [
        for (final mark in marks)
          {
            'Марка': mark.mark,
            'Номенклатура': mark.nomenclatureUid,
            'ГрупповойКод': mark.units > 1,
            'КоличествоЕдиниц': mark.units,
          },
      ],
    },
  };
}

/// Разбор ответа на выгрузку: успех или готовый текст неудачи.
class MarkUploadAnswer {
  const MarkUploadAnswer.succeeded() : error = '';
  const MarkUploadAnswer.failed(this.error);

  final String error;

  bool get succeeded => error.isEmpty;
}

/// Читает `Пакет.Результат` и `Пакет.Ошибка` из тела ответа сервера обмена.
MarkUploadAnswer parseMarkUploadAnswer(String body) {
  final packet = _object(_object(_json(body))['Пакет']);
  if (packet.isEmpty) {
    return const MarkUploadAnswer.failed(uploadNoPacket);
  }

  if (flagOf(packet['Результат'])) {
    return const MarkUploadAnswer.succeeded();
  }

  final error = textOf(packet['Ошибка']);
  return MarkUploadAnswer.failed(error.isEmpty ? uploadFailed : '$uploadFailedPrefix$error');
}

Object? _json(String body) {
  try {
    return jsonDecode(body);
  } catch (_) {
    return null;
  }
}

/// `Пакет` в ответах 1С бывает и объектом, и строкой JSON.
Map<String, Object?> _object(Object? value) {
  final parsed = value is String ? _json(value) : value;
  if (parsed is! Map) {
    return const {};
  }

  return {for (final item in parsed.entries) '${item.key}': item.value};
}
