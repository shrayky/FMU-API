export const createLayout = (config) => ({
    container: "app",
    type: "space",
    id: "root",
    responsive: true,
    ...config
});

export const createToolbar = (label) => ({
    view: "toolbar",
    padding: 5,
    height: 60,
    elements: [
        {
            view: "label",
            id: "toolbarLabel",
            label
        }
    ]
});