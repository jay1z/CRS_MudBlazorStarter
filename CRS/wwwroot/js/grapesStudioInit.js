window.addEventListener('DOMContentLoaded', function () {
    if (window.GrapesJsStudioSDK && document.getElementById('studio-editor')) {
        GrapesJsStudioSDK.createStudioEditor({
            root: '#studio-editor',
            licenseKey: 'a3ddf8cbeacf4bfd98a4e6ae11ecbc85ad2ed8a1ba4e43f1bd9acd57909654d2',
            project: { type: 'web' },
            assets: {
                storageType: 'self',
                onUpload: async ({ files }) => { return []; },
                onDelete: async ({ assets }) => { }
            },
            storage: {
                type: 'self',
                onSave: async ({ project }) => { },
                onLoad: async () => { return { project: { type: 'web' } }; },
                autosaveChanges: 100,
                autosaveIntervalMs: 10000
            },
            plugins: [
                StudioSdkPlugins_tableComponent.init({}),
                StudioSdkPlugins_listPagesComponent.init({}),
                StudioSdkPlugins_iconifyComponent.init({}),
                StudioSdkPlugins_accordionComponent.init({}),
                StudioSdkPlugins_flexComponent.init({}),
                StudioSdkPlugins_prosemirror.init({}),
                StudioSdkPlugins_canvasFullSize.init({}),
                StudioSdkPlugins_layoutSidebarButtons.init({})
            ]
        });
    }
});
