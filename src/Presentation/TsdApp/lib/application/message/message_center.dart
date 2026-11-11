import 'package:flutter/material.dart';

import '../../domain/exchange/exchange_call_result.dart';
import '../../domain/exchange/exchange_protocol.dart';
import '../../domain/exchange/exchange_server.dart';
import '../../domain/journal/work_log_entry.dart';
import '../../domain/message/app_message.dart';
import '../../domain/scan/barcode_kind.dart';
import '../../presentation/shell/message_dialog.dart';
import '../journal/work_log.dart';
import 'message_effects.dart';

const documentLoadFailedPrefix = 'Загрузка документа завершилась с ошибкой: ';

/// Единые правила сообщений: текст, запись в журнал, звук и внешний вид.
class MessageCenter {
  MessageCenter({WorkLog? log, this.effects = const MessageEffects()})
      : _log = log ?? WorkLog.instance;

  final WorkLog _log;
  final MessageEffects effects;

  AppMessage exchangeProbe(ExchangeServer server, ExchangeCallResult result) {
    if (result.success) {
      return AppMessage(
        kind: MessageKind.success,
        text: exchangeProbeSuccess,
        data: {'endpoint': server.endpoint},
      );
    }

    return AppMessage(
      kind: MessageKind.failure,
      text: '$exchangeProbeFailedPrefix${result.error}',
      data: _probeData(server, result),
    );
  }

  AppMessage exchangeBlocked(ExchangeServer server, ExchangeCallResult blocked) {
    return AppMessage(
      kind: MessageKind.warning,
      text: '$exchangeProbeFailedPrefix${blocked.error}',
      data: {'endpoint': server.endpoint},
    );
  }

  /// Загрузка документа не удалась: ошибка протокола, пакета или базы.
  AppMessage documentFailure({
    required String error,
    required String barcode,
    String endpoint = '',
    String details = '',
  }) {
    final raw = details.trim();
    return AppMessage(
      kind: MessageKind.failure,
      text: '$documentLoadFailedPrefix$error',
      data: {
        if (barcode.isNotEmpty) 'barcode': barcode,
        if (endpoint.isNotEmpty) 'endpoint': endpoint,
        if (raw.isNotEmpty) 'raw': raw,
      },
    );
  }

  /// Запрос документа не ушёл в сеть: сервер, адрес или протокол не готовы.
  AppMessage documentBlocked({
    required String error,
    required String barcode,
    String endpoint = '',
  }) {
    return AppMessage(
      kind: MessageKind.warning,
      text: '$documentLoadFailedPrefix$error',
      data: {
        if (barcode.isNotEmpty) 'barcode': barcode,
        if (endpoint.isNotEmpty) 'endpoint': endpoint,
      },
    );
  }

  /// Штрихкод не того вида, который ждёт экран в текущем состоянии.
  AppMessage barcodeWrongType({required String barcode, required List<BarcodeKind> expected}) {
    final titles = expected.map((kind) => kind.title).join(' или ');
    return AppMessage(
      kind: MessageKind.warning,
      text: 'Это не тот тип штрихкода: нужен $titles',
      data: {'barcode': barcode, 'expected': titles},
    );
  }

  /// Штрихкод не нашёлся в справочнике товаров документа.
  AppMessage barcodeNotFound({required String barcode}) {
    return AppMessage(
      kind: MessageKind.warning,
      text: 'Такого штрихкода нет в справочнике товаров',
      data: {'barcode': barcode},
    );
  }

  /// Товар по штрихкоду есть, но его нет в составе документа.
  AppMessage barcodeNotInDocument({required String barcode}) {
    return AppMessage(
      kind: MessageKind.warning,
      text: 'Товара с таким штрихкодом нет в документе',
      data: {'barcode': barcode},
    );
  }

  /// Запись в журнал без сообщения пользователю: диагностика подбора видна в «Логах работы».
  void trace(String text, [Map<String, Object?> data = const {}]) {
    _log.add(level: WorkLogLevel.info, text: text, data: data);
  }

  /// Записать сообщение в журнал, озвучить и показать диалогом.
  Future<void> show(BuildContext context, AppMessage message) async {
    _write(message);
    effects.play(message.kind);
    if (!context.mounted) {
      return;
    }

    await showDialog<void>(
      context: context,
      builder: (_) => MessageDialog(message: message),
    );
  }

  /// Записать сообщение в журнал, озвучить и показать баннером снизу.
  void showSnack(BuildContext context, AppMessage message) {
    _write(message);
    effects.play(message.kind);
    final messenger = ScaffoldMessenger.of(context);
    messenger.hideCurrentSnackBar();
    messenger.showSnackBar(SnackBar(content: MessageSnackBar(message: message)));
  }

  static WorkLogLevel levelOf(MessageKind kind) {
    switch (kind) {
      case MessageKind.success:
      case MessageKind.info:
        return WorkLogLevel.info;
      case MessageKind.warning:
        return WorkLogLevel.warning;
      case MessageKind.failure:
        return WorkLogLevel.error;
    }
  }

  void _write(AppMessage message) {
    _log.add(
      level: levelOf(message.kind),
      text: message.text,
      data: message.data,
    );
  }

  static Map<String, Object?> _probeData(ExchangeServer server, ExchangeCallResult result) {
    final raw = result.details.trim();
    return {
      'endpoint': server.endpoint,
      if (raw.isNotEmpty) 'raw': raw,
    };
  }
}
