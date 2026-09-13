/// Собирает код Data Matrix с камеры, сохраняя разделитель GS.
class CameraDataMatrix {
  static String? codeFrom({String? rawValue, List<int>? rawBytes}) {
    final fromBytes = rawBytes == null || rawBytes.isEmpty ? null : String.fromCharCodes(rawBytes);
    if (fromBytes != null && fromBytes.contains('\x1d')) {
      return fromBytes;
    }

    final value = rawValue?.trim();
    if (value != null && value.isNotEmpty) {
      return value;
    }

    if (fromBytes != null && fromBytes.trim().isNotEmpty) {
      return fromBytes;
    }

    return null;
  }
}
