export function saveConfiguration(formId) {
    const form = $$(formId);

    let data = JSON.stringify(form.getValues());

    form.disable();

    webix.ajax()
        .post("api/configuration/parameters", data)
        .then(answer => {
            let packet = answer.json();

            console.log(packet);

            if (!packet.isSuccess) {
                webix.message(packet);
                return;
            }

            var needToRestart = packet.needToRestart;

            if (needToRestart) {
                webix.message("Изменения будут применены после перезапуска службы DS:Fmu-Api (она произойдет в течение одной минуты)");
            }
        })
        .finally(_ => {
            form.enable();
        });
}
