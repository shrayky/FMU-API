import { ApiServerAddress } from '../../utils/net.js';
import { ScannerWedge } from '../../utils/scannerWedge.js';

class MarksView {
    constructor(id) {
        this.formName = "MarksView";
        this.id = id;

        this.marksApiAddress = "/api/marks";
        this.currentPage = 1;
        this.pageSize = 50;
        this.searchTerm = "";

        this.LABELS = {
            formTitle: "FMU-API: Марки",
            search: "Поиск по номеру марки",
            totalMarks: "Всего марок: ",
            noData: "Нет данных",
            markId: "Номер марки",
            state: "Состояние",
            checkDate: "Дата проверки",
            onlineCheck: "On-line проверка",
            offlineCheck: "Off-line проверка"
        }

        this.NAMES = {
            toolbarLabel: "toolbarLabel",
            searchInput: "searchInput",
            marksTable: "marksTable",
            paginationInfo: "paginationInfo",
            prevButton: "prevButton",
            nextButton: "nextButton"
        }

        this.scanner = new ScannerWedge({
            timeoutMs: 50,
            onScan: (code, meta) => this._onScan(code, meta)
        });
    }

    loadConfig() {
        return this;
    }
    
    render() {
        $$("toolbarLabel").setValue(this.LABELS.formTitle);

        var formElements = [
            {
                view: "toolbar",
                borderless: true,
                elements: [
                    {
                        view: "text",
                        id: this.NAMES.searchInput,
                        placeholder: this.LABELS.search,
                        width: 300,
                        on: {
                            onTimedKeyPress: () => this._onSearch()
                        }
                    },
                    {
                        view: "button",
                        value: "Поиск",
                        width: 100,
                        click: () => this._onSearch()
                    },
                    {},
                    {
                        view: "button",
                        id: this.NAMES.prevButton,
                        value: "←",
                        width: 50,
                        disabled: true,
                        click: () => this._goToPage(this.currentPage - 1)
                    },
                    {
                        view: "label",
                        id: this.NAMES.paginationInfo,
                        label: "",
                        width: 150,
                        align: "center"
                    },
                    {
                        view: "button",
                        id: this.NAMES.nextButton,
                        value: "→",
                        width: 50,
                        disabled: true,
                        click: () => this._goToPage(this.currentPage + 1)
                    }

                ]
            },
            this._marksTable(),
            {}
        ];

        var form = {
            view: "form",
            id: this.id,
            name: this.formName,
            disabled: true,
            elements: formElements,
            on: {
                onAfterRender: () => {
                    this.scanner.start();
                    setTimeout(() => {
                        const searchInput = $$(this.NAMES.searchInput);
                        if (searchInput) {
                            searchInput.focus();
                        }
                    }, 50);
                },
                onDestruct: () => {
                    this.scanner.stop();
                }
            }
        }

        return form;
    }

    delayedDataLoading() {
        setTimeout(() => {
            this._loadMarks();
        }, 500);

        this.scanner.start();

        return this;
    }

    _marksTable() {
        return {
            id: this.NAMES.marksTable,
            view: "datatable",
            columns: [
                { 
                    id: "markId", 
                    header: this.LABELS.markId, 
                    width: 300, 
                    sort: "string",
                    fillspace: true,
                    format: webix.template.escape
                },
                { 
                    id: "state",
                    header: this.LABELS.state,
                    width: 120,
                    sort: "string",
                },
                { 
                    id: "checkDate", 
                    header: this.LABELS.checkDate, 
                    width: 150, 
                    sort: "date",
                }
            ],
            autoheight: true,
            scroll: false,
            select: false,
            data: [],
        }
    }

    _paginationControls() {
        return {
            view: "toolbar",
            elements: [
                {
                    view: "button",
                    id: this.NAMES.prevButton,
                    value: "←",
                    width: 50,
                    disabled: true,
                    click: () => this._goToPage(this.currentPage - 1)
                },
                {
                    view: "label",
                    id: this.NAMES.paginationInfo,
                    label: this.LABELS.page + "1",
                    width: 150
                },
                {
                    view: "button",
                    id: this.NAMES.nextButton,
                    value: "→",
                    width: 50,
                    disabled: true,
                    click: () => this._goToPage(this.currentPage + 1)
                },
                {}
            ]
        }
    }

    async _loadMarks() {
        
        let form = $$(this.id);

        webix.extend(form, webix.ProgressBar);
        form.showProgress({
            type: "icon",
        });

        const url = new URL(this.marksApiAddress, window.location.origin);
        url.searchParams.set('page', this.currentPage.toString());
        url.searchParams.set('pageSize', this.pageSize.toString());
        
        if (this.searchTerm) {
            url.searchParams.set('search', this.searchTerm);
        }

        let data;

        try {
            const response = await fetch(url);
            
            if (!response.ok) {
                throw new Error(`Ошибка получения данных ${response.status} ${response.statusText}`);
            }

            data = await response.json();


        } catch (error) {
            if (error.name === 'TypeError' || 
                error.message.includes('fetch') || 
                error.message.includes('Failed to fetch') ||
                error.message.includes('NetworkError') ||
                error.message.includes('ERR_CONNECTION_REFUSED')) {
                return;
            }
            form.hideProgress();
            console.error("Ошибка при загрузке марок:", error);
            webix.message({ text: "Ошибка при загрузке марок", type: "error" });
        }
        
        if (!data) {
            form.hideProgress();
            return;
        }

        this._updateMarksTable(data);
        this._updatePagination(data);

        form.enable();
        form.hideProgress();
    }

    _updateMarksTable(data) {
        const table = $$(this.NAMES.marksTable);
        
        if (!table)
            return;

        const tableData = data.marks.map(mark => ({
            id: mark.id,
            markId: mark.markId,
            state: mark.state,
            checkDate: new Date(mark.trueApiAnswerProperties.reqTimestamp).toLocaleString(),
            haveTrueApiAnswer: mark.haveTrueApiAnswer
        }));

        table.clearAll();
        table.parse(tableData);
        table.resize();
    }

    _updatePagination(data) {
        const prevButton = $$(this.NAMES.prevButton);
        const nextButton = $$(this.NAMES.nextButton);
        const paginationInfo = $$(this.NAMES.paginationInfo);

        if (prevButton) {
            prevButton.enable();
            if (data.currentPage <= 1) {
                prevButton.disable();
            }
        }

        if (nextButton) {
            nextButton.enable();
            if (data.currentPage >= data.totalPages) {
                nextButton.disable();
            }
        }

        if (paginationInfo) {
            paginationInfo.setValue(
                `${data.currentPage} из ${data.totalPages}`
            );
        }
    }

    _toSgtin(markCode) {
        let code = (markCode || "").trim().replace(/\\u001d/gi, "\x1d");

        if (code.startsWith("01")) {
            const gsPos = code.indexOf("\x1d");
            if (gsPos > 0) {
                const gtin = code.substring(2, 16);
                const serial = code.substring(18, gsPos);
                return gtin + serial;
            }
        }

        if (code.length === 29) {
            return code.substring(0, 21);
        }

        return code;
    }

    _onScan(code, meta = {}) {
        const sgtin = this._toSgtin(code);
        const searchInput = $$(this.NAMES.searchInput);

        if (searchInput) {
            searchInput.setValue(sgtin);
            searchInput.focus();
        }

        const warnings = [];
        if (meta.capsLock) {
            warnings.push("Включён Caps Lock — раскладка сканера может исказить код");
        }
        if (meta.cyrillic) {
            warnings.push("В штрихкоде есть русские символы — проверьте раскладку клавиатуры");
        }

        if (warnings.length > 0) {
            webix.message({
                text: warnings.join(". "),
                type: "error",
                expire: 5000
            });
        }

        this.searchTerm = sgtin;
        this.currentPage = 1;
        this._loadMarks();
    }

    _onSearch() {
        const searchInput = $$(this.NAMES.searchInput);
        if (searchInput) {
            this.searchTerm = searchInput.getValue();
            searchInput.focus();
        }
        this.currentPage = 1;
        this._loadMarks();
    }

    _goToPage(page) {
        if (page >= 1) {
            this.currentPage = page;
            this._loadMarks();
        }
    }
}

export default function (id) {
    const view = new MarksView(id)
        .loadConfig()
        .delayedDataLoading()
        .render();

    return view;
}
