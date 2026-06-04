import { disconnectBeerTap, loadBeerTaps } from '../../services/BeerTapsService.js';
import { pollingManager } from '../../services/PollingManager.js';

class BeerTapsView {

    constructor(id) {
        this.id = id;
        this.FormName = "BeerTapsView";

        this.POLL_INTERVAL = 30_000;

        this.LABELS = {
            formTitle: "FMU-API: Пивные краны",
            pollingLabel: "⏳ Обновление...",
            tapName: "Кран",
            wareName: "Товар",
            wareCode: "Код товара",
            markCode: "Марка",
            kegVolume: "Объём кега",
            sales: "Продажи",
            refresh: "Обновить",
            disconnect: "Снять с крана",
            noRowSelected: "Не выбран кран для снятия",
            disconnectConfirm: "Снять пиво с крана «{0}»?",
            disconnectSuccess: "Кег снят с крана",
        };

        this.NAMES = {
            pollingLabel: "beerTapsPollingLabel",
            beerTapsTable: "beerTapsTable",
            refreshButton: "beerTapsRefreshButton",
            disconnectButton: "beerTapsDisconnectButton",
        };
    }

    loadConfig() {
        return this;
    }

    render() {
        $$("toolbarLabel").setValue(this.LABELS.formTitle);

        return {
            view: "form",
            id: this.id,
            name: this.FormName,
            disabled: false,
            elements: [
                this._toolbar(),
                {
                    view: "label",
                    id: this.NAMES.pollingLabel,
                    name: this.NAMES.pollingLabel,
                    label: this.LABELS.pollingLabel,
                    hidden: true,
                },
                {
                    id: this.NAMES.beerTapsTable,
                    view: "datatable",
                    columns: [
                        {
                            id: "tapName",
                            header: this.LABELS.tapName,
                            fillspace: true,
                            sort: "string",
                        },
                        {
                            id: "wareName",
                            header: this.LABELS.wareName,
                            fillspace: true,
                            sort: "string",
                        },
                        {
                            id: "wareCode",
                            header: this.LABELS.wareCode,
                            width: 120,
                            sort: "string",
                        },
                        {
                            id: "markCode",
                            header: this.LABELS.markCode,
                            fillspace: true,
                            sort: "string",
                        },
                        {
                            id: "kegVolume",
                            header: this.LABELS.kegVolume,
                            width: 110,
                            sort: "int",
                        },
                        {
                            id: "sales",
                            header: this.LABELS.sales,
                            width: 90,
                            sort: "int",
                        },
                    ],
                    autoheight: true,
                    scroll: false,
                    select: "row",
                    data: [],
                },
                {}
            ],
        };
    }

    _toolbar() {
        return {
            view: "toolbar",
            borderless: true,
            elements: [
                {
                    view: "button",
                    id: this.NAMES.refreshButton,
                    value: this.LABELS.refresh,
                    width: 120,
                    click: () => this._loadBeerTaps(),
                },
                {
                    view: "button",
                    id: this.NAMES.disconnectButton,
                    value: this.LABELS.disconnect,
                    width: 140,
                    click: () => this._disconnectSelected(),
                },
                {},
            ],
        };
    }

    async _loadBeerTaps() {
        const indicator = $$(this.NAMES.pollingLabel);
        const refreshButton = $$(this.NAMES.refreshButton);
        const disconnectButton = $$(this.NAMES.disconnectButton);

        if (indicator)
            indicator.show();

        if (refreshButton)
            refreshButton.disable();

        if (disconnectButton)
            disconnectButton.disable();

        try {
            const beerTaps = await loadBeerTaps();
            this._updateTable(beerTaps);
        } catch (error) {
            if (error.name === 'TypeError' ||
                error.message?.includes('fetch') ||
                error.message?.includes('Failed to fetch') ||
                error.message?.includes('NetworkError') ||
                error.message?.includes('ERR_CONNECTION_REFUSED')) {
                return;
            }

            console.error("Ошибка при получении списка пивных кранов:", error);
            webix.message("Ошибка при загрузке списка кранов");
        } finally {
            if (indicator)
                indicator.hide();

            if (refreshButton)
                refreshButton.enable();

            if (disconnectButton)
                disconnectButton.enable();
        }
    }

    _disconnectSelected() {
        const table = $$(this.NAMES.beerTapsTable);
        const selectedRow = table?.getSelectedItem();

        if (!selectedRow) {
            webix.message(this.LABELS.noRowSelected);
            return;
        }

        webix.confirm({
            title: "Подтверждение",
            text: this.LABELS.disconnectConfirm.replace("{0}", selectedRow.tapName),
            ok: "Да",
            cancel: "Нет",
        }).then(async () => {
            const disconnectButton = $$(this.NAMES.disconnectButton);

            if (disconnectButton)
                disconnectButton.disable();

            try {
                await disconnectBeerTap(selectedRow.markCode);
                webix.message(this.LABELS.disconnectSuccess);
                await this._loadBeerTaps();
            } catch (error) {
                console.error("Ошибка при снятии с крана:", error);
                webix.message(error.message || "Ошибка при снятии с крана");
            } finally {
                if (disconnectButton)
                    disconnectButton.enable();
            }
        });
    }

    startPolling() {
        const pollBeerTaps = async () => {
            await this._loadBeerTaps();
        };

        pollingManager.register(
            'beer-taps-polling',
            pollBeerTaps,
            this.POLL_INTERVAL,
            {
                initialDelay: 100,
                autoStart: true,
            }
        );
    }

    _updateTable(beerTaps) {
        const table = $$(this.NAMES.beerTapsTable);

        if (!table)
            return;

        const tableData = (beerTaps ?? []).map((row, index) => ({
            id: row.id || `tap_${index}`,
            tapName: row.tapName || "—",
            wareName: row.wareName || "—",
            wareCode: row.wareCode || "—",
            markCode: row.markCode || "—",
            kegVolume: row.kegVolume ?? 0,
            sales: row.sales ?? 0,
        }));

        table.clearAll();
        table.parse(tableData);
    }
}

export default function beerTapsView(id) {
    const view = new BeerTapsView(id).loadConfig();
    const layout = view.render();

    setTimeout(() => { view.startPolling(); }, 500);

    return layout;
}
