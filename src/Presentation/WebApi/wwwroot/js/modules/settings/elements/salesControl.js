import { Label, TextBox, Text, padding, CheckBox } from "../../../utils/ui.js";

class SaleControllsConfigurationElement {
    constructor(id) {
        this.id = id;
        this.SETTINGS_ID = "saleControlls";
        this.LABELS = {
            title: "Контроль при продаже товаров",
            banSalesReturnedWares: "Блокировать продажу возвращенных товаров",
            ignoreVerificationErrorForTrueApiGroups: "Коды групп товаров игнорирующик проверку статусов кода маркировки в Честном Знаке",
            checkIsOwnerField: "Проверять владельца марки",
            forFrontolMoreThen205: "Настройки для тарифного фронтола (6.21.0 и выше):",
            checkReceiptReturn: "Проверять товары из чеков возврата",
            sendEmptyTrueApiAnswerWhenTimeoutError : "Генерировать пустой ответ от честного знака при недоступности cdn",
            correctExpireDateInReturns: "Исправлять истекший срок годности в чеках возврата",
        };
    }

    loadConfig(config) {
        if (config?.saleControlConfig) {
            console.log(config.saleControlConfig);
            this.banSalesReturnedWares = config.saleControlConfig.banSalesReturnedWares;
            this.ignoreVerificationErrorForTrueApiGroups = config.saleControlConfig.ignoreVerificationErrorForTrueApiGroups;
            this.checkIsOwnerField = config.saleControlConfig.checkIsOwnerField;
            this.checkReceiptReturn = config.saleControlConfig.checkReceiptReturn;
            this.sendEmptyTrueApiAnswerWhenTimeoutError = config.saleControlConfig.sendEmptyTrueApiAnswerWhenTimeoutError;
            this.сorectExpireDateInSaleReturn = config.saleControlConfig.corectExpireDateInSaleReturn;
        }

        return this;
    }

    render() {
        var elements = [];

        elements.push(
            Label("lSaleseControlParametrs", this.LABELS.title),
        );

        elements.push(
            {
                padding: padding,
                rows: [
                    CheckBox(this.LABELS.banSalesReturnedWares, "saleControlConfig.banSalesReturnedWares", {value: this.banSalesReturnedWares}),
                    Text(this.LABELS.ignoreVerificationErrorForTrueApiGroups, "saleControlConfig.ignoreVerificationErrorForTrueApiGroups", this.ignoreVerificationErrorForTrueApiGroups),
                    CheckBox(this.LABELS.checkIsOwnerField, "saleControlConfig.checkIsOwnerField", {value: this.checkIsOwnerField}),
                    CheckBox(this.LABELS.correctExpireDateInReturns, "saleControlConfig.corectExpireDateInSaleReturn", {value: this.сorectExpireDateInSaleReturn}),

                    Label("scForFrontolMoreThen21", this.LABELS.forFrontolMoreThen205),

                    {
                        padding: { left: 20, },
                        rows: [
                            CheckBox(this.LABELS.checkReceiptReturn, "saleControlConfig.checkReceiptReturn", {value: this.checkReceiptReturn}),
                            CheckBox(this.LABELS.sendEmptyTrueApiAnswerWhenTimeoutError, "saleControlConfig.sendEmptyTrueApiAnswerWhenTimeoutError", {value: this.sendEmptyTrueApiAnswerWhenTimeoutError}),
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