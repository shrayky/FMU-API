package ru.fmuapi.tsd_app

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent

/// Принимает broadcast сканера из манифеста и отдаёт в MainActivity.
class ScanMonitorReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context?, incoming: Intent?) {
        MainActivity.forwardMonitor(incoming)
    }
}
