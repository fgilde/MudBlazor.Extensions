function clsNum(gridEl, prefix) {
    const c = [...gridEl.classList].find(x => x.startsWith(prefix));
    return c ? parseInt(c.split("-").pop()) : null;
}

function readItemFromClasses(el) {
    const g = p => {
        const c = [...el.classList].find(x => x.startsWith(`mud-ex-grid-section-${p}-`));
        return c ? parseInt(c.split("-").pop()) : 1;
    };
    const rs = g("row-start"), re = g("row-end"), cs = g("col-start"), ce = g("col-end");
    return{ row: rs, col: cs, rowSpan: re - rs, colSpan: ce - cs };
}

function buildModel(gridEl) {
    return[...gridEl.querySelectorAll(".mud-ex-grid-section")].map(el => ({
        id: el.dataset.id,
        el,
        ...readItemFromClasses(el)
    }));
}

function measure(gridEl) {
    const r = gridEl.getBoundingClientRect();
    const rows = clsNum(gridEl, "mud-ex-grid-row-") || 12;
    const cols = clsNum(gridEl, "mud-ex-grid-column-") || 12;
    return{ rows, cols, cw: r.width / cols, ch: r.height / rows, left: r.left, top: r.top };
}

function px(item, m) {
    return{ x: (item.col - 1) * m.cw, y: (item.row - 1) * m.ch, w: item.colSpan * m.cw, h: item.rowSpan * m.ch };
}

function rectsOverlap(a, b) {
    return!(a.row + a.rowSpan - 1 < b.row ||
        b.row + b.rowSpan - 1 < a.row ||
        a.col + a.colSpan - 1 < b.col ||
        b.col + b.colSpan - 1 < a.col);
}

function clearTransforms(gridEl) {
    gridEl.querySelectorAll(".mud-ex-grid-section").forEach(el => {
        el.style.transform = "";
        el.style.transition = "";
    });
}

function canPlace(layout, itm) { return!layout.some(x => x.id !== itm.id && rectsOverlap(itm, x)); }

function canPlaceWithOpts(layout, itm, rows, cols, optsMap) {
    const o = optsMap.get(itm.id) || {};
    const minc = Math.max(1, o.minCol ?? 1);
    const maxc = Math.min(cols - itm.colSpan + 1, o.maxCol ?? Number.MAX_SAFE_INTEGER);
    const minr = Math.max(1, o.minRow ?? 1);
    const maxr = Math.min(rows - itm.rowSpan + 1, o.maxRow ?? Number.MAX_SAFE_INTEGER);
    if (itm.col < minc || itm.col > maxc) return false;
    if (itm.row < minr || itm.row > maxr) return false;
    if (itm.row + itm.rowSpan - 1 > rows || itm.col + itm.colSpan - 1 > cols) return false;
    return!layout.some(x => x.id !== itm.id && rectsOverlap(itm, x));
}

function bfsPush(layout, moving, rows, cols, optsMap) {
    if (canPlaceWithOpts(layout, moving, rows, cols, optsMap)) return layout;
    const q = [moving];
    const seen = new Set([moving.id]);
    while (q.length) {
        const cur = q.shift();
        for (const other of layout) {
            if (other.id === cur.id) continue;
            if (rectsOverlap(cur, other)) {
                const opts = [
                    { row: cur.row + cur.rowSpan, col: other.col }, { row: other.row, col: cur.col + cur.colSpan }
                ];
                let placed = false;
                for (const t of opts) {
                    let r = Math.max(1, t.row), c = Math.max(1, t.col);
                    while (r <= rows && !placed) {
                        while (c <= cols) {
                            const cand = { ...other, row: r, col: c };
                            if (!layout.some(x => x.id !== cand.id && rectsOverlap(cand, x)) &&
                                cand.row + cand.rowSpan - 1 <= rows &&
                                cand.col + cand.colSpan - 1 <= cols) {
                                other.row = cand.row;
                                other.col = cand.col;
                                placed = true;
                                break;
                            }
                            c++;
                        }
                        if (!placed) {
                            c = 1;
                            r++;
                        }
                    }
                }
                if (!seen.has(other.id)) {
                    q.push(other);
                    seen.add(other.id);
                }
            }
        }
    }
    return layout;
}

function trySwapOrPush(layout, moving, rows, cols, optsMap) {
    if (canPlaceWithOpts(layout, moving, rows, cols, optsMap)) return layout;
    const overlappers = layout.filter(x => x.id !== moving.id && rectsOverlap(moving, x));
    if (overlappers.length === 1) {
        const target = overlappers[0];
        const moverOld = layout.find(x => x.id === moving.id);
        const candidateInOld = { ...target, row: moverOld.row, col: moverOld.col };
        const canSwapPlace = !layout.some(x => x.id !== target.id && rectsOverlap(candidateInOld, x)) &&
            candidateInOld.row + candidateInOld.rowSpan - 1 <= rows &&
            candidateInOld.col + candidateInOld.colSpan - 1 <= cols;
        if (canSwapPlace) {
            target.row = moverOld.row;
            target.col = moverOld.col;
            return layout;
        }
    }
    return bfsPush(layout, moving, rows, cols, optsMap);
}

function floatUp(layout, rows, cols) {
    for (const it of layout) {
        let moved = true;
        while (moved) {
            const cand = { ...it, row: Math.max(1, it.row - 1) };
            if (cand.row === it.row) break;
            if (cand.row + cand.rowSpan - 1 > rows) break;
            const tmp = layout.map(x => x.id === it.id ? cand : x);
            if (canPlace(tmp, cand)) {
                it.row = cand.row;
            } else {
                moved = false;
            }
        }
    }
    return layout;
}

function compactVertical(layout, rows, cols) {
    const ordered = [...layout].sort((a, b) => a.col - b.col || a.row - b.row);
    for (const it of ordered) {
        let best = it.row;
        for (let r = 1; r < it.row; r++) {
            const cand = { ...it, row: r };
            const tmp = layout.map(x => x.id === it.id ? cand : x);
            if (canPlace(tmp, cand)) best = r;
            else break;
        }
        it.row = best;
    }
    return layout;
}

function nearestFree(layout, moving, rows, cols) {
    const maxR = rows - moving.rowSpan + 1;
    const maxC = cols - moving.colSpan + 1;
    let best = null, bestDist = Infinity;
    for (let r = 1; r <= maxR; r++) {
        for (let c = 1; c <= maxC; c++) {
            const cand = { ...moving, row: r, col: c };
            const tmp = layout.map(x => x.id === moving.id ? cand : x);
            if (canPlace(tmp, cand)) {
                const d = Math.abs(r - moving.row) + Math.abs(c - moving.col);
                if (d < bestDist) {
                    best = cand;
                    bestDist = d;
                }
            }
        }
    }
    return best || moving;
}

const _bound = new WeakSet();
export const mudexGrid = {
    bind(gridEl, dotnet) {
        if (!gridEl._mudex) {
            gridEl._mudex = { dotnet };
        }
        if (!gridEl._mudex.observer) {
            const obs = new MutationObserver(() => {
                [...gridEl.querySelectorAll(".mud-ex-grid-section")].forEach(el => {
                    if (!_bound.has(el)) {
                        _bound.add(el);
                        mudexGrid._wireOne(gridEl, el);
                    }
                });
            });
            obs.observe(gridEl, { childList: true, subtree: true });
            gridEl._mudex.observer = obs;
        }
        [...gridEl.querySelectorAll(".mud-ex-grid-section")].forEach(el => {
            if (!_bound.has(el)) {
                _bound.add(el);
                mudexGrid._wireOne(gridEl, el);
            }
        });
    },
    _wireOne(gridEl, itemEl) {
        mudexGrid.wireDrag(gridEl, itemEl);
        const hE = itemEl.querySelector(".mud-ex-grid-handle-e");
        const hS = itemEl.querySelector(".mud-ex-grid-handle-s");
        const hSE = itemEl.querySelector(".mud-ex-grid-handle-se");
        mudexGrid.wireResize(gridEl, itemEl, [hE, hS, hSE]);
    },
    wireDrag(gridEl, itemEl) {
        const overlay = gridEl.querySelector(".mud-ex-grid-overlay");
        const placeholder = gridEl.querySelector(".mud-ex-grid-placeholder");
        let start = null, ctx = null, lastProposal = null;
        const onDown = e => {
            const opts = JSON.parse(itemEl.dataset.options);
            if (!opts.movable) return;
            if (e.button !== 0) return;
            itemEl.setPointerCapture(e.pointerId);
            start = { x: e.clientX, y: e.clientY };
            const base = buildModel(gridEl);
            const me = base.find(x => x.el === itemEl);
            const others = base.filter(x => x !== me);
            const m = measure(gridEl);
            overlay.textContent = "";
            const clone = itemEl.cloneNode(true);
            clone.classList.add("mud-ex-grid-clone");
            Object.assign(clone.style, { position: "absolute", pointerEvents: "none", zIndex: 30, left: 0, top: 0 });
            overlay.appendChild(clone);
            const mePx = { ...px(me, m) };
            Object.assign(clone.style,
                { width: `${mePx.w}px`, height: `${mePx.h}px`, transform: `translate(${mePx.x}px,${mePx.y}px)` });
            placeholder.style.display = "block";
            Object.assign(placeholder.style,
                { width: `${mePx.w}px`, height: `${mePx.h}px`, transform: `translate(${mePx.x}px,${mePx.y}px)` });
            ctx = { base, me, others, m, clone, placeholder };
        };
        const onMove = e => {
            if (!start || !ctx) return;
            const opts = JSON.parse(itemEl.dataset.options);
            const dx = e.clientX - start.x, dy = e.clientY - start.y;
            const stepX = Math.trunc((dx + Math.sign(dx) * 0.4 * ctx.m.cw) / ctx.m.cw);
            const stepY = Math.trunc((dy + Math.sign(dy) * 0.4 * ctx.m.ch) / ctx.m.ch);
            const dCol = opts.lockX ? 0 : stepX;
            const dRow = opts.lockY ? 0 : stepY;
            const rows = ctx.m.rows, cols = ctx.m.cols;
            const minc = Math.max(1, opts.minCol);
            const maxc = Math.min(cols - ctx.me.colSpan + 1, Number.isFinite(opts.maxCol) ? opts.maxCol : cols);
            const minr = Math.max(1, opts.minRow);
            const maxr = Math.min(rows - ctx.me.rowSpan + 1, Number.isFinite(opts.maxRow) ? opts.maxRow : rows);
            const target = {
                ...ctx.me,
                row: Math.max(minr, Math.min(maxr, ctx.me.row + dRow)),
                col: Math.max(minc, Math.min(maxc, ctx.me.col + dCol))
            };
            const optsMap = new Map([[ctx.me.id, opts]]);
            let proposal = [target, ...ctx.others.map(o => ({ ...o }))];
            const gopts = JSON.parse(gridEl.dataset.options);
            if (gopts.resolveCollisions) {
                proposal = trySwapOrPush(proposal, target, rows, cols, optsMap);
            } else {
                const tmp = proposal.map(x => x);
                if (!canPlace(tmp, target)) {
                    const nf = nearestFree(tmp, target, rows, cols);
                    proposal = [nf, ...ctx.others.map(o => ({ ...o }))];
                }
            }
            if (gopts.floatMode) {
                proposal = floatUp(proposal, rows, cols);
            }
            lastProposal = proposal;
            const tpx = px(target, ctx.m);
            ctx.clone.style.transform = `translate(${tpx.x}px,${tpx.y}px)`;
            ctx.placeholder.style.transform = `translate(${tpx.x}px,${tpx.y}px)`;
            for (const it of proposal) {
                const baseIt = ctx.base.find(x => x.id === it.id);
                if (!baseIt) continue;
                const src = px(baseIt, ctx.m), dst = px(it, ctx.m);
                it.el.style.transition = "transform 70ms ease";
                it.el.style.transform = `translate(${dst.x - src.x}px,${dst.y - src.y}px)`;
            }
        };
        const onUp = () => {
            if (!ctx) {
                start = null;
                return;
            }
            const gopts = JSON.parse(gridEl.dataset.options);
            let finalProposal = lastProposal || [];
            if (gopts.compactOnDrop) {
                finalProposal = compactVertical(finalProposal, ctx.m.rows, ctx.m.cols);
            }
            const changes = finalProposal.map(x => ({
                id: x.id,
                row: x.row,
                column: x.col,
                rowSpan: x.rowSpan,
                colSpan: x.colSpan
            }));
            const dotnet = gridEl._mudex?.dotnet;
            if (!dotnet) {
                cleanup();
                return;
            }
            dotnet.invokeMethodAsync("MudEx_CommitLayout", changes).then(() => cleanup());

            function cleanup() {
                clearTransforms(gridEl);
                ctx?.clone?.remove();
                if (ctx?.placeholder) {
                    ctx.placeholder.style.display = "none";
                }
                start = null;
                ctx = null;
                lastProposal = null;
            }
        };
        itemEl.addEventListener("pointerdown", onDown);
        itemEl.addEventListener("pointermove", onMove);
        itemEl.addEventListener("pointerup", onUp);
        itemEl.addEventListener("pointercancel", onUp);
    },
    wireResize(gridEl, itemEl, handles) {
        const overlay = gridEl.querySelector(".mud-ex-grid-overlay");
        const placeholder = gridEl.querySelector(".mud-ex-grid-placeholder");
        const [hE, hS, hSE] = handles || [];
        const attach = (handle, mode) => {
            if (!handle) return;
            let start = null, ctx = null, lastProposal = null;
            const onDown = e => {
                const opts = JSON.parse(itemEl.dataset.options);
                if (!opts.resizable) return;
                e.stopPropagation();
                handle.setPointerCapture(e.pointerId);
                start = { x: e.clientX, y: e.clientY };
                const base = buildModel(gridEl);
                const me = base.find(x => x.el === itemEl);
                const others = base.filter(x => x !== me);
                const m = measure(gridEl);
                overlay.textContent = "";
                const clone = itemEl.cloneNode(true);
                clone.classList.add("mud-ex-grid-clone");
                Object.assign(clone.style,
                    { position: "absolute", pointerEvents: "none", zIndex: 30, left: 0, top: 0 });
                overlay.appendChild(clone);
                const mePx = px(me, m);
                Object.assign(clone.style,
                    { width: `${mePx.w}px`, height: `${mePx.h}px`, transform: `translate(${mePx.x}px,${mePx.y}px)` });
                placeholder.style.display = "block";
                Object.assign(placeholder.style,
                    { width: `${mePx.w}px`, height: `${mePx.h}px`, transform: `translate(${mePx.x}px,${mePx.y}px)` });
                ctx = { base, me, others, m, clone, placeholder };
            };
            const onMove = e => {
                if (!start || !ctx) return;
                const opts = JSON.parse(itemEl.dataset.options);
                const dx = e.clientX - start.x, dy = e.clientY - start.y;
                const stepCols = (mode === "s") ? 0 : Math.trunc((dx + Math.sign(dx) * 0.4 * ctx.m.cw) / ctx.m.cw);
                const stepRows = (mode === "e") ? 0 : Math.trunc((dy + Math.sign(dy) * 0.4 * ctx.m.ch) / ctx.m.ch);
                const rows = ctx.m.rows, cols = ctx.m.cols;
                let newW = ctx.me.colSpan + stepCols;
                let newH = ctx.me.rowSpan + stepRows;
                newW = Math.max(opts.minColSpan, Math.min(opts.maxColSpan, newW));
                newH = Math.max(opts.minRowSpan, Math.min(opts.maxRowSpan, newH));
                newW = Math.min(newW, cols - ctx.me.col + 1);
                newH = Math.min(newH, rows - ctx.me.row + 1);
                const target = { ...ctx.me, colSpan: newW, rowSpan: newH };
                const optsMap = new Map([[ctx.me.id, opts]]);
                let proposal = [target, ...ctx.others.map(o => ({ ...o }))];
                const gopts = JSON.parse(gridEl.dataset.options);
                if (gopts.resolveCollisions) {
                    proposal = trySwapOrPush(proposal, target, rows, cols, optsMap);
                } else {
                    const tmp = proposal.map(x => x);
                    if (!canPlace(tmp, target)) {
                        const nf = nearestFree(tmp, target, rows, cols);
                        proposal = [nf, ...ctx.others.map(o => ({ ...o }))];
                    }
                }
                if (gopts.floatMode) {
                    proposal = floatUp(proposal, rows, cols);
                }
                lastProposal = proposal;
                const tpx = px(target, ctx.m);
                ctx.clone.style.width = `${tpx.w}px`;
                ctx.clone.style.height = `${tpx.h}px`;
                placeholder.style.width = `${tpx.w}px`;
                placeholder.style.height = `${tpx.h}px`;
                for (const it of proposal) {
                    const baseIt = ctx.base.find(x => x.id === it.id);
                    if (!baseIt) continue;
                    const src = px(baseIt, ctx.m), dst = px(it, ctx.m);
                    it.el.style.transition = "transform 70ms ease";
                    it.el.style.transform = `translate(${dst.x - src.x}px,${dst.y - src.y}px)`;
                }
            };
            const onUp = () => {
                if (!ctx) {
                    start = null;
                    return;
                }
                const gopts = JSON.parse(gridEl.dataset.options);
                let finalProposal = lastProposal || [];
                if (gopts.compactOnDrop) {
                    finalProposal = compactVertical(finalProposal, ctx.m.rows, ctx.m.cols);
                }
                const changes = finalProposal.map(x => ({
                    id: x.id,
                    row: x.row,
                    column: x.col,
                    rowSpan: x.rowSpan,
                    colSpan: x.colSpan
                }));
                const dotnet = gridEl._mudex?.dotnet;
                if (!dotnet) {
                    cleanup();
                    return;
                }
                dotnet.invokeMethodAsync("MudEx_CommitLayout", changes).then(() => cleanup());

                function cleanup() {
                    clearTransforms(gridEl);
                    ctx?.clone?.remove();
                    ctx.placeholder.style.display = "none";
                    start = null;
                    ctx = null;
                    lastProposal = null;
                }
            };
            if (handle && typeof handle.addEventListener === 'function') {
                handle.addEventListener("pointerdown", onDown);
                handle.addEventListener("pointermove", onMove);
                handle.addEventListener("pointerup", onUp);
                handle.addEventListener("pointercancel", onUp);
            }
        };
        attach(hE, "e");
        attach(hS, "s");
        attach(hSE, "se");
    }
};