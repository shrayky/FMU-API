/// Пакет ТСД, который нода отдаёт клиенту.
class AppUpdate {
  const AppUpdate({
    required this.versionName,
    required this.versionCode,
    required this.sha256,
    required this.url,
    required this.abi,
  });

  final String versionName;
  final int versionCode;
  final String sha256;
  final String url;
  final String abi;

  String get fileName => 'tsd-$versionName-$versionCode-$abi.apk';

  static AppUpdate? parse(Object? body, {required String abi}) {
    if (body is! Map) {
      return null;
    }

    final versionName = body['versionName'];
    final sha256 = body['sha256'];
    final url = body['url'];
    final versionCode = _asInt(body['versionCode']);
    if (versionName is! String || versionName.isEmpty || versionCode == null || sha256 is! String || url is! String) {
      return null;
    }

    return AppUpdate(
      versionName: versionName,
      versionCode: versionCode,
      sha256: sha256.trim().toLowerCase(),
      url: url,
      abi: abi,
    );
  }

  static int? _asInt(Object? value) {
    if (value is int) {
      return value;
    }
    if (value is num) {
      return value.toInt();
    }
    return null;
  }
}

bool isNewerVersion(int current, int remote) => remote > current;
