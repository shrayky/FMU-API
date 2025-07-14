export function saveConfiguration(formId) {
    const form = $$(formId);

    let data = JSON.stringify(form.getValues());

    form.disable();

    webix.ajax()
        .post("api/configuration/parameters", data)
        .then(answer => {
            let packet = answer.json();

            if (!packet.isSuccess) {
                webix.message(packet.message);
                return;
            }
        })
        .finally(_ => {
            form.enable();
        });
}
