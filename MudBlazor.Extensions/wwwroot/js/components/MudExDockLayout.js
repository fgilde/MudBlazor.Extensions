class MudExDockLayout {
    elementRef; dotnet; options; containerRef; api;
    elementStore = new Map();
    bootstrapped = false;
    observer = null;

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
            className: this.options.className ?? 'dockview-theme-abyss',
            createComponent: (opts) => {
                const el = this.elementStore.get(opts.id);
                if (el) {
                    el.style.display = ''; // sichtbar im Panel
                    el.style.height = '100%';
                    el.style.width = '100%';
                    el.style.overflow = 'auto';
                    return { element: el, init() { }, update() { }, dispose() { } };
                }
                // Fallback – sollte nicht mehr vorkommen
                const div = document.createElement('div');
                div.textContent = `${opts.name ?? 'panel'} content`;
                return { element: div, init() { }, update() { }, dispose() { } };
            }
        });

        this.dotnet?.invokeMethodAsync('OnJsReady');
        this.observeAndBootstrap();
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

        // sofort versuchen
        tryBootstrap();

        // und falls ChildContent später kommt: MutationObserver
        this.observer = new MutationObserver(() => tryBootstrap());
        this.observer.observe(this.containerRef, { childList: true, subtree: true });
    }

    setOptions(options) {
        this.options = options;

    }

    _rootNodes() {
        // direkte Kinder, die dv-node sind
        return Array.from(this.containerRef.children).filter(n => n.classList?.contains('dv-node'));
    }

    // === Planerzeugung: Container positioniert seine direkten Leaf-/Gruppen-Resultate entlang data-dv-dir ===
    bootstrapFromDom(rootNodes) {
        const plan = [];
        let anchor = null;

        for (const root of rootNodes) {
            this._planFromNode(root, /*parentDir*/ 'right', plan, () => anchor, v => anchor = v);
        }

        // Panels anlegen
        for (const d of plan) {
            const payload = {
                id: d.id,
                component: 'blazor',
                title: d.title
            };
            if (d.position) payload.position = d.position;
            if (d.float === true) payload.float = true;
            if (d.locked === true) payload.locked = true;

            const panel = this.api.addPanel(payload);

            // Header pro Gruppe ausblenden
            if (d.hideHeader === true && !this.containerRef.classList.contains('dv-hide-tabs')) {
                const groupEl = panel.group?.element ?? panel.group?._element ?? null;
                if (groupEl) groupEl.classList.add('dv-hide-tabs');
            }
        }
    }

    _planFromNode(node, parentDir, plan, getAnchor, setAnchor) {
        const childNodes = Array.from(node.children).filter(n => n.classList?.contains('dv-node'));
        const isContainer = childNodes.length > 0;

        if (!isContainer) {
            // Leaf: dieses DV-Node-Element direkt als Panel-Inhalt verwenden
            const id = node.dataset.dvId || `p_${Math.random().toString(36).slice(2)}`;
            const title = node.dataset.dvTitle || id;
            const hideHeader = node.dataset.dvHide === 'true';
            const canClose = node.dataset.dvClose !== 'false'; // default true
            const isFloating = node.dataset.dvFloat === 'true';

            // Element vorerst verstecken; später im Panel sichtbar machen
            node.style.display = 'none';
            this.elementStore.set(id, node);

            const desc = {
                id,
                title,
                hideHeader,
                float: isFloating,
                locked: !canClose,
                position: null
            };

            const anchor = getAnchor();
            if (anchor) {
                desc.position = { referencePanel: anchor, direction: parentDir };
            }

            plan.push(desc);
            setAnchor(id);
            return;
        }

        // Container: Richtung für direkte Kinder
        const dir = (node.dataset.dvDir || 'right').toLowerCase();
        let localAnchor = getAnchor();

        for (const child of childNodes) {
            const beforeCount = plan.length;
            this._planFromNode(child, dir, plan, () => localAnchor, v => { localAnchor = v; });
            // Falls der Child-Subplan kein Leaf ergeben hat, bleibt localAnchor unverändert
            if (plan.length > beforeCount && !localAnchor) {
                // setze lokalen Anchor auf das zuletzt hinzugefügte Panel
                localAnchor = plan[plan.length - 1].id;
            }
        }

    }

    toJSON() {
        try { return JSON.stringify(this.api?.toJSON() ?? {}); } catch (e) { return '{}'; }
    }

    fromJSON(json) {
        try { this.api?.fromJSON(typeof json === 'string' ? JSON.parse(json) : json); } catch (e) { }
    }
}

window.MudExDockLayout = MudExDockLayout;

export function initializeMudExDockLayout(elementRef, containerRef, dotnet, options) {
    return new MudExDockLayout(elementRef, containerRef, dotnet, options);
}
