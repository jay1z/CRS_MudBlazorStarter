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
