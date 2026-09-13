import 'dart:async';

import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../../infrastructure/scan/camera_data_matrix.dart';
import '../../theme/webix_dark_theme.dart';

/// Полноэкранное сканирование только Data Matrix камерой устройства.
class CameraScanPage extends StatefulWidget {
  const CameraScanPage({super.key});

  @override
  State<CameraScanPage> createState() => _CameraScanPageState();
}

class _CameraScanPageState extends State<CameraScanPage> {
  late final MobileScannerController _controller;
  var _done = false;

  @override
  void initState() {
    super.initState();
    _controller = MobileScannerController(
      formats: const [BarcodeFormat.dataMatrix],
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
      if (barcode.format != BarcodeFormat.dataMatrix) {
        continue;
      }

      final code = CameraDataMatrix.codeFrom(
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
        title: const Text('Data Matrix'),
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
          const Align(
            alignment: Alignment.bottomCenter,
            child: Padding(
              padding: EdgeInsets.only(bottom: 32),
              child: Text(
                'Наведите камеру на Data Matrix',
                style: TextStyle(color: WebixDarkTheme.text),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
