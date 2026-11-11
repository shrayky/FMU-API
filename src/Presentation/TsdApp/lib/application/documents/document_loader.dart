import '../../domain/documents/document_load_result.dart';
import '../../domain/documents/document_packet.dart';
import '../../domain/documents/document_store.dart';
import '../../domain/documents/scanned_document.dart';
import '../../domain/exchange/exchange_cancellation.dart';
import '../../domain/exchange/exchange_protocol.dart';
import '../../domain/message/failure_text.dart';
import '../../infrastructure/documents/sqlite_document_store.dart';
import '../../infrastructure/settings/app_settings.dart';
import '../exchange/exchange_protocols.dart';

/// Загружает документ сбора по штрихкоду и пишет его в основную базу.
class DocumentLoader {
  DocumentLoader({
    DocumentStore? store,
    ExchangeProtocol? Function(String name)? protocol,
  })  : _store = store ?? SqliteDocumentStore(),
        _protocol = protocol ?? const ExchangeProtocols().byName;

  final DocumentStore _store;
  final ExchangeProtocol? Function(String name) _protocol;

  Future<DocumentLoadResult> load(
    AppSettings settings,
    String barcode,
    ExchangeCancellation cancellation,
  ) async {
    final server = settings.exchangeServer;
    if (server == null) {
      return const DocumentLoadResult.blocked(exchangeServerMissing);
    }

    final blocked = probeBlocked(server);
    if (blocked != null) {
      return DocumentLoadResult.blocked(blocked.error);
    }

    final protocol = _protocol(server.protocol);
    if (protocol == null) {
      return const DocumentLoadResult.blocked(exchangeProtocolUnknown);
    }

    final call = await protocol.documentByBarcode(
      server,
      cancellation,
      command: markCollectionDocumentCommand,
      barcode: barcode,
    );
    if (!call.success) {
      return DocumentLoadResult.failed(call.error, details: call.details);
    }

    final packet = parseDocumentPacket(call.body);
    if (packet == null) {
      return const DocumentLoadResult.failed(documentMissing);
    }

    final ScannedDocument document;
    try {
      document = await _store.save(packet);
    } catch (error) {
      return DocumentLoadResult.failed(
        'Не удалось сохранить документ: ${describeFailure(error)}',
        details: '$error',
      );
    }

    try {
      await _store.trim(days: settings.documentRetentionDays);
    } catch (_) {
      // Документ уже записан: сбой очистки срока хранения его не отменяет,
      // иначе повторный скан QR создаёт второй документ.
    }

    return DocumentLoadResult.saved(document);
  }
}
