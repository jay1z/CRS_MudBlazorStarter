window.crsChat = {
    scrollToBottom: function () {
        try {
            const containers = document.querySelectorAll('.mud-chat, .mud-paper');
            if (!containers || containers.length === 0) return;
            const el = containers[containers.length - 1];
            el.scrollTop = el.scrollHeight;
        } catch { }
    }
};