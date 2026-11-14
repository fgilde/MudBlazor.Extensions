const FOCUSABLE = 'button, input, select, textarea, [tabindex]';
const GROUP = '[role=toolbar], .mud-ex-ribbon-toolbar-group';
const ROVING = 'data-mudex-roving';
const KEYTIP_FRAMES = 30;

class MudExRibbon {
    constructor(elementRef, dotnet, panelId) {
        this.elementRef = elementRef;
        this.dotnet = dotnet;
        this.panelId = panelId;
        this.step = 0;
        this.max = 0;
        this.widths = [];
        this.frame = 0;
        this.busy = false;
        this.observed = null;
        this.header = null;
        this.backstageId = null;
        this.observer = new ResizeObserver(() => {
            this.placeBackstage(this.backstageId);
            this.schedule();
        });
        document.fonts?.ready.then(() => this.schedule());
        this.altPressed = false;
        this.keyTipLevel = null;
        this.keyTipBuffer = '';
        this.keyTipBadges = [];
        this.keyTipLayer = null;
        this.keyTipFrame = 0;
        this.rovingFrame = 0;
        this.onDocumentKeyDown = e => this.documentKeyDown(e);
        this.onDocumentKeyUp = e => this.documentKeyUp(e);
        this.onCancel = () => {
            this.altPressed = false;
            this.endKeyTips();
        };
        this.onViewportChange = () => this.endKeyTips();
        this.onRootKeyDown = e => this.rootKeyDown(e);
        this.onFocusIn = e => this.focusIn(e);
        document.addEventListener('keydown', this.onDocumentKeyDown, true);
        document.addEventListener('keyup', this.onDocumentKeyUp, true);
        document.addEventListener('pointerdown', this.onCancel, true);
        window.addEventListener('blur', this.onCancel);
        elementRef.addEventListener('keydown', this.onRootKeyDown);
        elementRef.addEventListener('focusin', this.onFocusIn);
        this.mutations = new MutationObserver(() => this.scheduleRoving());
        this.mutations.observe(elementRef, { childList: true, subtree: true, attributes: true, attributeFilter: ['disabled', 'class', 'hidden', 'style'] });
        this.scheduleRoving();
    }

    update(step, max, reset) {
        if (reset) {
            this.widths = [];
        }
        this.step = step;
        this.max = max;
        this.schedule();
    }

    schedule() {
        if (this.frame || !this.dotnet) {
            return;
        }
        this.frame = requestAnimationFrame(() => {
            this.frame = 0;
            this.check();
        });
    }

    body() {
        const body = document.getElementById(this.panelId);
        if (body !== this.observed) {
            if (this.observed) {
                this.observer.unobserve(this.observed);
            }
            this.observed = body;
            if (body) {
                this.observer.observe(body);
            }
        }
        return body;
    }

    check() {
        const body = this.body();
        if (!body || this.busy || !this.dotnet) {
            return;
        }
        const width = body.clientWidth;
        const overflows = body.scrollWidth > width;
        let next = this.step;
        if (overflows && this.step < this.max) {
            this.widths[this.step] = body.scrollWidth;
            next++;
        } else if (!overflows && this.step > 0 && width >= (this.widths[this.step - 1] ?? 0) + 8) {
            next--;
        }
        if (next === this.step) {
            return;
        }
        this.step = next;
        this.busy = true;
        this.dotnet.invokeMethodAsync('SetReductionStep', next)
            .catch(() => { })
            .finally(() => {
                this.busy = false;
                this.schedule();
            });
    }

    placeBackstage(id) {
        this.backstageId = id;
        const backstage = id ? document.getElementById(id) : null;
        const header = backstage?.previousElementSibling;
        if (!header) {
            return;
        }
        backstage.style.top = `${header.offsetTop + header.offsetHeight}px`;
        if (header !== this.header) {
            if (this.header) {
                this.observer.unobserve(this.header);
            }
            this.header = header;
            this.observer.observe(header);
        }
    }

    keyTipsAllowed() {
        const root = this.elementRef;
        if (!root?.isConnected) {
            return false;
        }
        const active = document.activeElement;
        if (active && root.contains(active)) {
            return true;
        }
        const modal = active?.closest?.('[aria-modal="true"], .mud-dialog');
        if (modal && !modal.contains(root)) {
            return false;
        }
        const ribbons = document.querySelectorAll('.mud-ex-ribbon');
        return ribbons[0] === root && (ribbons.length === 1 || !active || active === document.body);
    }

    documentKeyDown(e) {
        if (e.key === 'Alt') {
            if (!e.repeat) {
                this.altPressed = !e.ctrlKey && !e.shiftKey && !e.metaKey;
            }
            return;
        }
        this.altPressed = false;
        if (!this.keyTipLevel) {
            return;
        }
        if (e.key === 'Escape') {
            e.preventDefault();
            e.stopPropagation();
            if (this.keyTipLevel === 'item' && this.keyTipTargets('tab').length) {
                this.showKeyTips('tab');
            } else {
                this.endKeyTips();
            }
            return;
        }
        if (e.key.length !== 1 || e.ctrlKey || e.metaKey || e.altKey) {
            this.endKeyTips();
            return;
        }
        e.preventDefault();
        e.stopPropagation();
        this.typeKeyTip(e.key.toUpperCase());
    }

    documentKeyUp(e) {
        if (e.key !== 'Alt' || !this.altPressed) {
            return;
        }
        this.altPressed = false;
        if (this.keyTipLevel) {
            e.preventDefault();
            this.endKeyTips();
        } else if (this.keyTipsAllowed() && this.showKeyTips('tab')) {
            e.preventDefault();
        }
    }

    keyTipTargets(level) {
        const selector = `[data-mudex-keytip-level="${level}"]`;
        return [...this.elementRef.querySelectorAll(selector), ...document.querySelectorAll(`.mud-ex-ribbon-overlay ${selector}`)]
            .filter(el => !el.closest('.mud-ex-ribbon-quick-access') && !el.disabled
                && (el.checkVisibility?.({ visibilityProperty: true, opacityProperty: true }) ?? el.getClientRects().length > 0));
    }

    showKeyTips(level) {
        let targets = this.keyTipTargets(level);
        if (!targets.length && level === 'tab') {
            level = 'item';
            targets = this.keyTipTargets(level);
        }
        this.endKeyTips();
        if (!targets.length) {
            return false;
        }
        const layer = document.createElement('div');
        layer.className = 'mud-ex-ribbon-keytips';
        this.keyTipBadges = targets.map(el => {
            const rect = el.getBoundingClientRect();
            const badge = document.createElement('span');
            badge.className = 'mud-ex-ribbon-keytip';
            badge.textContent = el.dataset.mudexKeytip;
            badge.style.left = `${rect.left + rect.width / 2}px`;
            badge.style.top = `${rect.bottom}px`;
            layer.appendChild(badge);
            return { el, tip: el.dataset.mudexKeytip, badge };
        });
        document.body.appendChild(layer);
        this.keyTipLayer = layer;
        this.keyTipLevel = level;
        window.addEventListener('scroll', this.onViewportChange, true);
        window.addEventListener('resize', this.onViewportChange);
        return true;
    }

    typeKeyTip(key) {
        const buffer = this.keyTipBuffer + key;
        const matches = this.keyTipBadges.filter(b => b.tip.startsWith(buffer));
        if (!matches.length) {
            return;
        }
        const exact = matches.find(b => b.tip === buffer);
        if (exact) {
            this.activateKeyTip(exact.el);
            return;
        }
        this.keyTipBuffer = buffer;
        this.keyTipBadges.forEach(b => b.badge.hidden = !b.tip.startsWith(buffer));
    }

    activateKeyTip(el) {
        const target = el.matches(FOCUSABLE) ? el : el.querySelector(FOCUSABLE) ?? el;
        this.endKeyTips();
        target.focus();
        if (target.matches('input, select, textarea')) {
            return;
        }
        target.click();
        if (el.getAttribute('role') === 'tab') {
            this.awaitItemKeyTips(el, KEYTIP_FRAMES);
        }
    }

    awaitItemKeyTips(tab, frames) {
        this.keyTipFrame = requestAnimationFrame(() => {
            this.keyTipFrame = 0;
            const shown = tab.getAttribute('aria-selected') === 'true' && tab.getAttribute('aria-expanded') !== 'false';
            if (shown) {
                this.showKeyTips('item');
            } else if (frames <= 0) {
                this.endKeyTips();
            } else {
                this.awaitItemKeyTips(tab, frames - 1);
            }
        });
    }

    endKeyTips() {
        window.removeEventListener('scroll', this.onViewportChange, true);
        window.removeEventListener('resize', this.onViewportChange);
        cancelAnimationFrame(this.keyTipFrame);
        this.keyTipFrame = 0;
        this.keyTipLayer?.remove();
        this.keyTipLayer = null;
        this.keyTipBadges = [];
        this.keyTipLevel = null;
        this.keyTipBuffer = '';
    }

    scheduleRoving() {
        if (this.rovingFrame) {
            return;
        }
        this.rovingFrame = requestAnimationFrame(() => {
            this.rovingFrame = 0;
            this.elementRef?.querySelectorAll(GROUP).forEach(group => this.setRovingItem(group));
        });
    }

    rovingMembers(group) {
        return [...group.querySelectorAll(FOCUSABLE)].filter(el => el.closest(GROUP) === group
            && !el.disabled && !el.hasAttribute('disabled')
            && (el.hasAttribute(ROVING) || el.getAttribute('tabindex') !== '-1'));
    }

    shownRovingMembers(group) {
        return this.rovingMembers(group).filter(el => el.getClientRects().length > 0);
    }

    setRovingItem(group, current) {
        const members = this.rovingMembers(group);
        const shown = members.filter(el => el.getClientRects().length > 0);
        current ??= shown.find(el => el.hasAttribute(ROVING) && el.tabIndex === 0) ?? shown[0];
        members.forEach(el => {
            el.setAttribute(ROVING, '');
            el.tabIndex = el === current ? 0 : -1;
        });
    }

    rootKeyDown(e) {
        if (this.keyTipLevel || e.defaultPrevented || e.altKey || e.ctrlKey || e.metaKey || e.shiftKey
            || !['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(e.key)) {
            return;
        }
        const target = e.target;
        let group = null;
        let items;
        if (target.getAttribute('role') === 'tab') {
            items = [...this.elementRef.querySelectorAll('[role=tab]')];
        } else {
            group = target.closest(GROUP);
            if (!group || target.matches('input, select, textarea')) {
                return;
            }
            items = this.shownRovingMembers(group);
        }
        const index = items.indexOf(target);
        if (index < 0) {
            return;
        }
        const next = e.key === 'Home' ? 0
            : e.key === 'End' ? items.length - 1
            : (index + (e.key === 'ArrowLeft' ? -1 : 1) + items.length) % items.length;
        e.preventDefault();
        if (group) {
            this.setRovingItem(group, items[next]);
        }
        items[next].focus();
    }

    focusIn(e) {
        const group = e.target.closest?.(GROUP);
        if (group && this.shownRovingMembers(group).includes(e.target)) {
            this.setRovingItem(group, e.target);
        }
    }

    dispose() {
        cancelAnimationFrame(this.frame);
        cancelAnimationFrame(this.rovingFrame);
        this.endKeyTips();
        document.removeEventListener('keydown', this.onDocumentKeyDown, true);
        document.removeEventListener('keyup', this.onDocumentKeyUp, true);
        document.removeEventListener('pointerdown', this.onCancel, true);
        window.removeEventListener('blur', this.onCancel);
        this.elementRef?.removeEventListener('keydown', this.onRootKeyDown);
        this.elementRef?.removeEventListener('focusin', this.onFocusIn);
        this.mutations.disconnect();
        this.observer.disconnect();
        this.elementRef = null;
        this.dotnet = null;
        this.observed = null;
        this.header = null;
    }
}

export function initializeMudExRibbon(elementRef, dotnet, panelId) {
    return new MudExRibbon(elementRef, dotnet, panelId);
}
