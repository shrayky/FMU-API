const BEER_TAPS_URL = "/api/beer-taps";
const DISCONNECT_URL = "/api/fmu/beer/connect_keg";

export async function loadBeerTaps({ signal } = {}) {
    const options = signal ? { signal } : null;
    const response = await webix.ajax().get(BEER_TAPS_URL, options);
    return response.json();
}

export function disconnectBeerTap(markCode) {
    const data = JSON.stringify({
        type: "disconnect_tap",
        emptied_marking_code: markCode,
    });

    return webix.ajax()
        .headers({ "Content-Type": "application/json" })
        .post(DISCONNECT_URL, data)
        .then(answer => answer.json());
}