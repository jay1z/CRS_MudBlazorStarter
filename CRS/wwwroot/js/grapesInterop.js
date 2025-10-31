window.grapesInterop = {
    editor: null,
    init: function (selector) {
        if (this.editor) return;
        // load grapes via CDN if not already present
        var self = this;
        function create() { self._createEditor(selector); }

        if (typeof grapesjs === 'undefined') {
            var script = document.createElement('script');
            script.src = 'https://unpkg.com/grapesjs/dist/grapes.min.js';
            script.onload = () => {
                // after grapesjs loads, also load the preset plugin to get the full UI (blocks, panels, style manager)
                var p = document.createElement('script');
                p.src = 'https://unpkg.com/grapesjs-preset-webpage/dist/grapesjs-preset-webpage.min.js';
                p.onload = create;
                document.head.appendChild(p);
            };
            document.head.appendChild(script);
            // also load basic CSS
            var link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = 'https://unpkg.com/grapesjs/dist/css/grapes.min.css';
            document.head.appendChild(link);
        } else {
            // ensure preset is loaded; if not, load it then create
            if (typeof grapesjs.plugins === 'undefined' || typeof grapesjs.plugins.add === 'undefined') {
                create();
            } else {
                // attempt to load preset if not present
                var presetLoaded = false;
                try { presetLoaded = !!grapesjs.plugins.get('gjs-preset-webpage'); } catch (e) { presetLoaded = false; }
                if (!presetLoaded) {
                    var p = document.createElement('script');
                    p.src = 'https://unpkg.com/grapesjs-preset-webpage/dist/grapesjs-preset-webpage.min.js';
                    p.onload = create;
                    document.head.appendChild(p);
                } else create();
            }
        }
    },
    _createEditor: function (selector) {
        if (this.editor) return;
        var opts = {
            container: '#' + selector,
            fromElement: false,
            height: '100%',
            width: 'auto',
            storageManager: false,
            // if the preset is available, use it to provide a full UI (blocks, panels, style manager)
            plugins: (window.grapesjs && window.grapesjs.plugins) ? ['gjs-preset-webpage'] : [],
            pluginsOpts: {}
        };
        try {
            this.editor = grapesjs.init(opts);
        } catch (e) {
            // fallback: init basic editor without preset
            this.editor = grapesjs.init({ container: '#' + selector, fromElement: false, height: '100%', storageManager: false });
        }

        // add a simple block if the block manager is available
        try {
            if (this.editor.BlockManager) {
                this.editor.BlockManager.add('section', {
                    label: 'Section',
                    content: '<section><h2>Section</h2><p>Sample section</p></section>'
                });
            }
        } catch (e) { /* ignore */ }
    },
    isReady: function() {
        return !!this.editor;
    },
    showBlocks: function() {
        if (!this.editor) return;
        try {
            // run the command to open blocks
            if (this.editor.Commands) this.editor.Commands.run('open-blocks');
            // ensure panel button active
            var btn = this.editor.Panels && this.editor.Panels.getButton('views', 'open-blocks');
            if (btn) btn.set('active',1);
        } catch (e) { console.error(e); }
    },
    showStyleManager: function() {
        if (!this.editor) return;
        try {
            if (this.editor.Commands) this.editor.Commands.run('open-sm');
            var btn = this.editor.Panels && this.editor.Panels.getButton('views', 'open-sm');
            if (btn) btn.set('active',1);
        } catch (e) { console.error(e); }
    },
    loadHtml: function (html) {
        if (!this.editor) return;
        this.editor.setComponents(html);
    },
    getHtml: function () {
        if (!this.editor) return '';
        return this.editor.getHtml();
    },
    getCss: function () {
        if (!this.editor) return '';
        return this.editor.getCss ? this.editor.getCss() : '';
    },
    getComponents: function () {
        if (!this.editor) return '';
        try {
            var comps = this.editor.getComponents();
            if (comps && typeof comps.toJSON === 'function') return JSON.stringify(comps.toJSON());
            return JSON.stringify(comps);
        } catch (e) {
            try {
                return JSON.stringify(this.editor.getProjectData ? this.editor.getProjectData() : {});
            } catch (e2) {
                return '';
            }
        }
    },
    destroy: function () {
        if (!this.editor) return;
        this.editor.destroy();
        this.editor = null;
    }
};

// Dynamically load a CSS file
window.loadCss = function (href) {
    return new Promise(function (resolve, reject) {
        if (document.querySelector('link[href="' + href + '"]')) return resolve();
        var link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = href;
        link.onload = resolve;
        link.onerror = reject;
        document.head.appendChild(link);
    });
};

// Dynamically load a JS file
window.loadScript = function (src) {
    return new Promise(function (resolve, reject) {
        if (document.querySelector('script[src="' + src + '"]')) return resolve();
        var script = document.createElement('script');
        script.src = src;
        script.async = false;
        script.onload = resolve;
        script.onerror = reject;
        document.body.appendChild(script);
    });
};

// Load GrapesJS Studio SDK and plugins in order
window.loadGrapesStudioDeps = async function () {
    const css = 'https://unpkg.com/@grapesjs/studio-sdk/dist/style.css';
    const scripts = [
        'https://unpkg.com/@grapesjs/studio-sdk/dist/index.umd.js',
        'https://cdn.jsdelivr.net/npm/@grapesjs/studio-sdk-plugins@latest/dist/tableComponent/index.umd.js',
        'https://cdn.jsdelivr.net/npm/@grapesjs/studio-sdk-plugins@latest/dist/listPagesComponent/index.umd.js',
        'https://cdn.jsdelivr.net/npm/@grapesjs/studio-sdk-plugins@latest/dist/iconifyComponent/index.umd.js',
        'https://cdn.jsdelivr.net/npm/@grapesjs/studio-sdk-plugins@latest/dist/accordionComponent/index.umd.js',
        'https://cdn.jsdelivr.net/npm/@grapesjs/studio-sdk-plugins@latest/dist/flexComponent/index.umd.js',
        'https://cdn.jsdelivr.net/npm/@grapesjs/studio-sdk-plugins@latest/dist/prosemirror/index.umd.js',
        'https://cdn.jsdelivr.net/npm/@grapesjs/studio-sdk-plugins@latest/dist/canvasFullSize/index.umd.js',
        'https://cdn.jsdelivr.net/npm/@grapesjs/studio-sdk-plugins@latest/dist/layoutSidebarButtons/index.umd.js'
    ];
    await window.loadCss(css);
    for (const src of scripts) {
        await window.loadScript(src);
    }
};

// Initialize Grapes Studio editor and keep instance for disposal
window.initGrapesStudio = async function (rootSelector, dotNetRef) {
    try {
        if (!window.GrapesJsStudioSDK) {
            throw new Error('GrapesJsStudioSDK is not available');
        }
        if (window._grapesStudioEditor) {
            // already initialized
            return { ok: true, message: 'already initialized' };
        }

        const editor = await GrapesJsStudioSDK.createStudioEditor({
            root: rootSelector,
            licenseKey: 'a3ddf8cbeacf4bfd98a4e6ae11ecbc85ad2ed8a1ba4e43f1bd9acd57909654d2',
            project: { type: 'web' },
            assets: {
                storageType: 'self',
                onUpload: async ({ files }) => {
                    try {
                        if (dotNetRef) await dotNetRef.invokeMethodAsync('NotifySaveStatus', 'saving', 'Uploading assets');
                    } catch (e) { }
                    try {
                        const form = new FormData();
                        for (const f of files) form.append('files', f);
                        const resp = await fetch('/api/studio/projects/assets/upload', { method: 'POST', body: form, credentials: 'same-origin' });
                        if (!resp.ok) throw new Error('Upload failed');
                        const json = await resp.json();
                        try { if (dotNetRef) await dotNetRef.invokeMethodAsync('NotifySaveStatus', 'saved', 'Assets uploaded'); } catch (e) { }
                        return json;
                    } catch (e) {
                        console.error('assets.onUpload error', e);
                        try { if (dotNetRef) await dotNetRef.invokeMethodAsync('NotifySaveStatus', 'error', 'Asset upload failed'); } catch (err) { }
                        return [];
                    }
                },
                onDelete: async ({ assets }) => {
                    try {
                        await fetch('/api/studio/projects/assets', { method: 'DELETE', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(assets), credentials: 'same-origin' });
                    } catch (e) { console.error('assets.onDelete error', e); }
                }
            },
            storage: {
                type: 'self',
                onLoad: async () => {
                    // default: return blank project
                    return { project: { type: 'web', pages: [{ id: 'home', name: 'Home', components: '' }], activePageId: 'home' } };
                },
                onSave: async ({ project }) => {
                    try {
                        if (dotNetRef) await dotNetRef.invokeMethodAsync('NotifySaveStatus', 'saving', 'Saving project');
                    } catch (e) { }
                    try {
                        let html = '';
                        try {
                            const active = project.activePageId || (project.pages && project.pages[0] && project.pages[0].id);
                            if (project.pages && Array.isArray(project.pages)) {
                                const page = project.pages.find(p => p.id === active) || project.pages[0];
                                if (page) html = page.components || '';
                            }
                        } catch { }
                        const payload = { Name: project.name || 'Studio Project', Project: project, Html: html };
                        const resp = await fetch('/api/studio/projects/save', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload), credentials: 'same-origin' });
                        if (!resp.ok) { const txt = await resp.text(); throw new Error('Save failed: ' + txt); }
                        const result = await resp.json();
                        try { if (dotNetRef) await dotNetRef.invokeMethodAsync('NotifySaveStatus', 'saved', 'Saved successfully'); } catch (e) { }
                        return result;
                    } catch (e) {
                        console.error('storage.onSave error', e);
                        try { if (dotNetRef) await dotNetRef.invokeMethodAsync('NotifySaveStatus', 'error', e?.message || 'Save error'); } catch (err) { }
                        throw e;
                    }
                },
                autosaveChanges:100,
                autosaveIntervalMs:10000
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

        window._grapesStudioEditor = editor;
        return { ok: true };
    } catch (err) {
        console.error('initGrapesStudio error', err);
        throw err;
    }
};

// Dispose / destroy the editor instance
window.disposeGrapesStudio = function () {
    try {
        if (window._grapesStudioEditor && typeof window._grapesStudioEditor.destroy === 'function') {
            window._grapesStudioEditor.destroy();
        }
    } catch (e) { console.warn('disposeGrapesStudio failed', e); }
    finally { window._grapesStudioEditor = null; }
};
