export function ApiServerAddress(controllerName = "") {
    let element = document.getElementById("ApiServerIpPort");

    if (element == null)
        return "";

    let apiIpPort = element.value;

    return `http://${window.location.hostname}:${apiIpPort}/api${controllerName}`;
}

export function ServerAdres(controllerName = "") {
    let element = document.getElementById("ApiServerIpPort");

    if (element == null)
        return "";

    let apiIpPort = element.value;

    return `http://${window.location.hostname}:${apiIpPort}${controllerName}`;
}

export function SaveFormData(id, args) {
    let button = $$(id);

    let form = button.getFormView();

    if (form == undefined)
        return;

    if (!form.validate())
        return;

    form.disable();

    let updateDateTableId = form.config.dataTableId;
    let closeWindowAfterSave = form.config.closeWindowAfterSave;

    let controllerName = form.config.save.url;

    if (controllerName == undefined)
        controllerName = form.config.url.source;

    if (controllerName.indexOf("->") > 0)
    {
        let controllerNameSplited = controllerName.split("->");
        controllerName = controllerNameSplited[1];
    }

    let apiServerAddress = ApiServerAddress(controllerName);

    let fd = form.getValues();
    let data = JSON.stringify(form.getValues());

    webix.ajax()
        .post(apiServerAddress, data)
        .then(answer =>
        {
            let packet = answer.json();

            if (!packet.isSuccess) {
                webix.message(packet.message);
                return;
            }

            if (updateDateTableId != undefined) {
                let updateTable = $$(updateDateTableId);

                let element = updateTable.getItem(packet.content.id);

                if (element == undefined)
                    updateTable.add(packet.content);
                else
                    updateTable.updateItem(packet.content.id, packet.content);

                if (closeWindowAfterSave != undefined) {
                    if (closeWindowAfterSave) {
                        let window = form.getTopParentView();

                        if (window.config.view == "window")
                            $$(window.config.id).close()
                    }
                }
            }


        })
        .finally( _ =>
        { 
            form.enable();
        });
}