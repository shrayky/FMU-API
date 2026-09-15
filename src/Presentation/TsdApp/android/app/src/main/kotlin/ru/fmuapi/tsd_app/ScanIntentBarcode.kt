package ru.fmuapi.tsd_app

import android.os.Bundle

/// Достаёт строку штрихкода из extra broadcast-интента сканера.
object ScanIntentBarcode {
    val preferredKeys = listOf(
        "barcode",
        "barcode_string",
        "data",
        "SCAN_BARCODE1",
        "SCANNER_RESULT",
        "EXTRA_BARCODE_DECODING_DATA",
        "com.symbol.datawedge.data_string",
        "data_string",
        "barcode_data",
        "value",
        "scannerdata",
        "decode_rslt",
        "decode_data",
        "decode_data_disp",
    )

    private val ignoredKeys = setOf(
        "special_keys",
        "charset",
        "codeId",
        "aimId",
        "timestamp",
        "version",
        "length",
        "barcodeType",
        "barcode_type",
    )

    fun fromExtras(extras: Bundle, preferredKey: String): String {
        val preferred = extraValue(extras, preferredKey)
        if (preferred.isNotBlank() && preferredKey !in ignoredKeys) {
            return preferred
        }

        for (key in preferredKeys) {
            val value = extraValue(extras, key)
            if (value.isNotBlank()) {
                return value
            }
        }

        for (key in extras.keySet()) {
            if (key in ignoredKeys) {
                continue
            }
            val value = extraValue(extras, key)
            if (value.isNotBlank()) {
                return value
            }
        }

        return ""
    }

    fun extraValue(extras: Bundle, key: String): String {
        return runCatching { readExtra(extras, key) }.getOrDefault("")
    }

    private fun readExtra(extras: Bundle, key: String): String {
        val text = extras.getString(key)
        if (!text.isNullOrEmpty()) {
            return text
        }

        val sequence = extras.getCharSequence(key)
        if (!sequence.isNullOrEmpty()) {
            return sequence.toString()
        }

        val bytes = extras.getByteArray(key)
        if (bytes != null && bytes.isNotEmpty()) {
            return String(bytes, Charsets.ISO_8859_1)
        }

        @Suppress("DEPRECATION")
        val raw = extras.get(key) ?: return ""
        return when (raw) {
            is ByteArray ->
                if (raw.isEmpty()) {
                    ""
                } else {
                    String(raw, Charsets.ISO_8859_1)
                }
            else -> raw.toString()
        }
    }
}
