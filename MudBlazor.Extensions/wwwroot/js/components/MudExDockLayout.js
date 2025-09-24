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
        
        const mode = this.options.mode?.toLowerCase(); // 'grid' | 'dock' | 'pane' | 'split'
        // TODO: Support modes other than 'dock'
        this.api = createDockview(this.containerRef, {
            theme: {
                className: this.options.className ?? 'dockview-theme-abyss'
            },
            dndEdges: { top: true, right: true, bottom: true, left: true },
            createComponent: (opts) => {
                const el = this.elementStore.get(opts.id);
                if (el) {
                    el.style.display = '';
                    el.style.height = '100%';
                    el.style.width = '100%';
                    el.style.overflow = 'auto';
                    return { element: el, init() { }, update() { }, dispose() { } };
                }
                const div = document.createElement('div');
                div.textContent = 'blazor';
                return { element: div, init() { }, update() { }, dispose() { } };
            }
        });

        // 1) DOM-Leaves indexieren (für Restore/InitialLayout)
        this.indexDomLeaves();

        // 2) Initiales Layout anwenden, falls vorhanden
        if (this.options.initialLayoutJson) {
            try {
                this.api.fromJSON(JSON.parse(this.options.initialLayoutJson));
                this.bootstrapped = true;
                this.dotnet?.invokeMethodAsync('OnJsReady');
                return; // kein bootstrapFromDom nötig
            } catch (e) { console.warn('fromJSON failed', e); }
        }

        this.dotnet?.invokeMethodAsync('OnJsReady');
        this.observeAndBootstrap();
        this.wireEvents();
    }

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

    indexDomLeaves() {
        const nodes = this.containerRef.querySelectorAll('.dv-node');
        for (const node of nodes) {
            // Leaf = keine direkten .dv-node-Kinder
            const hasChildNodes = Array.from(node.children).some(n => n.classList?.contains('dv-node'));
            if (!hasChildNodes) {
                const opts = JSON.parse(node.dataset.options || '{}');
                if (!opts.id) continue;             // ohne stabile Id kein Mapping
                node.style.display = 'none';        // wird später ins Panel „übernommen“
                this.elementStore.set(opts.id, node);
            }
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

    toJSON() {
        try { return JSON.stringify(this.api?.toJSON() ?? {}); } catch (e) { return '{}'; }
    }

    fromJSON(json) {
        try { this.api?.fromJSON(typeof json === 'string' ? JSON.parse(json) : json); } catch (e) { }
    }

    dispose() {
        this.disposeEvents();
    }
}

window.MudExDockLayout = MudExDockLayout;

export function initializeMudExDockLayout(elementRef, containerRef, dotnet, options) {
    return new MudExDockLayout(elementRef, containerRef, dotnet, options);
}
