const IMPORT_URL = "api/service/couchdb-import";

/// Запускает перенос данных из CouchDB в SQLite. Ответ приходит сразу, копирование идёт в фоне.
export async function startCouchDbImport() {
    const response = await fetch(IMPORT_URL, {
        method: "POST",
        headers: { "Content-Type": "application/json;charset=utf-8" }
    });

    const data = await response.json().catch(() => ({}));

    if (!response.ok)
        throw new Error(data.message ?? "Ошибка запуска переноса данных");

    return data;
}

/// Возвращает текущее состояние переноса.
export async function getCouchDbImport() {
    const response = await fetch(IMPORT_URL);
    const data = await response.json().catch(() => ({}));

    if (!response.ok)
        throw new Error(data.message ?? "Ошибка получения состояния переноса данных");

    return data;
}
