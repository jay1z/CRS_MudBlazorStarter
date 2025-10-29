window.ensureQuillLoaded = function () {
    return new Promise((resolve, reject) => {
        try {
            if (window.Quill) {
                // If Quill already present, ensure blotFormatter registered
                return ensureBlotFormatterRegistered().then(() => resolve(true)).catch(reject);
            }

            // If a loader is already in progress, poll until Quill appears
            if (document.querySelector('script[data-quill-loader]')) {
                const check = () => {
                    if (window.Quill) {
                        ensureBlotFormatterRegistered().then(() => resolve(true)).catch(reject);
                    } else setTimeout(check, 50);
                };
                check();
                return;
            }

            const tryLoad = (src) => {
                return new Promise((res, rej) => {
                    const s = document.createElement('script');
                    s.setAttribute('data-quill-loader', '1');
                    s.src = src;
                    s.onload = () => res(true);
                    s.onerror = () => rej(new Error('Failed to load script: ' + src));
                    document.head.appendChild(s);
                });
            };

            const hostname = window.location.hostname;
            const isLocal = hostname === 'localhost' || hostname === '127.0.0.1' || hostname === '';

            // Try local Quill v2 first
            tryLoad('/lib/quill/v2/quill.min.js')
                .then(() => ensureBlotFormatterRegistered())
                .then(() => resolve(true))
                .catch((localErr) => {
                    if (isLocal) {
                        // Fallback to CDN for dev convenience
                        tryLoad('https://cdn.jsdelivr.net/npm/quill@2/dist/quill.js')
                            .then(() => ensureBlotFormatterRegistered(true))
                            .then(() => resolve(true))
                            .catch((cdnErr) => reject(cdnErr));
                    } else {
                        // In production, require local hosting of Quill for security and reliability
                        reject(new Error('Quill v2 not found locally; production must include /lib/quill/v2/quill.min.js'));
                    }
                });
        } catch (ex) {
            reject(ex);
        }
    });
};

// Ensure quill-blot-formatter is loaded and registered with Quill.
// If allowCdnFallback is true it will attempt CDN fallback for the module when running locally.
function ensureBlotFormatterRegistered(allowCdnFallback = false) {
    return new Promise((resolve, reject) => {
        try {
            const registerIfAvailable = () => {
                try {
                    const Q = window.Quill;
                    // Support both UMD/global and ES module default exports
                    const BlotFormatterGlobal = window.QuillBlotFormatter;
                    const BlotFormatter = BlotFormatterGlobal && (BlotFormatterGlobal.default || BlotFormatterGlobal) || null;
                    if (Q && BlotFormatter) {
                        try {
                            // register the module under the expected key
                            Q.register('modules/blotFormatter', BlotFormatter);
                        } catch (e) {
                            // ignore registration errors
                        }
                    }
                    resolve(true);
                } catch (err) {
                    reject(err);
                }
            };

            // If Quill isn't present yet, wait briefly for it (should be loaded before calling this)
            if (typeof window.Quill === 'undefined') {
                const checkQ = () => {
                    if (window.Quill) {
                        loadBlotFormatter().then(registerIfAvailable).catch(reject);
                    } else {
                        setTimeout(checkQ, 50);
                    }
                };
                checkQ();
            } else {
                // Quill present, attempt to load blot formatter then register
                loadBlotFormatter(allowCdnFallback).then(registerIfAvailable).catch(reject);
            }
        } catch (ex) {
            reject(ex);
        }
    });
}

function loadBlotFormatter(allowCdnFallback = false) {
    return new Promise((resolve, reject) => {
        try {
            // If module already available, resolve
            if (window.QuillBlotFormatter || (window.QuillBlotFormatter && window.QuillBlotFormatter.default)) {
                resolve(true);
                return;
            }

            // If a loader is already in progress, poll until QuillBlotFormatter appears
            if (document.querySelector('script[data-quot-blot-loader]')) {
                const check = () => {
                    if (window.QuillBlotFormatter || (window.QuillBlotFormatter && window.QuillBlotFormatter.default)) resolve(true);
                    else setTimeout(check, 50);
                };
                check();
                return;
            }

            const tryLoad = (src) => {
                return new Promise((res, rej) => {
                    const s = document.createElement('script');
                    s.setAttribute('data-quot-blot-loader', '1');
                    s.src = src;
                    s.onload = () => res(true);
                    s.onerror = () => rej(new Error('Failed to load script: ' + src));
                    document.head.appendChild(s);
                });
            };

            const hostname = window.location.hostname;
            const isLocal = hostname === 'localhost' || hostname === '127.0.0.1' || hostname === '';

            // Try local blot-formatter first
            tryLoad('/lib/quill/v2/quill-blot-formatter.min.js')
                .then(() => resolve(true))
                .catch((localErr) => {
                    if (allowCdnFallback && isLocal) {
                        // allow CDN fallback in dev
                        tryLoad('https://unpkg.com/quill-blot-formatter/dist/quill-blot-formatter.min.js')
                            .then(() => resolve(true))
                            .catch((cdnErr) => reject(cdnErr));
                    } else {
                        // Not fatal — some editor configurations don't require blot-formatter.
                        // Resolve even if not loaded so editor can function without it.
                        resolve(true);
                    }
                });
        } catch (ex) {
            reject(ex);
        }
    });
}

// Quill v2 loader and interop for Blazor
// - prefers local files at /lib/quill/v2/
// - falls back to CDN for local development when allowed
// - loads CSS and JS, registers blotFormatter module if available
// - exposes window.quillInterop: ensureLoaded(), init(), getHtml(), setHtml(), getContents()

(function () {
    const QUILL_V2_LOCAL_JS = '/lib/quill/v2/quill.min.js';
    const QUILL_V2_CDN_JS = 'https://cdn.jsdelivr.net/npm/quill@2/dist/quill.min.js';
    const QUILL_V2_LOCAL_CSS = '/lib/quill/v2/quill.snow.css';
    const QUILL_V2_CDN_CSS = 'https://cdn.jsdelivr.net/npm/quill@2/dist/quill.snow.css';
    const BLOT_FORMATTER_LOCAL = '/lib/quill/v2/quill-blot-formatter.min.js';
    const BLOT_FORMATTER_CDN = 'https://unpkg.com/quill-blot-formatter/dist/quill-blot-formatter.min.js';

    let loadingPromise = null;

    function isLocalHost() {
        const hostname = window.location.hostname;
        return hostname === 'localhost' || hostname === '127.0.0.1' || hostname === '';
    }

    function ensureCss(url) {
        return new Promise((resolve) => {
            if (Array.from(document.head.querySelectorAll('link')).some(l => l.href && l.href.indexOf(url) !== -1)) {
                resolve();
                return;
            }
            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = url;
            link.onload = () => resolve();
            link.onerror = () => resolve();
            document.head.appendChild(link);
        });
    }

    function ensureScript(url, dataAttr) {
        return new Promise((resolve, reject) => {
            // already loaded?
            if (Array.from(document.scripts).some(s => s.src && s.src.indexOf(url) !== -1)) {
                resolve();
                return;
            }
            const s = document.createElement('script');
            if (dataAttr) s.setAttribute(dataAttr, '1');
            s.src = url;
            s.async = false;
            s.onload = () => resolve();
            s.onerror = () => reject(new Error('Failed to load script: ' + url));
            document.head.appendChild(s);
        });
    }

    async function tryLoadPreferLocal(localUrl, cdnUrl, localAttr, cdnAllowed) {
        // attempt local file first
        try {
            await ensureScript(localUrl, localAttr);
            return true;
        } catch (localErr) {
            if (cdnAllowed) {
                try {
                    await ensureScript(cdnUrl, 'data-quill-cdn');
                    return true;
                } catch (cdnErr) {
                    throw cdnErr;
                }
            }
            throw localErr;
        }
    }

    function loadBlotFormatter(allowCdnFallback) {
        return new Promise(async (resolve, reject) => {
            try {
                // If already present, resolve
                if (window.QuillBlotFormatter || (window.QuillBlotFormatter && window.QuillBlotFormatter.default)) {
                    resolve(true);
                    return;
                }

                // Try local then CDN (only allow CDN fallback for dev)
                const cdnAllowed = allowCdnFallback && isLocalHost();
                try {
                    await tryLoadPreferLocal(BLOT_FORMATTER_LOCAL, BLOT_FORMATTER_CDN, 'data-quill-blot-loader', cdnAllowed);
                    resolve(true);
                } catch (err) {
                    // Not fatal: some setups don't use blot-formatter
                    console.warn('quill-loader: blot-formatter not loaded', err);
                    resolve(false);
                }
            } catch (ex) {
                reject(ex);
            }
        });
    }

    function loadDependencies() {
        if (loadingPromise) return loadingPromise;

        loadingPromise = (async function () {
            try {
                // Load CSS (prefer local, fallback to CDN in dev)
                const cssLocal = QUILL_V2_LOCAL_CSS;
                const cssCdn = QUILL_V2_CDN_CSS;
                try {
                    await ensureCss(cssLocal);
                } catch {
                    if (isLocalHost()) await ensureCss(cssCdn);
                    else await ensureCss(cssLocal); // attempt local even in prod; failure will be silent
                }

                // Load Quill v2 JS (prefer local; allow CDN only in dev)
                const allowCdn = isLocalHost();
                await tryLoadPreferLocal(QUILL_V2_LOCAL_JS, QUILL_V2_CDN_JS, 'data-quill-loader', allowCdn);

                // Load blot formatter (allow CDN only in dev)
                await loadBlotFormatter(allowCdn);

                // Attempt to register blotFormatter with Quill if available
                try {
                    const Q = window.Quill;
                    const BlotFormatterGlobal = window.QuillBlotFormatter;
                    const BlotFormatter = BlotFormatterGlobal && (BlotFormatterGlobal.default || BlotFormatterGlobal) || null;
                    if (Q && BlotFormatter) {
                        try {
                            Q.register('modules/blotFormatter', BlotFormatter);
                        } catch (regErr) {
                            console.warn('quill-loader: error registering blotFormatter', regErr);
                        }
                    }
                } catch (e) {
                    console.warn('quill-loader: exception while registering blotFormatter', e);
                }

                return { quill: window.Quill, blotFormatterRegistered: !!(window.Quill && window.Quill.imports && window.Quill.imports['modules/blotFormatter']) };
            } catch (e) {
                console.error('quill-loader: failed to load dependencies', e);
                throw e;
            }
        })();

        return loadingPromise;
    }

    // Expose interop
    window.quillInterop = window.quillInterop || {};

    window.quillInterop.ensureLoaded = function () {
        return loadDependencies();
    };

    // Initialize a Quill editor on the given element id. options may be a plain object compatible with Quill v2 config.
    window.quillInterop.init = async function (elementId, initialHtmlOrOptions, dotNetRef, optionsArg) {
        try {
            await loadDependencies();

            const el = document.getElementById(elementId);
            if (!el) {
                console.error('quillInterop.init: element with id ' + elementId + ' not found');
                return null;
            }

            // Support call signatures: (elementId, initialHtml) or (elementId, options)
            let cfg = { theme: 'snow' };
            let initialHtml = '';
            if (typeof initialHtmlOrOptions === 'string') {
                initialHtml = initialHtmlOrOptions;
                cfg = Object.assign(cfg, optionsArg || {});
            } else if (initialHtmlOrOptions && typeof initialHtmlOrOptions === 'object') {
                cfg = Object.assign(cfg, initialHtmlOrOptions);
            }

            // Ensure toolbar exists
            if (!cfg.modules) cfg.modules = {};
            if (!cfg.modules.toolbar) {
                cfg.modules.toolbar = [['bold', 'italic', 'underline', 'strike'], ['blockquote', 'code-block'], [{ 'header': 1 }, { 'header': 2 }], [{ 'list': 'ordered' }, { 'list': 'bullet' }], ['link', 'image'], ['clean']];
            }

            // Ensure blotFormatter module present in modules config if registered
            try {
                // Prefer checking the registered modules map to avoid enabling module when not registered
                if (window.Quill && window.Quill.imports && window.Quill.imports['modules/blotFormatter']) {
                    cfg.modules.blotFormatter = cfg.modules.blotFormatter || {};
                }
            } catch { }

            // Create the editor instance
            const editor = new window.Quill(el, cfg);

            // Set initial HTML if provided
            if (initialHtml && editor.root) {
                try { editor.root.innerHTML = initialHtml; } catch (e) { }
            }

            // Wire up change events to call DotNet callback if provided
            if (dotNetRef) {
                editor.on('text-change', function () {
                    try {
                        const html = editor.root ? editor.root.innerHTML : '';
                        dotNetRef.invokeMethodAsync('NotifyQuillChange', elementId, html).catch(() => { });
                    } catch (e) { }
                });
            }

            // Store instance
            const instanceId = elementId + '_' + Math.random().toString(36).substr(2, 9);
            window.quillInterop.instances = window.quillInterop.instances || {};
            window.quillInterop.instances[instanceId] = editor;

            return { instanceId: instanceId };
        }
        catch (e) {
            console.error('quillInterop.init error', e);
            return null;
        }
    };

    window.quillInterop.getContents = function (instanceHandle) {
        try {
            if (!window.quillInterop.instances || !window.quillInterop.instances[instanceHandle]) return null;
            return window.quillInterop.instances[instanceHandle].getContents();
        } catch (e) {
            console.error('quillInterop.getContents', e);
            return null;
        }
    };

    window.quillInterop.getHtml = function (instanceHandle) {
        try {
            if (!window.quillInterop.instances || !window.quillInterop.instances[instanceHandle]) return null;
            const editor = window.quillInterop.instances[instanceHandle];
            const container = editor.root;
            return container ? container.innerHTML : null;
        } catch (e) {
            console.error('quillInterop.getHtml', e);
            return null;
        }
    };

    window.quillInterop.setHtml = function (instanceHandle, html) {
        try {
            if (!window.quillInterop.instances || !window.quillInterop.instances[instanceHandle]) return false;
            const editor = window.quillInterop.instances[instanceHandle];
            if (editor && editor.root) editor.root.innerHTML = html || '';
            return true;
        } catch (e) {
            console.error('quillInterop.setHtml', e);
            return false;
        }
    };

})();
