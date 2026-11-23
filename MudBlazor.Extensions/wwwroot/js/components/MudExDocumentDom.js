import { ZWSP } from './MudExRichText.js';

export const PARAGRAPH = 'p.mud-ex-doc-p';
export const OPAQUE = '.mud-ex-doc-opaque';
const ATOMIC = new Set(['v', 'f', 'o', 'i']);
const OBJECT = String.fromCharCode(0xFFFC);
const MODEL = { v: '\v', f: '\f', o: OBJECT, i: OBJECT };

export const strip = text => text.split(ZWSP).join('');

const element = (tag, css, id) => {
    const node = document.createElement(tag);
    if (css) {
        node.setAttribute('style', css);
    }
    if (id != null) {
        node.dataset.b = id;
    }
    return node;
};

const marker = (tag, kind, css) => {
    const node = element(tag, css);
    node.dataset.k = kind;
    if (tag !== 'br') {
        node.contentEditable = 'false';
    }
    return node;
};

export function run(dto, media) {
    if (dto.k === 't') {
        const span = element('span', dto.css);
        span.dataset.c = dto.css ?? '';
        span.textContent = dto.t ?? '';
        return span;
    }
    let node;
    if (dto.k === 'v') {
        node = marker('br', 'v');
    } else if (dto.k === 'f') {
        node = marker('span', 'f', dto.css);
    } else if (dto.k === 'i' && media?.get(dto.media)) {
        node = marker('img', 'i', dto.css);
        node.draggable = false;
        node.alt = '';
        node.width = dto.w;
        node.height = dto.h;
        node.src = media.get(dto.media);
    } else if (dto.k === 'i') {
        node = marker('span', 'i', dto.css);
        node.style.display = 'inline-block';
        node.style.width = `${dto.w}px`;
        node.style.height = `${dto.h}px`;
    } else {
        node = marker('span', 'o', dto.css);
        node.textContent = dto.t ?? '';
    }
    if (dto.k === 'i') {
        node.dataset.m = dto.media ?? '';
        node.dataset.w = String(dto.w);
        node.dataset.h = String(dto.h);
    }
    node.dataset.c = dto.css ?? '';
    return node;
}

const ends = dto => !dto.text || ATOMIC.has(dto.runs?.[dto.runs.length - 1]?.k);

export function paragraph(dto, media) {
    const p = element('p', dto.css, dto.id);
    p.className = 'mud-ex-doc-p';
    p.dataset.c = dto.css ?? '';
    if (dto.label != null) {
        const label = marker('span', 'l', dto.labelCss);
        label.dataset.c = dto.labelCss ?? '';
        label.textContent = dto.label;
        p.append(label);
    }
    (dto.runs ?? []).forEach(r => p.append(run(r, media)));
    if (ends(dto)) {
        p.append(marker('br', 'e'));
    }
    return p;
}

function table(dto, media) {
    const node = element('table', dto.css, dto.id);
    if (dto.grid?.length) {
        const columns = document.createElement('colgroup');
        dto.grid.forEach(width => {
            const column = document.createElement('col');
            column.style.width = `${width}px`;
            columns.append(column);
        });
        node.append(columns);
    }
    const body = document.createElement('tbody');
    (dto.rows ?? []).forEach(row => {
        const tr = element('tr', row.css);
        (row.cells ?? []).forEach(cell => {
            const td = element('td', cell.css);
            if (cell.colSpan > 1) {
                td.colSpan = cell.colSpan;
            }
            if (cell.rowSpan > 1) {
                td.rowSpan = cell.rowSpan;
            }
            (cell.blocks ?? []).forEach(block => td.append(render(block, media)));
            tr.append(td);
        });
        body.append(tr);
    });
    node.append(body);
    return node;
}

export function render(dto, media) {
    if (dto.k === 'p') {
        return paragraph(dto, media);
    }
    if (dto.k === 't') {
        return table(dto, media);
    }
    const node = element('div', dto.css, dto.id);
    if (dto.k === 'h') {
        node.hidden = true;
        return node;
    }
    node.className = 'mud-ex-doc-opaque';
    node.contentEditable = 'false';
    (dto.blocks ?? []).forEach(block => node.append(render(block, media)));
    return node;
}

export function units(p) {
    const list = [];
    const walk = parent => {
        for (let node = parent.firstChild; node; node = node.nextSibling) {
            if (node.nodeType === Node.TEXT_NODE) {
                list.push({ node, len: strip(node.data).length });
            } else if (node.nodeType === Node.ELEMENT_NODE) {
                const kind = node.dataset?.k;
                if (kind) {
                    if (ATOMIC.has(kind)) {
                        list.push({ node, len: 1, kind });
                    }
                } else if (node.tagName !== 'BR') {
                    walk(node);
                }
            }
        }
    };
    walk(p);
    return list;
}

export const length = p => units(p).reduce((sum, unit) => sum + unit.len, 0);

const keyed = (p, node) => {
    for (let current = node.nodeType === Node.ELEMENT_NODE ? node : node.parentElement; current && current !== p; current = current.parentElement) {
        if (current.dataset?.k) {
            return current;
        }
    }
    return null;
};

const indexIn = node => Array.prototype.indexOf.call(node.parentNode.childNodes, node);

const before = (unit, range) => unit.node.nodeType === Node.TEXT_NODE
    ? range.comparePoint(unit.node, unit.node.length) <= 0
    : range.comparePoint(unit.node.parentNode, indexIn(unit.node) + 1) <= 0;

export function offsetOf(p, container, offset) {
    if (!p?.contains(container) || p.parentElement?.closest(OPAQUE)) {
        return null;
    }
    const all = units(p);
    const own = keyed(p, container);
    if (own) {
        const index = all.findIndex(unit => unit.node === own);
        const sum = all.slice(0, Math.max(index, 0)).reduce((total, unit) => total + unit.len, 0);
        return index < 0 ? (own.dataset.k === 'l' ? 0 : sum) : sum + (offset > 0 ? 1 : 0);
    }
    let sum = 0;
    if (container.nodeType === Node.TEXT_NODE) {
        for (const unit of all) {
            if (unit.node === container) {
                return sum + strip(container.data.slice(0, offset)).length;
            }
            sum += unit.len;
        }
        return null;
    }
    const range = document.createRange();
    range.setStart(container, offset);
    for (const unit of all) {
        if (!before(unit, range)) {
            break;
        }
        sum += unit.len;
    }
    return sum;
}

const local = (text, k) => {
    let seen = 0;
    for (let i = 0; i < text.length; i++) {
        if (seen === k && text[i] !== ZWSP) {
            return i;
        }
        if (text[i] !== ZWSP) {
            seen++;
        }
    }
    return text.length;
};

const after = node => ({ node: node.parentNode, offset: indexIn(node) + 1 });

export function pointAt(p, k) {
    const all = units(p);
    let acc = 0;
    for (const unit of all) {
        if (!unit.kind && acc < k && k <= acc + unit.len) {
            return { node: unit.node, offset: local(unit.node.data, k - acc) };
        }
        acc += unit.len;
    }
    acc = 0;
    let atomic = null;
    for (const unit of all) {
        if (!unit.kind && acc === k && unit.len > 0) {
            return { node: unit.node, offset: local(unit.node.data, 0) };
        }
        acc += unit.len;
        if (unit.kind && acc === k) {
            atomic = unit.node;
        }
    }
    if (atomic) {
        return after(atomic);
    }
    if (k > 0 && all.length) {
        const last = all[all.length - 1];
        return last.kind ? after(last.node) : { node: last.node, offset: last.node.length };
    }
    const first = all[0];
    if (first?.kind) {
        return { node: first.node.parentNode, offset: indexIn(first.node) };
    }
    if (first) {
        return { node: first.node, offset: first.node.data.lastIndexOf(ZWSP) + 1 };
    }
    const label = [...p.childNodes].findIndex(node => node.dataset?.k === 'l');
    return { node: p, offset: label + 1 };
}

export const serialise = p => units(p).map(unit => unit.kind ? MODEL[unit.kind] : strip(unit.node.data)).join('');

const same = (node, dto) => {
    if (node?.nodeType !== Node.ELEMENT_NODE || node.dataset.c !== (dto.css ?? '')) {
        return false;
    }
    if (dto.k === 't') {
        const text = node.firstChild;
        return node.tagName === 'SPAN' && !node.dataset.k && !node.dataset.p && node.childNodes.length <= 1
            && (text ? text.nodeType === Node.TEXT_NODE && text.data === (dto.t ?? '') : !dto.t);
    }
    if (dto.k === 'i') {
        return node.dataset.k === 'i' && node.dataset.m === (dto.media ?? '') && node.dataset.w === String(dto.w) && node.dataset.h === String(dto.h);
    }
    return node.dataset.k === dto.k && (dto.k !== 'o' || node.textContent === (dto.t ?? ''));
};

export function sameRuns(p, dto) {
    if (p?.tagName !== 'P' || dto.k !== 'p' || p.dataset.c !== (dto.css ?? '')) {
        return false;
    }
    const nodes = [...p.childNodes];
    const label = nodes[0]?.dataset?.k === 'l' ? nodes.shift() : null;
    if ((label == null) !== (dto.label == null) || label && (label.textContent !== dto.label || label.dataset.c !== (dto.labelCss ?? ''))) {
        return false;
    }
    const end = ends(dto);
    if (end !== (nodes[nodes.length - 1]?.dataset?.k === 'e')) {
        return false;
    }
    if (end) {
        nodes.pop();
    }
    const runs = dto.runs ?? [];
    return nodes.length === runs.length && runs.every((r, i) => same(nodes[i], r));
}

const editable = p => !p.parentElement.closest(OPAQUE) && !p.closest('[hidden]');

const following = (sheet, node) => {
    for (let current = node; current && current !== sheet; current = current.parentNode) {
        if (current.nextSibling) {
            return current.nextSibling;
        }
    }
    return null;
};

function nearest(sheet, node, offset) {
    const walker = document.createTreeWalker(sheet, NodeFilter.SHOW_ELEMENT, {
        acceptNode: n => n.matches(PARAGRAPH) && editable(n) ? NodeFilter.FILTER_ACCEPT : NodeFilter.FILTER_SKIP
    });
    const next = node.childNodes?.[offset] ?? following(sheet, node);
    if (next) {
        walker.currentNode = next;
        const found = next.nodeType === Node.ELEMENT_NODE && next.matches(PARAGRAPH) && editable(next) ? next : walker.nextNode();
        if (found) {
            return { p: found.dataset.b, o: 0 };
        }
    }
    let last = offset > 0 ? node.childNodes?.[offset - 1] : node;
    while (last?.lastChild && offset > 0) {
        last = last.lastChild;
    }
    walker.currentNode = last ?? node;
    const current = walker.currentNode;
    const found = current.nodeType === Node.ELEMENT_NODE && current.matches(PARAGRAPH) && editable(current) && current !== node ? current : walker.previousNode();
    return found ? { p: found.dataset.b, o: length(found) } : null;
}

export function pointOf(sheet, node, offset) {
    if (!node || !sheet.contains(node)) {
        return null;
    }
    const start = node.nodeType === Node.ELEMENT_NODE ? node : node.parentElement;
    const p = start.closest(PARAGRAPH);
    if (p && sheet.contains(p)) {
        const o = offsetOf(p, node, offset);
        return o == null ? null : { p: p.dataset.b, o };
    }
    if (start.closest(OPAQUE)) {
        return null;
    }
    return nearest(sheet, node, offset);
}
