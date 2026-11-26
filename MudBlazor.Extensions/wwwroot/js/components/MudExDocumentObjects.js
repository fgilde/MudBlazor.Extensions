import { OPAQUE } from './MudExDocumentDom.js';

const CORNERS = ['nw', 'ne', 'sw', 'se'];
const EDGE = 4;
const MIN_SIZE = 20;
const MAX_SIZE = 20000;
const IMAGE = '[data-k="i"]';

const imageOf = (sheet) => {
    const selection = document.getSelection();
    if (!selection?.rangeCount || selection.isCollapsed) {
        return null;
    }
    const range = selection.getRangeAt(0);
    if (!sheet.contains(range.commonAncestorContainer) || range.toString().split(String.fromCharCode(0x200B)).join('') !== '') {
        return null;
    }
    const images = [...sheet.querySelectorAll(IMAGE)].filter(node => range.intersectsNode(node));
    return images.length === 1 && images[0].dataset.w ? images[0] : null;
};

const edgeOf = (e, zoom) => {
    const cell = e.target instanceof Element ? e.target.closest('td') : null;
    const table = cell?.closest('table');
    if (!table || table.parentElement.closest(OPAQUE) || !table.dataset.b) {
        return null;
    }
    const box = cell.getBoundingClientRect();
    if (Math.abs(e.clientX - box.right) > EDGE) {
        return null;
    }
    const columns = [...table.querySelectorAll(':scope > colgroup > col')];
    const at = (box.right - table.getBoundingClientRect().left) / zoom;
    let sum = 0;
    for (let index = 0; index < columns.length; index++) {
        sum += parseFloat(columns[index].style.width);
        if (Math.abs(sum - at) < EDGE) {
            return { table, columns, index };
        }
    }
    return null;
};

export const objectMethods = {
    objectEvents() {
        this.mini = false;
        this.edge = null;
        this.imageSel = null;
        this.handles = null;
        return [
            [this.sheet, 'mousemove', e => this.edgeHover(e)], [this.sheet, 'mousedown', e => this.edgeDown(e), true],
            [this.sheet, 'contextmenu', e => this.contextMenu(e)], [this.sheet, 'mouseup', () => setTimeout(() => this.miniUpdate(), 0)],
            [this.sheet, 'keyup', e => (e.key.startsWith('Arrow') || e.key === 'Shift') && this.miniUpdate()],
            [this.sheet, 'keydown', e => this.miniKey(e)], [this.root, 'scroll', () => this.miniHide()]
        ];
    },

    objectsDispose() {
        this.handles?.remove();
        this.handles = null;
    },

    objectsUpdate() {
        const image = this.inert ? null : imageOf(this.sheet);
        this.imageSel = image;
        if (image) {
            this.placeHandles();
        } else if (this.handles) {
            this.handles.hidden = true;
        }
        const selection = document.getSelection();
        if (this.mini && (!selection?.rangeCount || selection.isCollapsed)) {
            this.miniHide();
        }
    },

    placeHandles() {
        const image = this.imageSel;
        if (!image?.isConnected) {
            return;
        }
        if (!this.handles) {
            const box = document.createElement('div');
            box.className = 'mud-ex-doc-handles';
            box.contentEditable = 'false';
            CORNERS.forEach(corner => {
                const handle = document.createElement('div');
                handle.className = `mud-ex-doc-handle mud-ex-doc-handle-${corner}`;
                handle.addEventListener('pointerdown', e => this.handleDown(e, corner));
                box.append(handle);
            });
            this.root.append(box);
            this.handles = box;
        }
        const rect = image.getBoundingClientRect();
        const root = this.root.getBoundingClientRect();
        Object.assign(this.handles.style, {
            left: `${rect.left - root.left + this.root.scrollLeft}px`, top: `${rect.top - root.top + this.root.scrollTop}px`,
            width: `${rect.width}px`, height: `${rect.height}px`
        });
        this.handles.hidden = false;
    },

    handleDown(e, corner) {
        const image = this.imageSel;
        if (!image || this.inert || e.button !== 0) {
            return;
        }
        e.preventDefault();
        e.stopPropagation();
        const target = e.currentTarget;
        target.setPointerCapture(e.pointerId);
        const zoom = this.zoom || 1;
        const w0 = Number(image.dataset.w);
        const h0 = Number(image.dataset.h);
        const x0 = e.clientX;
        let size = [w0, h0];
        const move = ev => {
            const dx = (ev.clientX - x0) / zoom * (corner.endsWith('w') ? -1 : 1);
            const w = Math.min(MAX_SIZE, Math.max(MIN_SIZE, w0 + dx));
            size = [w, Math.max(1, w * h0 / w0)];
            if (image.tagName === 'IMG') {
                image.width = size[0];
                image.height = size[1];
            } else {
                image.style.width = `${size[0]}px`;
                image.style.height = `${size[1]}px`;
            }
            this.placeHandles();
        };
        const up = () => {
            target.removeEventListener('pointermove', move);
            target.removeEventListener('pointerup', up);
            target.removeEventListener('pointercancel', up);
            if (Math.round(size[0]) !== w0 || Math.round(size[1]) !== h0) {
                this.command('imageSize', `${Math.round(size[0])},${Math.round(size[1])}`);
            }
        };
        target.addEventListener('pointermove', move);
        target.addEventListener('pointerup', up);
        target.addEventListener('pointercancel', up);
    },

    edgeHover(e) {
        if (this.inert || e.buttons) {
            return;
        }
        this.edge = edgeOf(e, this.zoom || 1);
        this.sheet.style.cursor = this.edge ? 'col-resize' : '';
    },

    edgeDown(e) {
        const edge = this.inert || e.button !== 0 ? null : edgeOf(e, this.zoom || 1);
        if (!edge) {
            this.miniHide();
            return;
        }
        e.preventDefault();
        e.stopPropagation();
        const zoom = this.zoom || 1;
        const { table, columns, index } = edge;
        const widths = columns.map(c => parseFloat(c.style.width));
        const start = [...widths];
        const x0 = e.clientX;
        const apply = () => {
            columns.forEach((c, i) => {
                c.style.width = `${widths[i]}px`;
            });
            table.style.width = `${widths.reduce((a, b) => a + b, 0)}px`;
        };
        const move = ev => {
            const dx = (ev.clientX - x0) / zoom;
            const last = index === columns.length - 1;
            const delta = last ? Math.max(dx, MIN_SIZE - start[index]) : Math.min(Math.max(dx, MIN_SIZE - start[index]), start[index + 1] - MIN_SIZE);
            widths[index] = start[index] + delta;
            if (!last) {
                widths[index + 1] = start[index + 1] - delta;
            }
            apply();
        };
        const up = () => {
            document.removeEventListener('mousemove', move, true);
            document.removeEventListener('mouseup', up, true);
            if (widths.some((w, i) => Math.round(w) !== Math.round(start[i]))) {
                this.command('columnWidths', `${table.dataset.b};${widths.map(w => Math.round(w)).join(';')}`);
            }
        };
        document.addEventListener('mousemove', move, true);
        document.addEventListener('mouseup', up, true);
    },

    contextMenu(e) {
        if (this.inert) {
            return;
        }
        e.preventDefault();
        this.miniHide();
        const x = e.clientX;
        const y = e.clientY;
        this.sync();
        this.enqueue(async () => {
            await this.dotnet.invokeMethodAsync('OnContextMenuAsync', x, y);
        });
    },

    miniKey(e) {
        if (this.mini && (e.key.length === 1 || ['Backspace', 'Delete', 'Enter', 'Tab'].includes(e.key)) && !e.ctrlKey && !e.metaKey) {
            this.miniHide();
        }
    },

    miniUpdate() {
        if (this.inert) {
            return;
        }
        const selection = document.getSelection();
        const range = selection?.rangeCount ? selection.getRangeAt(0) : null;
        if (!range || range.collapsed || !this.sheet.contains(range.commonAncestorContainer) || imageOf(this.sheet)) {
            this.miniHide();
            return;
        }
        const box = range.getBoundingClientRect();
        if (box.width === 0 && box.height === 0) {
            this.miniHide();
            return;
        }
        this.mini = true;
        this.dotnet.invokeMethodAsync('OnMiniToolbarAsync', true, box.left + box.width / 2, box.top).catch(() => { });
    },

    miniHide() {
        if (this.mini) {
            this.mini = false;
            this.dotnet.invokeMethodAsync('OnMiniToolbarAsync', false, 0, 0).catch(() => { });
        }
    }
};
