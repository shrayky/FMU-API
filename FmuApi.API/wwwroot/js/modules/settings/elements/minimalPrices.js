import { Label, Number, padding } from "../../../utils/ui.js";

class MinimalPricesConfigurationElement {
    constructor(id) {
        this.id = id;
        this.SETTINGS_ID = "minimalPrices";
        this.LABELS = {
            title: "Минимальные цены",
            tabaco: "Табак (в копейках 135р = 13500)",
        };
    }

    loadConfig(config) {
        if (config?.saleControlConfig) {
            this.tabaco = config.minimalPrices.tabaco;
        }

        return this;
    }

    render() {
        var elements = [];

        elements.push(
            Label("lMinimalPrices", this.LABELS.title),
        );

        elements.push(
            {
                padding: padding,
                rows: [
                    Number(this.LABELS.tabaco, "minimalPrices.tabaco", this.tabaco, "1111")
                ]
            }
        );

        return { id: this.id, rows: elements };
    }
}

export default function (id, config) {
    return new MinimalPricesConfigurationElement(id)
        .loadConfig(config)
        .render();
}