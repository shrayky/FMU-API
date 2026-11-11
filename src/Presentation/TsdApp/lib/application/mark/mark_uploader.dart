import '../../domain/documents/document_store.dart';
import '../../domain/documents/scanned_document.dart';
import '../../domain/documents/stored_mark.dart';
import '../../domain/documents/upload_packet.dart';
import '../../domain/exchange/exchange_cancellation.dart';
import '../../domain/exchange/exchange_protocol.dart';
import '../../domain/message/failure_text.dart';
import '../../infrastructure/documents/sqlite_document_store.dart';
import '../../infrastructure/settings/app_settings.dart';
import '../exchange/exchange_protocols.dart';

/// Итог выгрузки документа: успех, текст неудачи или отказ до сети.
class MarkUploadResult {
  const MarkUploadResult({this.error = '', this.blocked = false});

  const MarkUploadResult.uploaded()
      : error = '',
        blocked = false;

  final String error;

  /// Запрос не ушёл в сеть: сервер, адрес или протокол не готовы.
  final bool blocked;

  bool get uploaded => error.isEmpty;
}

/// Отправляет пакет документа сбора на сервер обмена и ставит отметку выгрузки.
class MarkUploader {
  MarkUploader({
    DocumentStore? store,
    ExchangeProtocol? Function(String name)? protocol,
  })  : _store = store ?? SqliteDocumentStore(),
        _protocol = protocol ?? const ExchangeProtocols().byName;

  final DocumentStore _store;
  final ExchangeProtocol? Function(String name) _protocol;

  /// Сколько раз повторяется запись отметки выгрузки, если база занята.
  static const _uploadedAttempts = 3;

  Future<MarkUploadResult> upload(
    AppSettings settings,
    ScannedDocument document,
    ExchangeCancellation cancellation,
  ) async {
    final server = settings.exchangeServer;
    if (server == null) {
      return const MarkUploadResult(error: exchangeServerMissing, blocked: true);
    }

    final blocked = probeBlocked(server);
    if (blocked != null) {
      return MarkUploadResult(error: blocked.error, blocked: true);
    }

    final protocol = _protocol(server.protocol);
    if (protocol == null) {
      return const MarkUploadResult(error: exchangeProtocolUnknown, blocked: true);
    }

    List<StoredMark> marks;
    try {
      marks = await _store.marks(document.id);
    } catch (error) {
      return MarkUploadResult(
        error: '$uploadFailedPrefix${describeFailure(error)}',
      );
    }

    final call = await protocol.uploadDocument(
      server,
      cancellation,
      packet: markUploadPacket(document, marks),
    );
    if (!call.success) {
      return MarkUploadResult(error: '$uploadFailedPrefix${call.error}');
    }

    final answer = parseMarkUploadAnswer(call.body);
    if (!answer.succeeded) {
      return MarkUploadResult(error: answer.error);
    }

    // Сервер уже принял пакет: повторяется только отметка выгрузки.
    // Сообщение об ошибке заставило бы отправить тот же документ второй раз.
    await _markUploaded(document.id);
    return const MarkUploadResult.uploaded();
  }

  /// Отметка выгрузки после принятого пакета: несколько попыток записи в базу.
  Future<void> _markUploaded(String documentId) async {
    for (var attempt = 0; attempt < _uploadedAttempts; attempt++) {
      try {
        await _store.setUploaded(documentId, uploaded: true);
        return;
      } catch (_) {
        // Пакет уже на сервере: сбой отметки не отменяет выгрузку.
      }
    }
  }
}
