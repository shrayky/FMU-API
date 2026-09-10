/**
 * Периодически читает буфер обмена и сообщает новую марку, если она отличается от предыдущей.
 */
export class ClipboardMarkWatcher {
    /**
     * @param {{
     *   intervalMs?: number,
     *   onCode?: (code: string) => void
     * }} [options]
     */
    constructor(options = {}) {
        this.intervalMs = options.intervalMs ?? 350;
        this.onCode = options.onCode ?? null;
        this._timer = null;
        this._last = "";
        this._busy = false;
        this._onPaste = this._onPaste.bind(this);
    }

    start() {
        this.stop();
        document.addEventListener("paste", this._onPaste, true);
        this._timer = setInterval(() => this._poll(), this.intervalMs);
        this._poll();
    }

    stop() {
        document.removeEventListener("paste", this._onPaste, true);
        if (this._timer) {
            clearInterval(this._timer);
            this._timer = null;
        }
    }

    remember(code) {
        this._last = this._normalize(code);
    }

    _normalize(value) {
        return String(value || "")
            .replace(/\u241d/g, "\x1d")
            .replace(/[\r\n\t]+/g, "")
            .trim();
    }

    _looksLikeMark(code) {
        if (code.length < 14)
            return false;

        if (code.indexOf("\x1d") >= 0)
            return true;

        if (code.startsWith("01") && code.length >= 18)
            return true;

        return /^\d{13,14}$/.test(code) || code.length >= 25;
    }

    _emit(code) {
        const normalized = this._normalize(code);
        if (!normalized || normalized === this._last || !this._looksLikeMark(normalized))
            return;

        this._last = normalized;
        if (typeof this.onCode === "function")
            this.onCode(normalized);
    }

    _onPaste(e) {
        const text = e.clipboardData?.getData("text") || "";
        if (text)
            this._emit(text);
    }

    async _poll() {
        if (this._busy || !navigator.clipboard || typeof navigator.clipboard.readText !== "function")
            return;

        this._busy = true;
        try {
            this._emit(await navigator.clipboard.readText());
        } catch {
            // нет разрешения или не HTTPS — оставляем клавиатурный захват
        } finally {
            this._busy = false;
        }
    }
}
