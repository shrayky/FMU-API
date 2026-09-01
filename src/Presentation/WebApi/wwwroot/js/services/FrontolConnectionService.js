const IMPORT_URL = "api/configuration/FrontolConnection/import-from-admin";
const LOAD_BEER_TAPS_URL = "api/configuration/FrontolConnection/load-beer-taps";

/// Загружает подключения из Frontol.Администратор (FrontolAdmin.ini).
export async function importFromFrontolAdmin() {
    const response = await fetch(IMPORT_URL);

    if (response.status === 404) {
        const data = await response.json();
        throw new Error(data.message ?? "Файл FrontolAdmin.ini не найден");
    }

    if (!response.ok)
        throw new Error("Ошибка импорта подключений Frontol");

    return response.json();
}

export async function loadBeerTapsFromFrontol(connectionId) {
    const response = await fetch(`${LOAD_BEER_TAPS_URL}?connectionId=${connectionId}`, { method: "POST" });
    const data = await response.json().catch(() => ({}));

    if (!response.ok)
        throw new Error(data.message ?? "Ошибка загрузки кранов из Frontol");

    return data;
}
