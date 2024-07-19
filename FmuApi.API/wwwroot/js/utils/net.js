export function ApiServerAdres(controllerName = "") {
    let element = document.getElementById("ApiServerIpPort");

    if (element == null)
        return "";

    let apiIpPort = element.value;

    return `http://${window.location.hostname}:${apiIpPort}/api${controllerName}`;
}

export function SaveFormData(id, args) {
    let button = $$(id);

    let form = button.getFormView();

    if (form == undefined)
        return;

    if (!form.validate())
        return;

    form.disable();

    let upadteDateTableId = form.config.dataTableId;
    let closeWindowAfterSave = form.config.closeWindowAfterSave;

    let controllerName = form.config.save.url;

    if (controllerName == undefined)
        controllerName = form.config.url.source;

    if (controllerName.indexOf("->") > 0)
    {
        let controllerNameSplited = controllerName.split("->");
        controllerName = controllerNameSplited[1];
    }

    let apiServerAdres = ApiServerAdres(controllerName);

    let fd = form.getValues();
    let data = JSON.stringify(form.getValues());

    webix.ajax()
        .post(apiServerAdres, data)
        .then(answer =>
        {
            let packet = answer.json();

            if (!packet.isSuccess) {
                webix.message(packet.message);
                return;
            }

            if (upadteDateTableId != undefined) {
                let upadteTable = $$(upadteDateTableId);

                let element = upadteTable.getItem(packet.content.id);

                if (element == undefined)
                    upadteTable.add(packet.content);
                else
                    upadteTable.updateItem(packet.content.id, packet.content);

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