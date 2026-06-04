const IMPORT_URL = "api/configuration/FrontolConnection/import-from-admin";

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
