// Создаем файл для кастомных компонентов
webix.protoUI({
    name: "formtable",
    $allowsClear: true,
    setValue: function(value) {
        this.clearAll();
        if (value) this.parse(value);
    },
    getValue: function() {
        return this.serialize();
    }
}, webix.ui.datatable);