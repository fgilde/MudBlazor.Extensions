class MudExDockLayout {
    elementRef; dotnet; options; containerRef; api;
    elementStore = new Map();
    bootstrapped = false;
    observer = null;
    disposables = [];
    _isReinitializing = false;
    instanceId = crypto?.randomUUID ? crypto.randomUUID() : String(Date.now()) + Math.random(); // stabile ID

    constructor(elementRef, containerRef, dotnet, options) {
        this.elementRef = elementRef;
        this.dotnet = dotnet;
        this.containerRef = containerRef;
        this.options = options || {};
        this.init();
    }

    async init() {
        const { createDockview } = await import(this.options.module);

        this.api = createDockview(this.containerRef, {
            theme: { className: this.options.className ?? 'dockview-theme-abyss' },
            dndEdges: { top: true, right: true, bottom: true, left: true },
            createComponent: (opts) => this._createComponent(opts)
        });

        // 1) DOM-Leaves indexieren (für Restore/InitialLayout)
        this.indexDomLeaves();

        // 2) Initiales Layout anwenden, falls vorhanden
        if (this.options.initialLayoutJson) {
            try {
                this.api.fromJSON(JSON.parse(this.options.initialLayoutJson));
                this.bootstrapped = true;
                this.dotnet?.invokeMethodAsync('OnJsReady');
                return;
            } catch (e) { console.warn('fromJSON failed', e); }
        }

        this.dotnet?.invokeMethodAsync('OnJsReady');
        this.observeAndBootstrap();
        this.wireEvents();
    }

    // ---------------- Stash / Duplicate-Handling ----------------

    _ensureStash() {
        let stash = this.containerRef.querySelector(':scope > .dv-stash');
        if (!stash) {
            stash = document.createElement('div');
            stash.className = 'dv-stash';
            stash.style.display = 'none';
            this.containerRef.appendChild(stash);
        }
        return stash;
    }

    _hideAndStash(node, id) {
        if (!node) return;
        try {
            node.dataset.dvId = id;
            node.style.display = 'none';
            this._ensureStash().appendChild(node);
        } catch { /* noop */ }
    }

    _hideAndStashDuplicates(id, exceptEl = null) {
        const selector = `[data-dv-id="${CSS.escape(id)}"], .dv-node[data-options*='"id":"${CSS.escape(id)}"']`;
        const nodes = this.containerRef.querySelectorAll(selector);
        for (const n of nodes) {
            if (exceptEl && n === exceptEl) continue;
            this._hideAndStash(n, id);
        }
    }

    // ---------------- Component-Erzeugung ----------------

    _createComponent(opts) {
        // 1) bevorzugt: bereits gestashte/indizierte Instanz
        let el = this.elementStore.get(opts.id);

        // 2) Stash prüfen
        if (!el) {
            const stashHit = this.containerRef.querySelector(`:scope > .dv-stash [data-dv-id="${CSS.escape(opts.id)}"]`);
            if (stashHit) {
                el = stashHit;
                this.elementStore.set(opts.id, el);
            }
        }

        // 3) DOM (neue Leaf-Instanz) prüfen
        if (!el) {
            const domHit = this.containerRef.querySelector(`.dv-node [data-dv-id="${CSS.escape(opts.id)}"], .dv-node[data-options*='"id":"${CSS.escape(opts.id)}"']`);
            if (domHit) {
                el = domHit;
                this.elementStore.set(opts.id, el);
            }
        }

        if (el) {
            el.dataset.dvId = opts.id;
            el.style.display = '';
            el.style.height = '100%';
            el.style.width = '100%';
            el.style.overflow = 'auto';

            // andere Kopien derselben id verstecken & stashen
            this._hideAndStashDuplicates(opts.id, el);

            return {
                element: el,
                init() { },
                update() { },
                dispose: () => this._hideAndStash(el, opts.id)
            };
        }

        // Fallback – wenn kein DOM-Content vorhanden ist
        const div = document.createElement('div');
        div.textContent = 'blazor';
        return { element: div, init() { }, update() { }, dispose() { } };
    }

    // ---------------- Events ----------------

    wireEvents() {
        this.disposeEvents();
        const push = (d) => this.disposables.push(d).length && d;
        try { push(this.api.onDidAddPanel?.(p => this.dotnet?.invokeMethodAsync('OnJsPanelAdded', p.id))); } catch (e) { }
        try { push(this.api.onDidRemovePanel?.(p => this.dotnet?.invokeMethodAsync('OnJsPanelRemoved', p.id))); } catch (e) { }
        try { push(this.api.onDidActivePanelChange?.(e => this.dotnet?.invokeMethodAsync('OnJsActiveChanged', e?.id ?? null))); } catch (e) { }
        try {
            push(this.api.onDidMovePanel?.(e => {
                const payload = { PanelId: e.panel?.id ?? null, FromGroupId: e.from?.id ?? e.fromGroup?.id ?? null, ToGroupId: e.to?.id ?? e.toGroup?.id ?? null, ToIndex: e.index ?? 0 };
                this.dotnet?.invokeMethodAsync('OnJsPanelMoved', payload);
            }));
        } catch (e) { }
    }

    disposeEvents() {
        for (const d of this.disposables || []) {
            try { d?.dispose?.(); } catch (e) { }
        }
        this.disposables = [];
    }

    // ---------------- DOM-Index & Bootstrap ----------------

    indexDomLeaves() {
        // NICHT clearen – vorhandene (gestashte/aktive) Referenzen behalten
        const nodes = this.containerRef.querySelectorAll('.dv-node');
        for (const node of nodes) {
            const hasChildNodes = Array.from(node.children).some(n => n.classList?.contains('dv-node'));
            if (hasChildNodes) continue;

            const opts = JSON.parse(node.dataset.options || '{}');
            const id = opts.id;
            if (!id) continue;

            if (this.elementStore.has(id)) {
                // Duplikat -> sofort verstecken & stashen
                this._hideAndStash(node, id);
                continue;
            }

            node.dataset.dvId = id;
            node.style.display = 'none';
            this.elementStore.set(id, node);

            // Falls doch mehrere Instanzen existieren, alle übrigen einsammeln
            this._hideAndStashDuplicates(id, node);
        }
    }

    observeAndBootstrap() {
        const tryBootstrap = () => {
            if (this.bootstrapped) return;
            const roots = this._rootNodes();
            if (roots.length === 0) return;
            this.bootstrapFromDom(roots);
            this.bootstrapped = true;
            this.observer?.disconnect();
        };

        tryBootstrap();
        this.observer = new MutationObserver(() => tryBootstrap());
        this.observer.observe(this.containerRef, { childList: true, subtree: true });
    }

    setOptions(options) {
        this.options = options;
    }

    _rootNodes() {
        return Array.from(this.containerRef.children).filter(n => n.classList?.contains('dv-node'));
    }

    bootstrapFromDom(rootNodes) {
        const plan = [];

        for (const root of rootNodes) {
            const rootOpts = JSON.parse(root.dataset.options || '{}');
            const rootDir = rootOpts.direction?.toLowerCase() || 'right';
            let localAnchor = null;
            this._planFromNode(root, rootDir, plan, () => localAnchor, v => { localAnchor = v; });
        }

        for (const p of plan) {
            const panel = this.api.addPanel(p);
            panel.isVisible = p.isVisible;
            if (p.hideHeader === true && !this.containerRef.classList.contains('dv-hide-tabs')) {
                panel.group.header.hidden = true;
            }
            if (p.float === true) {
                this.api.addFloatingGroup(panel, p.floatBounds || undefined);
            }
            if (p.locked != null) {
                panel.group.locked = p.locked;
            }
        }
    }

    _planFromNode(node, parentDir, plan, getAnchor, setAnchor) {
        const children = Array.from(node.children).filter(n => n.classList?.contains('dv-node'));
        const isContainer = children.length > 0;
        const nodeOptions = JSON.parse(node.dataset.options || '{}');

        if (!isContainer) {
            const anchor = getAnchor();
            plan.push(nodeOptions);
            nodeOptions.position = anchor ? { referencePanel: anchor, direction: parentDir } : { direction: parentDir };
            setAnchor(nodeOptions.id);
            return;
        }

        const dir = nodeOptions.direction?.toLowerCase() || parentDir || 'right';
        let localAnchor = null;

        for (const child of children) {
            this._planFromNode(child, dir, plan, () => localAnchor, v => { localAnchor = v; });
        }
    }

    // ---------------- Serialization ----------------

    toJSON() {
        try { return JSON.stringify(this.api?.toJSON() ?? {}); } catch (e) { return '{}'; }
    }

    fromJSON(json) {
        try { this.api?.fromJSON(typeof json === 'string' ? JSON.parse(json) : json); } catch (e) { }
    }

    // ---------------- Reinitialize ----------------

    /**
     * Re-Init nach Blazor-Render: robust für gemischte Zustände (manche Items im Dockview,
     * manche nur im DOM, manche temporär weg). Beibehaltet dieselbe Wrapper-Instanz.
     */
    async reinitialize(opts = {}) {
        if (this._isReinitializing) return;
        this._isReinitializing = true;

        const { preserveLayout = true, raiseReady = false, initialLayoutJson = null } = opts;

        let layoutJson = '{}';
        if (preserveLayout) {
            try { layoutJson = this.toJSON(); } catch (e) { layoutJson = '{}'; }
        } else if (initialLayoutJson) {
            layoutJson = initialLayoutJson;
        }

        // alle bekannten Elemente in Stash parken (überleben Dispose)
        try {
            const stash = this._ensureStash();
            for (const [id, el] of this.elementStore.entries()) {
                if (!el) continue;
                try {
                    el.dataset.dvId = id;
                    if (el.parentElement !== stash) stash.appendChild(el);
                    el.style.display = 'none';
                } catch { /* noop */ }
            }
        } catch { /* noop */ }

        // Events/Observer aus, API entsorgen — Wrapper bleibt identisch
        try { this.disposeEvents(); } catch { }
        try { this.observer?.disconnect?.(); } catch { }
        try { this.api?.dispose?.(); } catch { }

        // API neu erzeugen (gleiche Wrapper-Instanz!)
        const { createDockview } = await import(this.options.module);
        this.api = createDockview(this.containerRef, {
            theme: { className: this.options.className ?? 'dockview-theme-abyss' },
            dndEdges: { top: true, right: true, bottom: true, left: true },
            createComponent: (opts) => this._createComponent(opts)
        });

        // DOM-Leaves mergen & Layout wiederherstellen
        this.indexDomLeaves();

        try {
            const parsed = JSON.parse(layoutJson || '{}');
            if (parsed && Object.keys(parsed).length > 0) {
                this.api.fromJSON(parsed);
                this.bootstrapped = true;
            } else {
                const roots = this._rootNodes();
                if (roots.length > 0) {
                    this.bootstrapFromDom(roots);
                    this.bootstrapped = true;
                }
            }
        } catch (e) {
            console.warn('reinitialize.fromJSON failed -> fallback to DOM bootstrap', e);
            const roots = this._rootNodes();
            if (roots.length > 0) {
                this.bootstrapFromDom(roots);
                this.bootstrapped = true;
            }
        }

        this.wireEvents();
        if (raiseReady) {
            try { this.dotnet?.invokeMethodAsync('OnJsReady'); } catch { }
        }

        // Duplikate final einsammeln
        try {
            for (const [id, el] of this.elementStore.entries()) {
                if (!id || !el) continue;
                this._hideAndStashDuplicates(id, el);
            }
        } catch { }

        this._isReinitializing = false;
    }

    // ---------------- Cleanup ----------------

    dispose() {
        this.disposeEvents();
        try { this.observer?.disconnect?.(); } catch { }
        try { this.api?.dispose?.(); } catch { }
    }
}

window.MudExDockLayout = MudExDockLayout;

// Caching: pro containerRef nur eine Wrapper-Instanz zurückgeben
export function initializeMudExDockLayout(elementRef, containerRef, dotnet, options) {
    if (containerRef.__mudExDockInstance && containerRef.__mudExDockInstance instanceof MudExDockLayout) {
        // ggf. Options/DotNet aktualisieren
        const inst = containerRef.__mudExDockInstance;
        inst.setOptions?.(options || inst.options);
        inst.dotnet = dotnet || inst.dotnet;
        return inst; // dieselbe Instanz
    }
    const inst = new MudExDockLayout(elementRef, containerRef, dotnet, options);
    containerRef.__mudExDockInstance = inst; // cache
    return inst;
}
