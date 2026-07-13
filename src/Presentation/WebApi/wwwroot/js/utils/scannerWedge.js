/**
 * Захват ввода клавиатурного сканера (wedge) с сохранением спецсимволов (включая GS).
 * Между символами — таймаут; завершение скана — по CR (Enter), сам CR в код не включается.
 * preventDefault только пока буфер не пуст — ручной ввод возможен, а поток скана (после 1-го символа) не уходит в адресную строку.
 */
export class ScannerWedge {
    /**
     * @param {{ timeoutMs?: number, onScan?: (code: string) => void, debug?: boolean }} [options]
     */
    constructor(options = {}) {
        this.timeoutMs = options.timeoutMs ?? 50;
        /** @type {(code: string, meta: { capsLock: boolean, cyrillic: boolean }) => void} */
        this.onScan = options.onScan ?? null;
        this.debug = options.debug ?? false;
        this.buffer = [];
        this.timer = null;
        this.active = false;
        this.capsLock = false;
        this._onKeyDown = this._onKeyDown.bind(this);
        this._onKeyUp = this._onKeyUp.bind(this);
    }

    /** Пишет отладочное сообщение в консоль. */
    _log(...args) {
        if (this.debug) {
            console.log("[ScannerWedge]", ...args);
        }
    }

    /** Блокирует действие браузера по клавише (в т.ч. Ctrl+L / F6 → адресная строка). */
    _blockBrowser(e) {
        e.preventDefault();
        e.stopPropagation();
        e.stopImmediatePropagation();
    }

    /** Запускает прослушивание клавиатуры. */
    start() {
        if (this.active) {
            this._log("start() пропущен — уже активен");
            return;
        }

        window.addEventListener("keydown", this._onKeyDown, true);
        window.addEventListener("keyup", this._onKeyUp, true);
        this.active = true;
        this._log("start() — слушатели keydown/keyup на window (capture) включены");
    }

    /** Останавливает прослушивание и очищает буфер. */
    stop() {
        if (!this.active) {
            this._log("stop() пропущен — уже выключен");
            return;
        }

        window.removeEventListener("keydown", this._onKeyDown, true);
        window.removeEventListener("keyup", this._onKeyUp, true);
        this._reset();
        this.active = false;
        this._log("stop() — слушатели выключены");
    }

    /** Очищает буфер и таймер ожидания следующего символа. */
    _reset() {
        if (this.buffer.length > 0) {
            this._log("reset буфера, было символов:", this.buffer.length, JSON.stringify(this.buffer.join("")));
        }

        this.buffer = [];
        this.capsLock = false;

        if (this.timer) {
            clearTimeout(this.timer);
            this.timer = null;
        }
    }

    /**
     * Проверяет наличие кириллицы в строке кода.
     * @param {string} code
     * @returns {boolean}
     */
    _hasCyrillic(code) {
        return /[а-яёА-ЯЁ]/.test(code);
    }

    /** Перезапускает таймер очистки буфера при паузе между символами. */
    _scheduleReset() {
        if (this.timer) {
            clearTimeout(this.timer);
        }

        this.timer = setTimeout(() => {
            this._log("таймаут", this.timeoutMs, "мс — очистка буфера");
            this._reset();
        }, this.timeoutMs);
    }

    /**
     * Преобразует событие клавиши в символ для буфера скана (или null, если это не данные).
     * @param {KeyboardEvent} e
     * @returns {string|null}
     */
    _keyToChar(e) {
        if (e.key === "Enter" || e.key === "\r") {
            return "\r";
        }

        // GS напрямую или как Ctrl+] / F8 / F12 (частые настройки сканеров)
        if (e.key === "\x1d" || e.key === "F8" || e.key === "F12" ||
            (e.ctrlKey && !e.altKey && !e.metaKey && (e.key === "]" || e.code === "BracketRight"))) {
            return "\x1d";
        }

        if (e.key.length === 1 && !e.ctrlKey && !e.altKey && !e.metaKey) {
            return e.key;
        }

        return null;
    }

    /**
     * keyup блокируем только во время накопления скана.
     * @param {KeyboardEvent} e
     */
    _onKeyUp(e) {
        if (this.buffer.length > 0) {
            this._blockBrowser(e);
        }
    }

    /**
     * Обрабатывает нажатие клавиши: накопление символов скана или завершение по CR.
     * @param {KeyboardEvent} e
     */
    _onKeyDown(e) {
        // CapsLock фиксируем один раз за скан (как в эталонном ScannerCheckOnline)
        if (!this.capsLock && typeof e.getModifierState === "function") {
            this.capsLock = e.getModifierState("CapsLock");
        }

        const ch = this._keyToChar(e);
        const scanning = this.buffer.length > 0;

        // Блокируем поток скана и GS (F8/F12/Ctrl+]), иначе браузер перехватывает клавишу
        if (scanning || ch === "\x1d") {
            this._blockBrowser(e);
        }

        this._log("keydown", {
            key: e.key,
            code: e.code,
            ctrl: e.ctrlKey,
            alt: e.altKey,
            scanning,
            capsLock: this.capsLock,
            mapped: ch === "\x1d" ? "{GS}" : ch === "\r" ? "{CR}" : ch,
            bufferLen: this.buffer.length
        });

        if (ch === null) {
            return;
        }

        if (ch === "\r") {
            if (this.buffer.length === 0) {
                return;
            }

            const code = this.buffer.join("").trim();
            const meta = {
                capsLock: this.capsLock,
                cyrillic: this._hasCyrillic(code)
            };
            this._log("скан завершён по CR:", JSON.stringify(code), meta);
            this._reset();

            if (code && typeof this.onScan === "function") {
                this.onScan(code, meta);
            }

            return;
        }

        this.buffer.push(ch);
        this._log("в буфер:", ch === "\x1d" ? "{GS}" : ch, "bufferLen=", this.buffer.length);
        this._scheduleReset();
    }
}
