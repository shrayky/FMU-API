export const TIME_FORMAT_24H = "%H:%i";

/**
 * Разбирает время "HH:mm" или "HH:mm:ss" в Date для поля выбора времени.
 * Значимы только часы, минуты и секунды; пустое значение даёт полночь.
 */
export function parseTime(value) {
    const parts = String(value ?? "").split(":");
    const hours = parseInt(parts[0], 10) || 0;
    const minutes = parseInt(parts[1], 10) || 0;
    const seconds = parseInt(parts[2], 10) || 0;

    return new Date(2000, 0, 1, hours, minutes, seconds);
}

/**
 * Форматирует значение поля выбора времени в "HH:mm:ss" для TimeOnly.
 */
export function formatTime(value) {
    if (!value)
        return "00:00:00";

    const date = value instanceof Date ? value : new Date(value);
    const hours = date.getHours().toString().padStart(2, "0");
    const minutes = date.getMinutes().toString().padStart(2, "0");
    const seconds = date.getSeconds().toString().padStart(2, "0");

    return `${hours}:${minutes}:${seconds}`;
}
