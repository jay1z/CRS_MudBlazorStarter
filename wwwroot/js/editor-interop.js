window.getMudHtmlEditorContent = function (editorId) {
    try {
        const el = document.getElementById(editorId);
        if (!el) return null;
        // The MudHtmlEditor renders Quill editor inside; find the .ql-editor within the element
        const ql = el.querySelector('.ql-editor');
        return ql ? ql.innerHTML : null;
    } catch (e) {
        console.error('getMudHtmlEditorContent error', e);
        return null;
    }
};