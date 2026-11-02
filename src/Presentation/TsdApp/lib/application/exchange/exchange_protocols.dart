import 'package:http/http.dart' as http;

import '../../domain/exchange/exchange_protocol.dart';
import '../../domain/exchange/exchange_server.dart';
import '../../infrastructure/exchange/data_mobile_protocol.dart';
import '../../infrastructure/exchange/ds_tsd_protocol.dart';

/// Выбор реализации обмена по имени на карточке сервера.
class ExchangeProtocols {
  const ExchangeProtocols();

  ExchangeProtocol? byName(String name, {http.Client Function()? createClient}) {
    switch (name) {
      case defaultExchangeProtocol:
        return DsTsdProtocol(httpClient: createClient?.call());
      case dataMobileProtocol:
        return const DataMobileProtocol();
      default:
        return null;
    }
  }
}
