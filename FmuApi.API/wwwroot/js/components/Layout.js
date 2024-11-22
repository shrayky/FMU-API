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

export const createSidebar = (items, onSelect) => ({
    view: "sidebar",
    id: "sidebar",
    width: 200,
    collapsed: false,
    data: items,
    on: {
        onAfterSelect: onSelect
    }
});