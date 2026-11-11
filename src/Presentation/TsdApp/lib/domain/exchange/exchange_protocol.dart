import 'exchange_call_result.dart';
import 'exchange_cancellation.dart';
import 'exchange_server.dart';

const markCollectionDocumentCommand = 'СборМарокПоДокументу';
const markStatusCommand = 'СтатусСбораМарки';
const trueApiTokenCommand = 'ТокенТруАпи';
const exchangeServerMissing = 'Не выбран сервер обмена';
const exchangeAddressMissing = 'Не задан адрес сервера обмена';
const exchangeTimeoutMissing = 'Не задан таймаут запроса';
const exchangeProtocolUnknown = 'Неизвестный протокол обмена';
const exchangeCommandPending = 'Команда ещё не подключена';
const dataMobileReserved = 'Протокол Data Mobile ещё не подключён';
const exchangeProbeSuccess = 'Связь с сервером установлена';
const exchangeProbeFailedPrefix = 'Проверка связи завершилась с ошибкой: ';

ExchangeCallResult? probeTransportBlocked(ExchangeServer server) {
  if (server.host.trim().isEmpty || server.port == 0) {
    return const ExchangeCallResult(success: false, error: exchangeAddressMissing);
  }
  if (server.timeoutSeconds == 0) {
    return const ExchangeCallResult(success: false, error: exchangeTimeoutMissing);
  }
  return null;
}

ExchangeCallResult? probeBlocked(ExchangeServer server) {
  final transport = probeTransportBlocked(server);
  if (transport != null) {
    return transport;
  }
  if (!exchangeProtocolNames.contains(server.protocol)) {
    return const ExchangeCallResult(success: false, error: exchangeProtocolUnknown);
  }
  return null;
}

/// Команды обмена с сервером учётной базы. Имя протокола выбирает реестр, не этот тип.
abstract interface class ExchangeProtocol {
  Future<ExchangeCallResult> probe(ExchangeServer server, ExchangeCancellation cancellation);

  Future<ExchangeCallResult> documentByBarcode(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String command,
    required String barcode,
  });

  Future<ExchangeCallResult> markStatus(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String mark,
  });

  Future<ExchangeCallResult> trueApiToken(ExchangeServer server, ExchangeCancellation cancellation);

  Future<ExchangeCallResult> loadGoods(ExchangeServer server, ExchangeCancellation cancellation);

  Future<ExchangeCallResult> uploadDocument(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required Map<String, Object?> packet,
  });

  Future<ExchangeCallResult> setPrice(
    ExchangeServer server,
    ExchangeCancellation cancellation, {
    required String nomenclature,
    required num price,
  });
}
