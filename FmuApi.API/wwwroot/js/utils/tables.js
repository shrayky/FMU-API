export function TableOfElement(elementId) {
    let element = $$(elementId);

    if (element == undefined)
        return;

    let tableId = element.data.tableId;

    if (tableId == undefined)
        return;

    return $$(tableId);
}

export function OnDeleteAllRows(elementId) {
    let table = TableOfElement(elementId);

    if (table == undefined)
        return;

    webix.confirm({
        title: "Вы уверены?",
        text: "Вы собираетесь полностью очистить таблицу?",
        ok: "Да",
        cancel: "Нет",
    }).then(function () {
        table.clearAll();
        $$(elementId).disable();
    })
}

export function OnDeleteRow(elementId, args) {
    let table = TableOfElement(elementId);

    if (table == undefined)
        return;

    let rowData = table.getSelectedItem();

    if (rowData == undefined) {
        webix.message("Не выбрана строка для удаления!");
        return;
    }

    table.remove(rowData.id);
}

export function OnAddRow(elementId, args) {
    let table = TableOfElement(elementId);

    if (table == undefined)
        return;

    table.add({});
}

