import 'dart:async';

import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../../domain/scan/barcode_kind.dart';
import '../../infrastructure/scan/camera_code.dart';
import '../../theme/webix_dark_theme.dart';

/// Формат камеры для вида штрихкода.
BarcodeFormat cameraFormatOf(BarcodeKind kind) {
  return switch (kind) {
    BarcodeKind.document => BarcodeFormat.qrCode,
    BarcodeKind.mark => BarcodeFormat.dataMatrix,
    BarcodeKind.ean13 => BarcodeFormat.ean13,
  };
}

/// Полноэкранное сканирование камерой устройства только заданных видов штрихкода.
class CameraScanPage extends StatefulWidget {
  const CameraScanPage({super.key, required this.kinds}) : assert(kinds.length != 0);

  /// Какие виды штрихкода считывать. Ошибку типа решает экран, а не камера.
  final List<BarcodeKind> kinds;

  @override
  State<CameraScanPage> createState() => _CameraScanPageState();
}

class _CameraScanPageState extends State<CameraScanPage> {
  late final MobileScannerController _controller;
  var _done = false;

  List<BarcodeFormat> get _formats {
    return [for (final kind in widget.kinds) cameraFormatOf(kind)];
  }

  String get _label => widget.kinds.map((kind) => kind.title).join(', ');

  @override
  void initState() {
    super.initState();
    _controller = MobileScannerController(
      formats: _formats,
      detectionSpeed: DetectionSpeed.normal,
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _onDetect(BarcodeCapture capture) {
    if (_done) {
      return;
    }

    for (final barcode in capture.barcodes) {
      final code = CameraCode.codeFrom(
        rawValue: barcode.rawValue,
        rawBytes: _bytes(barcode),
      );
      if (code == null) {
        continue;
      }

      _done = true;
      unawaited(_finish(code));
      return;
    }
  }

  Future<void> _finish(String code) async {
    try {
      await _controller.stop();
    } catch (_) {}
    if (!mounted) {
      return;
    }

    Navigator.of(context).pop(code);
  }

  static List<int>? _bytes(Barcode barcode) {
    final decoded = barcode.rawDecodedBytes;
    if (decoded is DecodedBarcodeBytes) {
      return decoded.bytes;
    }

    if (decoded is DecodedVisionBarcodeBytes) {
      return decoded.bytes ?? decoded.rawBytes;
    }

    return null;
  }

  static String _errorText(MobileScannerException error) {
    switch (error.errorCode) {
      case MobileScannerErrorCode.permissionDenied:
        return 'Нет доступа к камере';
      case MobileScannerErrorCode.unsupported:
        return 'Камера недоступна на этом устройстве';
      default:
        return 'Не удалось открыть камеру';
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: WebixDarkTheme.background,
      appBar: AppBar(
        title: Text(_label),
      ),
      body: Stack(
        fit: StackFit.expand,
        children: [
          MobileScanner(
            controller: _controller,
            onDetect: _onDetect,
            errorBuilder: (context, error) {
              return Center(
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Text(
                    _errorText(error),
                    textAlign: TextAlign.center,
                    style: const TextStyle(color: WebixDarkTheme.text),
                  ),
                ),
              );
            },
          ),
          Align(
            alignment: Alignment.bottomCenter,
            child: Padding(
              padding: const EdgeInsets.only(bottom: 32),
              child: Text(
                'Наведите камеру на $_label',
                style: const TextStyle(color: WebixDarkTheme.text),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
