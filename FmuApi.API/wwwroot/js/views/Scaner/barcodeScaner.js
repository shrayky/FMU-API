import { TableToolabr, Label, TextBox, PasswordBox, TextBoxFormated, CheckBox } from "../../utils/ui.js";

export function barcodeScaner(id) {
    $$("toolbarLabel").setValue("FMU-API: Сканер");

    return {
        view: "form",
        id: id,
        name: "fmuScanerForm",
        elements:
        [
        ]
    }

}