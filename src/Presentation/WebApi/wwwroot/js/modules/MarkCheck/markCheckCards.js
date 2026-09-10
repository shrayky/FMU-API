const STATUS_LABELS = {
    EMITTED: "Эмитирован",
    APPLIED: "Нанесён",
    INTRODUCED: "В обороте",
    WRITTEN_OFF: "Списан",
    RETIRED: "Выбыл",
    WITHDRAWN: "Изъят",
    DISAGGREGATION: "Расформирован",
    DISAGGREGATED: "Расформирован",
    WAIT_SHIPMENT: "Ожидает отгрузку",
    EXPORTED: "Экспортирован"
};

let stylesAdded = false;

/**
 * Подключает стили карточек проверки марки.
 */
export function ensureMarkCheckStyles() {
    if (stylesAdded)
        return;

    webix.html.addStyle(`
        .mark-cards {
            padding: 4px 0 16px;
        }
        .mark-card-empty,
        .mark-card-error {
            padding: 16px;
            color: #9ca3af;
            font-size: 14px;
        }
        .mark-card-checking {
            color: #f3f4f6;
            font-size: 16px;
        }
        .mark-card-error {
            color: #f87171;
        }
        .mark-card-row {
            padding: 12px 16px;
            border-bottom: 1px solid rgba(255, 255, 255, 0.08);
        }
        .mark-card-label {
            color: #9ca3af;
            font-size: 13px;
            line-height: 1.3;
        }
        .mark-card-value {
            color: #f3f4f6;
            font-size: 16px;
            line-height: 1.4;
            margin-top: 4px;
            word-break: break-word;
        }
        .mark-card-value.ok {
            color: #4ade80;
        }
        .mark-card-value.warn {
            color: #f87171;
        }
        .mark-json-link .webix_button,
        .mark-json-link button {
            background: transparent !important;
            border: none !important;
            box-shadow: none !important;
            color: #60a5fa !important;
            text-decoration: underline;
            text-align: left;
            padding-left: 4px;
        }
        .mark-scan-field {
            line-height: 36px;
            padding: 0 10px;
            overflow: hidden;
            white-space: nowrap;
            text-overflow: ellipsis;
            font-size: 14px;
        }
        .mark-scan-placeholder {
            color: #9ca3af;
        }
        .mark-scan-status,
        .mark-scan-status .webix_el_box,
        .mark-scan-status input {
            background: transparent !important;
            border: none !important;
            box-shadow: none !important;
        }
        .mark-scan-status input {
            text-align: center;
            font-size: 16px;
            color: #d1d5db !important;
        }
    `);

    stylesAdded = true;
}

/**
 * Собирает HTML карточек из ответа проверки марки.
 */
export function buildCheckingHtml() {
    return emptyHtml("Проверка марки…");
}

export function buildScanHintHtml() {
    return emptyHtml("Сканируйте марку");
}

export function buildMarkCardsHtml(combined) {
    if (!combined)
        return emptyHtml("Нет данных");

    const errors = collectErrors(combined);
    const info = extractCisInfo(combined.trueApi);

    if (!info) {
        if (errors.length > 0)
            return `<div class="mark-cards">${errors.map(errorHtml).join("")}</div>`;

        return emptyHtml("Нет данных");
    }

    const expire = formatDate(info.expireDate || info.expirationDate);
    const expireTone = expireToneFor(info.expireDate || info.expirationDate);
    const verified = extractVerified(combined.permissive);
    const rows = [
        row("Название", info.productName),
        row("Статус", statusValue(info), statusTone(info)),
        row("Срок годности", expire, expireTone),
        row("Владелец", ownerValue(info)),
        row("Бренд", info.brand),
        row("Производитель", producerValue(info)),
        row("Дата производства", formatDate(info.producedDate || info.productionDate)),
        row("Криптозащита КМ", cryptoValue(verified), cryptoTone(verified)),
        row("GTIN", info.gtin),
        row("ТН ВЭД", info.tnVedEaes),
        row("Заявленный объём / вес нетто", weightValue(info))
    ].filter(Boolean);

    const parts = [`<div class="mark-cards">`];
    parts.push(...errors.map(errorHtml));
    parts.push(...rows);
    parts.push("</div>");
    return parts.join("");
}

function extractCisInfo(trueApi) {
    if (!trueApi || trueApi.status !== "ok" || !Array.isArray(trueApi.data))
        return null;

    const item = trueApi.data.find(entry => entry?.cisInfo) || trueApi.data[0];
    return item?.cisInfo || null;
}

function extractVerified(permissive) {
    const codes = permissive?.truemark_response?.codes
        || permissive?.truemark_responses?.[0]?.response?.codes;

    if (!codes?.[0] || codes[0].verified === undefined || codes[0].verified === null)
        return null;

    return Boolean(codes[0].verified);
}

function collectErrors(combined) {
    const errors = [];

    if (combined.permissive?.error)
        errors.push(`Разрешительный режим: ${combined.permissive.error}`);

    if (combined.trueApi && combined.trueApi.status !== "ok") {
        const reason = combined.trueApi.reason || "нет данных True API";
        errors.push(`True API: ${reason}`);
    }

    const item = combined.trueApi?.data?.[0];
    if (item?.errorMessage)
        errors.push(item.errorMessage);

    return errors;
}

function producerValue(info) {
    return info.producerName || info.producerInn || "";
}

function ownerValue(info) {
    if (info.ownerName && info.ownerInn)
        return `${info.ownerName}, ИНН ${info.ownerInn}`;

    return info.ownerName || info.ownerInn || "";
}

function statusValue(info) {
    if (info.markWithdraw && !info.status)
        return "Выбыл";

    if (!info.status)
        return "";

    return STATUS_LABELS[info.status] || info.status;
}

function statusTone(info) {
    if (info.status === "INTRODUCED")
        return "ok";

    if (info.status || info.markWithdraw)
        return "warn";

    return null;
}

function cryptoValue(verified) {
    if (verified === null)
        return "";

    return verified ? "Проверено" : "Не проверено";
}

function cryptoTone(verified) {
    if (verified === null)
        return null;

    return verified ? "ok" : "warn";
}

function weightValue(info) {
    if (info.volumeWeight)
        return info.volumeWeight;

    if (info.productWeight === undefined || info.productWeight === null || info.productWeight === "")
        return "";

    const weight = Number(info.productWeight);
    if (Number.isNaN(weight))
        return String(info.productWeight);

    return Number.isInteger(weight) ? `${weight} г` : `${weight} г`;
}

function expireToneFor(value) {
    const date = parseDate(value);
    if (!date)
        return "ok";

    return date.getTime() >= Date.now() ? "ok" : "warn";
}

function formatDate(value) {
    const date = parseDate(value);
    if (!date)
        return value ? String(value) : "";

    return date.toLocaleDateString("ru-RU");
}

function parseDate(value) {
    if (!value)
        return null;

    if (value instanceof Date && !Number.isNaN(value.getTime()))
        return value;

    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime()))
        return null;

    return parsed;
}

function row(label, value, tone) {
    if (value === undefined || value === null || String(value).trim() === "")
        return "";

    const toneClass = tone ? ` ${tone}` : "";

    return `<div class="mark-card-row">`
        + `<div class="mark-card-label">${escapeHtml(label)}</div>`
        + `<div class="mark-card-value${toneClass}">${escapeHtml(value)}</div>`
        + `</div>`;
}

function errorHtml(text) {
    return `<div class="mark-card-error">${escapeHtml(text)}</div>`;
}

function emptyHtml(text) {
    const extra = text.indexOf("Проверка") === 0 ? " mark-card-checking" : "";
    return `<div class="mark-cards"><div class="mark-card-empty${extra}">${escapeHtml(text)}</div></div>`;
}

function escapeHtml(value) {
    return String(value)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;");
}
