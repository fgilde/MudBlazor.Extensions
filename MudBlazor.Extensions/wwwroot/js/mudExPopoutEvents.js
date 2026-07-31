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

    const boundDocuments = new WeakSet();

    function alive(win) {
        try { return !!win && !win.closed && !!win.document; } catch { return false; }
    }

    function attachTo(win, type, listener, options) {
        try { win.document.addEventListener(type, listener, options); } catch { /* window already gone */ }
    }

    /** Binds the recorded listeners to the window's current document, once per document. */
    function replay(win, onNewDocument) {
        if (!alive(win)) return false;
        const doc = win.document;
        if (boundDocuments.has(doc)) return true;
        boundDocuments.add(doc);
        for (const entry of recorded) attachTo(win, entry.type, entry.listener, entry.options);
        try { onNewDocument?.(win); } catch { /* caller's problem, not ours */ }
        return true;
    }

    /**
     * A window opens on about:blank and gets its real document a moment later, which throws away
     * everything registered on the first one. Neither its load event nor anything else registered
     * on that window survives the swap, so the only reliable way is to watch for the new document.
     */
    function watch(win, attemptsLeft, onNewDocument) {
        if (!alive(win)) { popouts.delete(win); return; }
        replay(win, onNewDocument);
        let settled = false;
        try { settled = win.document.readyState === 'complete' && win.location.href !== 'about:blank'; } catch { settled = true; }
        if (!settled && attemptsLeft > 0) setTimeout(() => watch(win, attemptsLeft - 1, onNewDocument), 100);
    }

    // blazor registers a listener per event name lazily, the first time a component needs it, so
    // recording has to keep running and feed windows that are already open
    document.addEventListener = function (type, listener, options) {
        recorded.push({ type, listener, options });
        for (const win of popouts) {
            if (alive(win)) attachTo(win, type, listener, options); else popouts.delete(win);
        }
        return attachToMainDocument(type, listener, options);
    };

    // A popout that outlives the page it belongs to is a trap: its dom still hangs in the old
    // blazor renderer, so it reacts to nothing and the reloaded page cannot adopt it either.
    window.addEventListener('pagehide', function () {
        for (const win of popouts) {
            try { win.close(); } catch { /* already gone */ }
        }
        popouts.clear();
    });

    window.MudExPopoutEvents = {
        /**
         * Replays the listeners of the main document into a popout window, and keeps doing so
         * until its real document is there. Safe to call again, per document it binds once.
         * <paramref name="onNewDocument"/> runs whenever a document of that window was bound —
         * the place for anything else the popout page needs, like its title.
         */
        attach: function (win, onNewDocument) {
            if (!win || win === window) return false;
            // callers may not know whether a window was seen before (a layout restore reopens
            // popouts on its own), so calling again must not start a second watcher
            if (popouts.has(win) && alive(win) && boundDocuments.has(win.document)) return true;
            popouts.add(win);
            watch(win, 100, onNewDocument); // ten seconds is more than a local page needs
            return true;
        },

        /** Main document first, then every popout — a panel's dom may have travelled. */
        documents: function () {
            const documents = [document];
            for (const win of popouts) {
                if (alive(win)) documents.push(win.document); else popouts.delete(win);
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
