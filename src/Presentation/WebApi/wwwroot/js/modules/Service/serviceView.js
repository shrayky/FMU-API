import { loadParameters } from '../../services/ConfigurationService.js';
import { startCouchDbImport, getCouchDbImport } from '../../services/CouchDbImportService.js';
import { pollingManager } from '../../services/PollingManager.js';

const SQLITE_PROVIDER = "Sqlite";

const POLLING_ID = "couchdb-import-polling";
const INIT_DELAY = 200;
const POLL_INTERVAL = 2000;

const NAMES = {
    button: "couchDbImportButton",
    status: "couchDbImportStatus",
    hint: "couchDbImportHint"
};

const LABELS = {
    formTitle: "FMU-API: Сервис",
    button: "Перенести данные из CouchDB в SQLite",
    confirmation: "Перенести данные из CouchDB в SQLite? Копирование может занять долго, база CouchDB не изменяется.",
    needSqlite: "Перенос доступен при провайдере SQLite. Выберите SQLite в настройках базы данных и перезапустите службу.",
    needCouchDb: "Заполните подключение CouchDB в настройках базы данных: включите базу и укажите адрес, пользователя и пароль.",
    idle: "Перенос не запускался.",
    running: "Идёт перенос данных.",
    completed: "Перенос завершён.",
    failed: "Перенос прерван ошибкой.",
    database: "Текущая база",
    copied: "Записано документов",
    completedDatabases: "Баз готово",
    error: "Ошибка",
    startError: "Ошибка запуска переноса данных",
    stateError: "Ошибка получения состояния переноса данных"
};

export default function serviceView(id) {
    $$("toolbarLabel").setValue(LABELS.formTitle);

    const view = {
        id,
        rows: [
            {
                view: "form",
                elements: [
                    {
                        view: "label",
                        id: NAMES.hint,
                        label: ""
                    },

                    {
                        cols: [
                            {
                                view: "button",
                                id: NAMES.button,
                                value: LABELS.button,
                                width: 360,
                                autowidth: false,
                                disabled: true,
                                click: () => confirmStart()
                            },
                            {}
                        ]
                    },

                    {
                        view: "label",
                        id: NAMES.status,
                        label: LABELS.idle
                    },

                    {}
                ]
            }
        ]
    };

    setTimeout(loadState, INIT_DELAY);

    return view;
}

async function loadState() {
    let config = null;

    try {
        config = await loadParameters();
    } catch (error) {
        console.error("Ошибка загрузки настроек базы данных:", error);
    }

    const database = config?.database ?? {};

    if (database.provider !== SQLITE_PROVIDER) {
        showHint(LABELS.needSqlite);
        return;
    }

    const couchDbIsFilled = Boolean(database.enable) &&
        Boolean(database.netAddress) &&
        Boolean(database.userName) &&
        Boolean(database.password);

    if (!couchDbIsFilled) {
        showHint(LABELS.needCouchDb);
        return;
    }

    setButtonEnabled(true);

    await refreshState();
}

async function refreshState() {
    try {
        const state = await getCouchDbImport();

        renderState(state);

        if (state?.phase === "Running") {
            setButtonEnabled(false);
            startPolling();
            return;
        }

        stopPolling();
        setButtonEnabled(true);
    } catch (error) {
        console.error(error);
        stopPolling();
        setButtonEnabled(true);
    }
}

function startPolling() {
    const polling = pollingManager.getInfo(POLLING_ID);

    if (polling?.isRunning)
        return;

    pollingManager.register(POLLING_ID, refreshState, POLL_INTERVAL, { autoStart: true });
}

function stopPolling() {
    pollingManager.unregister(POLLING_ID);
}

function confirmStart() {
    webix.confirm({
        title: LABELS.formTitle,
        text: LABELS.confirmation,
        callback: async (result) => {
            if (!result)
                return;

            setButtonEnabled(false);

            try {
                renderState(await startCouchDbImport());
                await refreshState();
            } catch (error) {
                console.error(error);
                webix.message({ type: "error", text: error.message ?? LABELS.startError });
                await refreshState();
            }
        }
    });
}

function renderState(state) {
    const label = $$(NAMES.status);

    if (!label)
        return;

    label.setValue(describeState(state));
}

function describeState(state) {
    if (!state || state.phase === "Idle")
        return LABELS.idle;

    if (state.phase === "Running")
        return `${LABELS.running} ${LABELS.database}: ${state.currentDatabase}. ${LABELS.copied}: ${state.copiedInCurrent}. ${LABELS.completedDatabases}: ${state.completedDatabases} из ${state.totalDatabases}.`;

    if (state.phase === "Completed")
        return `${LABELS.completed} ${LABELS.completedDatabases}: ${state.completedDatabases} из ${state.totalDatabases}.`;

    return `${LABELS.failed} ${LABELS.error}: ${state.error}`;
}

function showHint(text) {
    const hint = $$(NAMES.hint);

    if (hint)
        hint.setValue(text);

    renderState(null);
    setButtonEnabled(false);
}

function setButtonEnabled(enabled) {
    const button = $$(NAMES.button);

    if (!button)
        return;

    if (enabled)
        button.enable();
    else
        button.disable();
}
