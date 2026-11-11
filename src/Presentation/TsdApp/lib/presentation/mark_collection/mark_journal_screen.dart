import 'dart:async';

import 'package:flutter/material.dart';

import '../../application/documents/document_loader.dart';
import '../../domain/documents/document_store.dart';
import '../../domain/documents/scanned_document.dart';
import '../../infrastructure/documents/sqlite_document_store.dart';
import '../../infrastructure/settings/app_settings.dart';
import '../../theme/webix_dark_theme.dart';
import 'mark_collection_screen.dart';

/// Журнал сбора марок: сохранённые документы, поиск и новый сбор.
class MarkJournalScreen extends StatefulWidget {
  const MarkJournalScreen({
    super.key,
    required this.settings,
    this.store,
    this.loader,
  });

  final AppSettings settings;
  final DocumentStore? store;
  final DocumentLoader? loader;

  @override
  State<MarkJournalScreen> createState() => _MarkJournalScreenState();
}

class _MarkJournalScreenState extends State<MarkJournalScreen> {
  final _search = TextEditingController();
  late final DocumentStore _store;
  late final DocumentLoader _loader;
  var _documents = <ScannedDocument>[];
  var _request = 0;

  @override
  void initState() {
    super.initState();
    _store = widget.store ?? SqliteDocumentStore();
    _loader = widget.loader ?? DocumentLoader(store: _store);
    unawaited(_reload(trim: true));
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  /// Читает документы от новых к старым. Поиск отбирает строки в Dart, потолка строк нет.
  Future<void> _reload({bool trim = false}) async {
    final request = ++_request;
    var documents = <ScannedDocument>[];
    try {
      if (trim) {
        await _store.trim(days: widget.settings.documentRetentionDays);
      }
      documents = await _store.documents(query: _search.text);
    } catch (_) {
      documents = <ScannedDocument>[];
    }
    if (!mounted || request != _request) {
      return;
    }
    setState(() => _documents = documents);
  }

  Future<void> _open(ScannedDocument? document) async {
    var opened = document;
    if (document != null) {
      opened = await _document(document);
    }
    if (!mounted) {
      return;
    }

    await Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => MarkCollectionScreen(
          settings: widget.settings,
          document: opened,
          store: _store,
          loader: _loader,
        ),
      ),
    );
    if (!mounted) {
      return;
    }
    await _reload(trim: true);
  }

  /// Список не читает тело: состав открывается по целой строке.
  Future<ScannedDocument?> _document(ScannedDocument document) async {
    try {
      return await _store.document(document.id) ?? document;
    } catch (_) {
      return document;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: ColoredBox(
        color: WebixDarkTheme.background,
        child: SafeArea(
          child: Column(
            children: [
              Row(
                children: [
                  TextButton(
                    onPressed: () => Navigator.of(context).pop(),
                    child: const Text('Закрыть'),
                  ),
                  Expanded(
                    child: TextField(
                      controller: _search,
                      decoration: const InputDecoration(
                        hintText: 'Поиск',
                        isDense: true,
                      ),
                      onChanged: (_) => unawaited(_reload()),
                    ),
                  ),
                  const Padding(
                    padding: EdgeInsets.symmetric(horizontal: 8),
                    child: Text('Сбор марок', style: TextStyle(color: WebixDarkTheme.accent)),
                  ),
                  IconButton(
                    onPressed: () => _open(null),
                    icon: const Icon(Icons.add),
                  ),
                ],
              ),
              const Padding(
                padding: EdgeInsets.fromLTRB(16, 8, 16, 4),
                child: Row(
                  children: [
                    Expanded(
                      child: Text(
                        'Документ / Выгружен',
                        style: TextStyle(color: WebixDarkTheme.muted, fontSize: 12),
                      ),
                    ),
                    SizedBox(
                      width: 72,
                      child: Text('Номер', style: TextStyle(color: WebixDarkTheme.muted, fontSize: 12)),
                    ),
                    SizedBox(
                      width: 88,
                      child: Text('Дата', style: TextStyle(color: WebixDarkTheme.muted, fontSize: 12)),
                    ),
                  ],
                ),
              ),
              Expanded(child: _list()),
            ],
          ),
        ),
      ),
    );
  }

  Widget _list() {
    final documents = _documents;
    if (documents.isEmpty) {
      return const Center(child: Text('Нет данных для отображения'));
    }

    return ListView.separated(
      itemCount: documents.length,
      separatorBuilder: (_, _) => const Divider(height: 1, color: WebixDarkTheme.border),
      itemBuilder: (context, index) {
        final document = documents[index];
        return InkWell(
          onTap: () => _open(document),
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(document.presentation),
                      if (document.uploaded)
                        const Text(
                          uploadMark,
                          style: TextStyle(color: WebixDarkTheme.ok, fontSize: 12),
                        ),
                    ],
                  ),
                ),
                SizedBox(width: 72, child: Text(document.number)),
                SizedBox(width: 88, child: Text(formatDocumentDate(document.scannedAt))),
              ],
            ),
          ),
        );
      },
    );
  }
}
