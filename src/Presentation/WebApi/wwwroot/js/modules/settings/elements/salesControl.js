import { Label, Text, Number, padding, CheckBox } from "../../../utils/ui.js";
import { windowsPathValidation } from "../../../utils/validators.js";

class SaleControllsConfigurationElement {
    constructor(id) {
        this.id = id;
        this.SETTINGS_ID = "saleControlls";
        this.MARK_CHECK_RESULT_SAVE_ID = "markCheckResultSaveSettings";
        this.LABELS = {
            title: "Контроль при продаже товаров",
            banSalesReturnedWares: "Блокировать продажу возвращенных товаров",
            ignoreVerificationErrorForTrueApiGroups: "Коды групп товаров игнорирующик проверку статусов кода маркировки в Честном Знаке",
            checkIsOwnerField: "Проверять владельца марки средствами fmu-api",
            forFrontolMoreThen205: "Настройки для тарифного фронтола (6.21.0 и выше):",
            checkReceiptReturn: "Проверять товары из чеков возврата",
            correctExpireDateInReturns: "Исправлять истекший срок годности в чеках возврата",
            sendLocalModuleInformationalInRequestId: "Отправлять информацию о локальном модуле для тега 1265 в requestId",
            resetSoldStatusForReturn: "Для возвратов - изменять стаус `Проданно` для товаров (тарифный фронтол до 25 версии)",
            useBeerTaps: "Использовать пивные краны",
            markCheckResultSaveTitle: "Сохранение результатов проверки марки в текустовый файл (для печсати через скрипты драйвера ккт атол):",
            markCheckResultSaveEnable: "Сохранять результаты проверки марки в файлы",
            markCheckResultSaveDirectory: "Каталог для сохранения файлов",
            markCheckResultSaveFileLifespanHours: "Время жизни файлов (часов)",
        };
    }

    loadConfig(config) {
        if (config?.saleControlConfig) {
            this.banSalesReturnedWares = config.saleControlConfig.banSalesReturnedWares;
            this.ignoreVerificationErrorForTrueApiGroups = config.saleControlConfig.ignoreVerificationErrorForTrueApiGroups;
            this.checkIsOwnerField = config.saleControlConfig.checkIsOwnerField;
            this.checkReceiptReturn = config.saleControlConfig.checkReceiptReturn;
            this.correctExpireDateInSaleReturn = config.saleControlConfig.correctExpireDateInSaleReturn;
            this.sendLocalModuleInformationalInRequestId = config.saleControlConfig.sendLocalModuleInformationalInRequestId;
            this.resetSoldStatusForReturn = config.saleControlConfig.resetSoldStatusForReturn;
            this.useBeerTaps = config.saleControlConfig.useBeerTaps;

            const markCheckResultSave = config.saleControlConfig.markCheckResultSave ?? {};
            this.markCheckResultSaveEnable = markCheckResultSave.enable ?? false;
            this.markCheckResultSaveDirectory = markCheckResultSave.directory ?? "";
            this.markCheckResultSaveFileLifespanHours = markCheckResultSave.fileLifespanHours ?? 1;
        }

        return this;
    }

    render() {
        var elements = [];

        elements.push(
            Label("lSalesControlParameters", this.LABELS.title),
        );

        elements.push(
            {
                padding: padding,
                rows: [

                    CheckBox(this.LABELS.banSalesReturnedWares, "saleControlConfig.banSalesReturnedWares", {value: this.banSalesReturnedWares}),
                    Text(this.LABELS.ignoreVerificationErrorForTrueApiGroups, "saleControlConfig.ignoreVerificationErrorForTrueApiGroups", this.ignoreVerificationErrorForTrueApiGroups),
                    CheckBox(this.LABELS.checkIsOwnerField, "saleControlConfig.checkIsOwnerField", {value: this.checkIsOwnerField}),
                    CheckBox(this.LABELS.correctExpireDateInReturns, "saleControlConfig.correctExpireDateInSaleReturn", {value: this.correctExpireDateInSaleReturn}),
                    CheckBox(this.LABELS.sendLocalModuleInformationalInRequestId, "saleControlConfig.sendLocalModuleInformationalInRequestId", {value: this.sendLocalModuleInformationalInRequestId}),
                    CheckBox(this.LABELS.useBeerTaps, "saleControlConfig.useBeerTaps", {value: this.useBeerTaps}),

                    Label("scForFrontolMoreThen21", this.LABELS.forFrontolMoreThen205),

                    {
                        padding: { left: 20, },
                        rows: [
                            CheckBox(this.LABELS.checkReceiptReturn, "saleControlConfig.checkReceiptReturn", { value: this.checkReceiptReturn }),
                            CheckBox(this.LABELS.resetSoldStatusForReturn, "saleControlConfig.resetSoldStatusForReturn", { value: this.resetSoldStatusForReturn }),
                        ]
                    },

                    CheckBox(this.LABELS.markCheckResultSaveTitle, "saleControlConfig.markCheckResultSave.enable", {
                        value: this.markCheckResultSaveEnable,
                        on: {
                            onChange: (enabled) => {
                                if (enabled) {
                                    $$(this.MARK_CHECK_RESULT_SAVE_ID).enable();
                                } else {
                                    $$(this.MARK_CHECK_RESULT_SAVE_ID).disable();
                                }
                            }
                        }
                    }),

                    {
                        id: this.MARK_CHECK_RESULT_SAVE_ID,
                        disabled: !this.markCheckResultSaveEnable,
                        padding: padding,
                        rows: [
                            Text(
                                this.LABELS.markCheckResultSaveDirectory,
                                "saleControlConfig.markCheckResultSave.directory",
                                this.markCheckResultSaveDirectory,
                                windowsPathValidation
                            ),
                            Number(
                                this.LABELS.markCheckResultSaveFileLifespanHours,
                                "saleControlConfig.markCheckResultSave.fileLifespanHours",
                                this.markCheckResultSaveFileLifespanHours
                            ),
                        ],
                    }
                ]
            }
        );

        return { id: this.id, rows: elements };
    }
}

export default function (id, config) {
    return new SaleControllsConfigurationElement(id)
        .loadConfig(config)
        .render();
}