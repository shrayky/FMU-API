package ru.fmuapi.tsd_app

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.os.Build
import android.os.Bundle
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel

class MainActivity : FlutterActivity() {
    private val scanChannelName = "ru.fmuapi.tsd/scan"
    private val monitorChannelName = "ru.fmuapi.tsd/broadcast-monitor"
    private var scanChannel: MethodChannel? = null
    private var monitorChannel: MethodChannel? = null
    private var scanReceiver: BroadcastReceiver? = null
    private var monitorReceiver: BroadcastReceiver? = null
    private var extraKey: String = "barcode"

    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)
        scanChannel = MethodChannel(flutterEngine.dartExecutor.binaryMessenger, scanChannelName)
        scanChannel?.setMethodCallHandler { call, result ->
            if (call.method == "configure") {
                extraKey = call.argument<String>("extra")?.ifBlank { "barcode" } ?: "barcode"
                registerScanReceiver(call.argument<String>("action").orEmpty())
                result.success(null)
            } else {
                result.notImplemented()
            }
        }

        monitorChannel = MethodChannel(flutterEngine.dartExecutor.binaryMessenger, monitorChannelName)
        monitorChannel?.setMethodCallHandler { call, result ->
            when (call.method) {
                "startMonitor" -> {
                    val fromDart = monitorActionsFrom(call.arguments)
                    result.success(registerMonitorReceiver(fromDart))
                }
                "stopMonitor" -> {
                    unregisterMonitorReceiver()
                    result.success(null)
                }
                else -> result.notImplemented()
            }
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        deliverIntent(intent)
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        deliverIntent(intent)
    }

    override fun onDestroy() {
        unregisterScanReceiver()
        unregisterMonitorReceiver()
        super.onDestroy()
    }

    private fun registerScanReceiver(action: String) {
        unregisterScanReceiver()
        if (action.isBlank()) {
            return
        }

        scanReceiver = object : BroadcastReceiver() {
            override fun onReceive(context: Context?, incoming: Intent?) {
                deliverIntent(incoming)
            }
        }
        registerExported(scanReceiver, IntentFilter(action))
    }

    private fun registerMonitorReceiver(actions: List<String>): Int {
        unregisterMonitorReceiver()
        val unique = (defaultActions + actions)
            .map { it.trim() }
            .filter { it.isNotEmpty() }
            .distinct()
        if (unique.isEmpty()) {
            return 0
        }

        monitorSink = { incoming -> emitMonitor(incoming) }
        monitorReceiver = object : BroadcastReceiver() {
            override fun onReceive(context: Context?, incoming: Intent?) {
                emitMonitor(incoming)
            }
        }

        val filter = IntentFilter()
        for (action in unique) {
            filter.addAction(action)
        }
        filter.priority = IntentFilter.SYSTEM_HIGH_PRIORITY
        registerExported(monitorReceiver, filter)
        return unique.size
    }

    private fun emitMonitor(incoming: Intent?) {
        if (incoming == null) {
            return
        }

        monitorChannel?.invokeMethod(
            "onBroadcast",
            mapOf(
                "action" to incoming.action.orEmpty(),
                "extras" to extrasMap(incoming),
            ),
        )
    }

    private fun registerExported(receiver: BroadcastReceiver?, filter: IntentFilter) {
        if (receiver == null) {
            return
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            registerReceiver(receiver, filter, Context.RECEIVER_EXPORTED)
        } else {
            @Suppress("UnspecifiedRegisterReceiverFlag")
            registerReceiver(receiver, filter)
        }
    }

    private fun unregisterScanReceiver() {
        val current = scanReceiver ?: return
        unregisterReceiver(current)
        scanReceiver = null
    }

    private fun unregisterMonitorReceiver() {
        monitorSink = null
        val current = monitorReceiver ?: return
        unregisterReceiver(current)
        monitorReceiver = null
    }

    private fun deliverIntent(incoming: Intent?) {
        if (incoming == null) {
            return
        }

        val extras = incoming.extras
        val code = extras?.getString(extraKey)
            ?: extras?.getString("barcode")
            ?: extras?.getString("data")
            ?: extras?.getString("com.symbol.datawedge.data_string")
            ?: incoming.dataString

        if (code.isNullOrBlank()) {
            return
        }

        scanChannel?.invokeMethod("onScan", code)
    }

    private fun extrasMap(incoming: Intent): Map<String, String> {
        val extras = incoming.extras ?: return emptyMap()
        val mapped = mutableMapOf<String, String>()
        for (key in extras.keySet()) {
            mapped[key] = runCatching { extraValue(extras, key) }.getOrElse { "<unreadable>" }
        }
        return mapped
    }

    private fun extraValue(extras: Bundle, key: String): String {
        val text = extras.getString(key)
        if (!text.isNullOrEmpty()) {
            return text
        }

        val bytes = extras.getByteArray(key)
        if (bytes != null) {
            return String(bytes, Charsets.UTF_8)
        }

        @Suppress("DEPRECATION")
        return extras.get(key)?.toString().orEmpty()
    }

    companion object {
        val defaultActions = listOf(
            "nlscan.action.SCANNER_RESULT",
            "android.intent.ACTION_DECODE_DATA",
            "android.intent.ACTION_DECODE",
            "com.android.server.scannerservice.broadcast",
            "android.intent.action.SCANRESULT",
            "android.intent.action.RECEIVE_SCAN_RESULT",
            "android.intent.action.BARCODE",
            "android.intent.action.DECODE_DATA",
            "com.symbol.datawedge.api.RESULT_ACTION",
            "com.honeywell.aidc.action.ACTION_BARCODE_DATA",
            "com.honeywell.intent.action.SCAN_RESULT",
            "scan.rcv.message",
            "com.scanner.broadcast",
            "device.scanner.ACTION",
            "device.scanner.EVENT",
            "com.rscja.scanner.action.SCAN_RESULT",
            "com.ubx.decoder.broadcast.SCAN",
            "com.cipherlab.barcodebaseapi.ACTION_BARCODE_DATA",
            "ru.atol.scanner.action.SCAN",
            "com.xcheng.scanner.action.BARCODE_DECODING_BROADCAST",
            "com.sunmi.scanner.ACTION_DATA_CODE_RECEIVED",
            "com.android.scanner.broadcast",
            "scanner.action.BARCODE",
            "com.zebra.scanner.ACTION",
        )

        @Volatile
        var monitorSink: ((Intent) -> Unit)? = null

        fun forwardMonitor(incoming: Intent?) {
            val intent = incoming ?: return
            monitorSink?.invoke(intent)
        }

        fun monitorActionsFrom(arguments: Any?): List<String> {
            val map = arguments as? Map<*, *> ?: return emptyList()
            val raw = map["actions"] as? List<*> ?: return emptyList()
            return raw.map { it.toString() }
        }
    }
}
