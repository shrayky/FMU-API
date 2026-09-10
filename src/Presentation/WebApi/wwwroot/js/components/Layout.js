export const createLayout = (config) => ({
    container: "app",
    type: "space",
    id: "root",
    responsive: true,
    ...config
});

export const createToolbar = (label, options = {}) => ({
    view: "toolbar",
    id: "mainToolbar",
    padding: options.hidden ? 0 : 5,
    height: options.hidden ? 0 : 60,
    hidden: !!options.hidden,
    elements: [
        {
            view: "label",
            id: "toolbarLabel",
            label
        }
    ]
});