import { OnDeleteAllRows, OnDeleteRow, OnAddRow } from "./tables.js";

export function TableToolabr(tableName) {
    return {
        cols: [
            {
                view: "button",
                value: "Добавить",
                id: `add_${tableName}`,
                tableId: tableName,
                autowidth: "false",
                width: 400,
                click: OnAddRow,
            },
            {
                view: "button",
                value: "Удалить",
                id: `delete_${tableName}`,
                tableId: tableName,
                autowidth: "false",
                width: 400,
                disabled: true,
                click: OnDeleteRow
            },
            {
                view: "button",
                value: "Удалить все",
                id: `deleteAll_${tableName}`,
                tableId: tableName,
                autowidth: "false",
                width: 400,
                disabled: false,
                click: OnDeleteAllRows
            },
            {}
        ]

    }
}

export function Label(id, label, styling = {}) {
    let newElement = {
        view: "label",
        id: id,
        label: label
    };

    for (let style in styling) {
        newElement[style] = styling[style];
    }

    return newElement;
}

export function LabelItalic(id, label) {
    return {
        view: "label",
        id: id,
        label: label,
        css: { "font-style": "italic", "font-size": 8 },
    }
}

export function TextBox(type, label, name) {
    let id = name[0].toUpperCase() + name.slice(1);

    return {
        view: "text",
        type: type,
        label: label,
        labelPosition: "top",
        id: id,
        name: name,
    }
}

export function PasswordBox(label, name) {
    let id = name[0].toUpperCase() + name.slice(1);

    return {
        view: "text",
        type: "password",
        label: label,
        labelPosition: "top",
        id: id,
        name: name,
    }
}

export function TextBoxFormated(type, label, name, format) {
    let id = name[0].toUpperCase() + name.slice(1);

    return {
        view: "text",
        type: type,
        format: format,
        label: label,
        labelPosition: "top",
        id: id,
        name: name,
    }
}

export function CheckBox(label, name) {
    let id = name[0].toUpperCase() + name.slice(1);

    return {
        view: "checkbox",
        label: label,
        labelPosition: "top",
        id: id,
        name: name,
    }
}