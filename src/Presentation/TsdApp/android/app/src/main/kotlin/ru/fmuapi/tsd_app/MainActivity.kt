package ru.fmuapi.tsd_app

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.os.SystemClock
import android.provider.Settings
import androidx.core.content.FileProvider
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodCall
import io.flutter.plugin.common.MethodChannel
import java.io.File

class MainActivity : FlutterActivity() {
    private val scanChannelName = "ru.fmuapi.tsd/scan"
    private val monitorChannelName = "ru.fmuapi.tsd/broadcast-monitor"
    private val updateChannelName = "ru.fmuapi.tsd/update"
    private val exportChannelName = "ru.fmuapi.tsd/export"
    private var scanChannel: MethodChannel? = null
    private var monitorChannel: MethodChannel? = null
    private var scanReceiver: BroadcastReceiver? = null
    private var monitorReceiver: BroadcastReceiver? = null
    private var extraKey: String = "barcode"
    private var lastScanAction: String = ""
    private var lastMonitorActions: List<String> = emptyList()
    private var monitorActive: Boolean = false
    private var lastScanCode: String = ""
    private var lastScanAt: Long = 0

    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)
        scanChannel = MethodChannel(flutterEngine.dartExecutor.binaryMessenger, scanChannelName)
        scanChannel?.setMethodCallHandler { call, result ->
            if (call.method == "configure") {
                extraKey = call.argument<String>("extra")?.ifBlank { "barcode" } ?: "barcode"
                lastScanAction = call.argument<String>("action").orEmpty()
                registerScanReceiver(lastScanAction)
                result.success(null)
            } else {
                result.notImplemented()
            }
        }
        registerScanReceiver("")

        val updateChannel = MethodChannel(flutterEngine.dartExecutor.binaryMessenger, updateChannelName)
        updateChannel.setMethodCallHandler { call, result ->
            when (call.method) {
                "abi" -> result.success(preferredAbi())
                "updatesDirectory" -> {
                    val directory = File(cacheDir, "updates")
                    directory.mkdirs()
                    result.success(directory.absolutePath)
                }
                "install" -> installPackage(call.argument<String>("path").orEmpty(), result)
                else -> result.notImplemented()
            }
        }

        monitorChannel = MethodChannel(flutterEngine.dartExecutor.binaryMessenger, monitorChannelName)
        monitorChannel?.setMethodCallHandler { call, result ->
            when (call.method) {
                "startMonitor" -> {
                    lastMonitorActions = monitorActionsFrom(call.arguments)
                    monitorActive = true
                    result.success(registerMonitorReceiver(lastMonitorActions))
                }
                "stopMonitor" -> {
                    monitorActive = false
                    unregisterMonitorReceiver()
                    result.success(null)
                }
                else -> result.notImplemented()
            }
        }

        val exportChannel = MethodChannel(flutterEngine.dartExecutor.binaryMessenger, exportChannelName)
        exportChannel.setMethodCallHandler { call, result ->
            when (call.method) {
                "exportsDirectory" -> {
                    val directory = File(cacheDir, "exports")
                    directory.mkdirs()
                    result.success(directory.absolutePath)
                }
                "share" -> shareFiles(call, result)
                else -> result.notImplemented()
            }
        }
    }

    /// Системное меню «Поделиться»: файл копии базы уходит в выбранное приложение.
    private fun shareFiles(call: MethodCall, result: MethodChannel.Result) {
        val paths = call.argument<List<String>>("paths").orEmpty()
        if (paths.isEmpty()) {
            result.error("empty", "Нет файлов для отправки", null)
            return
        }

        val uris = ArrayList<Uri>()
        for (path in paths) {
            val file = File(path)
            if (!file.isFile) {
                result.error("file", "Файл не найден: $path", null)
                return
            }
            uris.add(FileProvider.getUriForFile(this, "$packageName.fileprovider", file))
        }

        val intent = if (uris.size == 1) {
            Intent(Intent.ACTION_SEND).apply { putExtra(Intent.EXTRA_STREAM, uris[0]) }
        } else {
            Intent(Intent.ACTION_SEND_MULTIPLE).apply {
                putParcelableArrayListExtra(Intent.EXTRA_STREAM, uris)
            }
        }
        intent.type = call.argument<String>("type")?.ifBlank { "*/*" } ?: "*/*"
        call.argument<String>("subject")?.takeIf { it.isNotBlank() }?.let {
            intent.putExtra(Intent.EXTRA_SUBJECT, it)
        }
        intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
        startActivity(Intent.createChooser(intent, "Поделиться базой документов"))
        result.success(null)
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        deliverIntent(intent)
    }

    override fun onResume() {
        super.onResume()
        registerScanReceiver(lastScanAction)
        if (monitorActive) {
            registerMonitorReceiver(lastMonitorActions)
        }
    }

    override fun onPause() {
        unregisterScanReceiver()
        unregisterMonitorReceiver()
        super.onPause()
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
        lastScanAction = action
        unregisterScanReceiver()
        val actions = (listOf(action.trim()) + defaultActions)
            .map { it.trim() }
            .filter { it.isNotEmpty() }
            .distinct()
        if (actions.isEmpty()) {
            return
        }

        scanReceiver = object : BroadcastReceiver() {
            override fun onReceive(context: Context?, incoming: Intent?) {
                deliverIntent(incoming)
            }
        }
        val filter = IntentFilter()
        for (scanAction in actions) {
            filter.addAction(scanAction)
        }
        filter.addCategory(Intent.CATEGORY_DEFAULT)
        registerExported(scanReceiver, filter)
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

        monitorReceiver = object : BroadcastReceiver() {
            override fun onReceive(context: Context?, incoming: Intent?) {
                emitMonitor(incoming)
            }
        }

        val filter = IntentFilter()
        for (action in unique) {
            filter.addAction(action)
        }
        filter.addCategory(Intent.CATEGORY_DEFAULT)
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
        val current = monitorReceiver ?: return
        unregisterReceiver(current)
        monitorReceiver = null
    }

    private fun deliverIntent(incoming: Intent?) {
        if (incoming == null || incoming.action == Intent.ACTION_MAIN) {
            return
        }

        val extras = incoming.extras
        val fromExtras = extras?.let { ScanIntentBarcode.fromExtras(it, extraKey) }.orEmpty()
        val code = fromExtras.ifBlank { incoming.dataString.orEmpty() }

        if (code.isBlank()) {
            return
        }

        val now = SystemClock.elapsedRealtime()
        if (code == lastScanCode && now - lastScanAt < 400) {
            return
        }
        lastScanCode = code
        lastScanAt = now

        scanChannel?.invokeMethod("onScan", code)
    }

    private fun preferredAbi(): String {
        if (Build.SUPPORTED_ABIS.any { it.equals("arm64-v8a", ignoreCase = true) }) {
            return "arm64-v8a"
        }
        return "armeabi-v7a"
    }

    private fun installPackage(path: String, result: MethodChannel.Result) {
        val file = File(path)
        if (!file.isFile) {
            result.error("file", "Файл обновления не найден", null)
            return
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O && !packageManager.canRequestPackageInstalls()) {
            startActivity(
                Intent(
                    Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES,
                    Uri.parse("package:$packageName"),
                ),
            )
            result.error("permission", "Разрешите установку из этого приложения", null)
            return
        }

        val uri = FileProvider.getUriForFile(this, "$packageName.fileprovider", file)
        val intent = Intent(Intent.ACTION_VIEW).apply {
            setDataAndType(uri, "application/vnd.android.package-archive")
            addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
            addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
        }
        startActivity(intent)
        result.success(null)
    }

    private fun extrasMap(incoming: Intent): Map<String, String> {
        val extras = incoming.extras ?: return emptyMap()
        val mapped = mutableMapOf<String, String>()
        for (key in extras.keySet()) {
            mapped[key] = runCatching { ScanIntentBarcode.extraValue(extras, key) }.getOrElse { "<unreadable>" }
        }
        return mapped
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

        fun monitorActionsFrom(arguments: Any?): List<String> {
            val map = arguments as? Map<*, *> ?: return emptyList()
            val raw = map["actions"] as? List<*> ?: return emptyList()
            return raw.map { it.toString() }
        }
    }
}
