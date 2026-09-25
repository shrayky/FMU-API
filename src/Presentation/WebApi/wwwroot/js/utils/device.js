const MOBILE_UA_PATTERN = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini|Windows Phone|Mobile/i;

/**
 * Ширина окна, при которой сайдбар автоматически сворачивается.
 */
export const NARROW_SCREEN_WIDTH = 1280;

/**
 * Определяет, что интерфейс открыт на телефоне или планшете.
 *
 * Ширина окна намеренно не участвует в проверке: компьютер с низким
 * разрешением должен получать обычную версию интерфейса.
 */
export function isMobileDevice() {
    const userAgentData = navigator.userAgentData;

    // Chromium сообщает тип устройства явным флагом (User-Agent Client Hints).
    if (userAgentData && typeof userAgentData.mobile === "boolean") {
        return userAgentData.mobile;
    }

    if (MOBILE_UA_PATTERN.test(navigator.userAgent)) {
        return true;
    }

    return isTouchOnlyDevice();
}

/**
 * Тач-ввод без мыши и тачпада. Проверяется именно coarse-указатель, а не
 * navigator.maxTouchPoints: у ноутбуков с сенсорным экраном основным
 * указателем остаётся мышь, и они не должны считаться телефонами.
 */
function isTouchOnlyDevice() {
    if (typeof window.matchMedia !== "function") {
        return false;
    }

    return window.matchMedia("(hover: none) and (pointer: coarse)").matches;
}

/**
 * Узкое окно — сигнал только для раскладки (автосворачивание сайдбара),
 * а не для выбора мобильной версии интерфейса.
 */
export function isNarrowScreen(threshold = NARROW_SCREEN_WIDTH) {
    return window.innerWidth <= threshold;
}
