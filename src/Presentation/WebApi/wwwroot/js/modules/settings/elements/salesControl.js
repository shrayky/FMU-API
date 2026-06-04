import { Label, TextBox, Text, padding, CheckBox } from "../../../utils/ui.js";

class SaleControllsConfigurationElement {
    constructor(id) {
        this.id = id;
        this.SETTINGS_ID = "saleControlls";
        this.LABELS = {
            title: "Контроль при продаже товаров",
            banSalesReturnedWares: "Блокировать продажу возвращенных товаров",
            ignoreVerificationErrorForTrueApiGroups: "Коды групп товаров игнорирующик проверку статусов кода маркировки в Честном Знаке",
            checkIsOwnerField: "Проверять владельца марки средствами fmu-api",
            forFrontolMoreThen205: "Настройки для тарифного фронтола (6.21.0 и выше):",
            checkReceiptReturn: "Проверять товары из чеков возврата",
            sendEmptyTrueApiAnswerWhenTimeoutError : "Генерировать пустой ответ от честного знака при недоступности cdn",
            correctExpireDateInReturns: "Исправлять истекший срок годности в чеках возврата",
            sendLocalModuleInformationalInRequestId: "Отправлять информацию о локальном модуле для тега 1265 в requestId",
            resetSoldStatusForReturn: "Для возвратов - изменять стаус `Проданно` для товаров (тарифный фронтол до 25 версии)",
            useBeerTaps: "Использовать пивные краны"
        };
    }

    loadConfig(config) {
        if (config?.saleControlConfig) {
            this.banSalesReturnedWares = config.saleControlConfig.banSalesReturnedWares;
            this.ignoreVerificationErrorForTrueApiGroups = config.saleControlConfig.ignoreVerificationErrorForTrueApiGroups;
            this.checkIsOwnerField = config.saleControlConfig.checkIsOwnerField;
            this.checkReceiptReturn = config.saleControlConfig.checkReceiptReturn;
            this.sendEmptyTrueApiAnswerWhenTimeoutError = config.saleControlConfig.sendEmptyTrueApiAnswerWhenTimeoutError;
            this.correctExpireDateInSaleReturn = config.saleControlConfig.correctExpireDateInSaleReturn;
            this.sendLocalModuleInformationalInRequestId = config.saleControlConfig.sendLocalModuleInformationalInRequestId;
            this.resetSoldStatusForReturn = config.saleControlConfig.resetSoldStatusForReturn;
            this.useBeerTaps = config.saleControlConfig.useBeerTaps;
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
                            CheckBox(this.LABELS.sendEmptyTrueApiAnswerWhenTimeoutError, "saleControlConfig.sendEmptyTrueApiAnswerWhenTimeoutError", { value: this.sendEmptyTrueApiAnswerWhenTimeoutError }),
                            CheckBox(this.LABELS.resetSoldStatusForReturn, "saleControlConfig.resetSoldStatusForReturn", { value: this.resetSoldStatusForReturn }),
                        ]
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