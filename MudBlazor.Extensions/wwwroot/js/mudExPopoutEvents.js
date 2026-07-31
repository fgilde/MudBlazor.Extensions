/*
 * Blazor routes every dom event through a single listener on the document of the page it started
 * in. A panel that MudExDockLayout moves into a popout window lives in another document, so nothing
 * reaches blazor any more: the panel looks alive but no button, no input and no click outside works.
 *
 * This records what gets attached to the main document and replays it into every popout window, so
 * blazor's handlers, the mud popovers and the outside click detection keep working over there.
 *
 * Load it BEFORE blazor starts — the registrations it misses are gone:
 *   <script src="_content/MudBlazor.Extensions/js/mudExPopoutEvents.js"></script>
 */
(function () {
    if (window.MudExPopoutEvents) return;

    const recorded = [];
    const popouts = new Set();
    const attachToMainDocument = document.addEventListener.bind(document);

    function attachTo(win, type, listener, options) {
        try { win.document.addEventListener(type, listener, options); } catch { /* window already gone */ }
    }

    // blazor registers a listener per event name lazily, the first time a component needs it, so
    // recording has to keep running and feed windows that are already open
    document.addEventListener = function (type, listener, options) {
        recorded.push({ type, listener, options });
        for (const win of popouts) attachTo(win, type, listener, options);
        return attachToMainDocument(type, listener, options);
    };

    window.MudExPopoutEvents = {
        /** Replays the listeners of the main document into a popout window. */
        attach: function (win) {
            if (!win || win === window || popouts.has(win)) return false;
            popouts.add(win);
            for (const entry of recorded) attachTo(win, entry.type, entry.listener, entry.options);
            win.addEventListener('pagehide', () => popouts.delete(win), { once: true });
            return true;
        },

        /** Main document first, then every popout — a panel's dom may have travelled. */
        documents: function () {
            const documents = [document];
            for (const win of popouts) {
                try { if (win.document) documents.push(win.document); } catch { /* window already gone */ }
            }
            return documents;
        },

        /** getElementById across all of them. */
        byId: function (id) {
            for (const doc of window.MudExPopoutEvents.documents()) {
                const element = doc.getElementById(id);
                if (element) return element;
            }
            return null;
        },

        /** querySelector across all of them. */
        query: function (selector) {
            for (const doc of window.MudExPopoutEvents.documents()) {
                const element = doc.querySelector(selector);
                if (element) return element;
            }
            return null;
        },

        /** querySelectorAll across all of them, flattened. */
        queryAll: function (selector) {
            const all = [];
            for (const doc of window.MudExPopoutEvents.documents()) all.push(...doc.querySelectorAll(selector));
            return all;
        }
    };
})();
