function clsNum(gridEl, prefix) {
    const c = [...gridEl.classList].find(x => x.startsWith(prefix));
    return c ? parseInt(c.split('-').pop()) : null;
}


function readItemFromClasses(el) {
    const get = (p) => { const c = [...el.classList].find(x => x.startsWith(`mud-ex-grid-section-${p}-`)); return c ? parseInt(c.split('-').pop()) : 1; };
    const rs = get('row-start'), re = get('row-end'), cs = get('col-start'), ce = get('col-end');
    return { row: rs, col: cs, rowSpan: re - rs, colSpan: ce - cs };
}


function rectsOverlap(a, b) {
    return !(a.row + a.rowSpan - 1 < b.row || b.row + b.rowSpan - 1 < a.row || a.col + a.colSpan - 1 < b.col || b.col + b.colSpan - 1 < a.col);
}


function buildModel(gridEl) {
    return [...gridEl.querySelectorAll('.mud-ex-grid-section')].map(el => ({ id: el.dataset.id, el, ...readItemFromClasses(el) }));
}


function measure(gridEl) {
    const r = gridEl.getBoundingClientRect();
    const rows = clsNum(gridEl, 'mud-ex-grid-row-') || 12;
    const cols = clsNum(gridEl, 'mud-ex-grid-column-') || 12;
    return { rows, cols, cw: r.width / cols, ch: r.height / rows, left: r.left, top: r.top };
}


function px(item, m) {
    return { x: (item.col - 1) * m.cw, y: (item.row - 1) * m.ch, w: item.colSpan * m.cw, h: item.rowSpan * m.ch };
}


function canPlace(layout, itm, rows, cols) {
    if (itm.row < 1 || itm.col < 1 || itm.row + itm.rowSpan - 1 > rows || itm.col + itm.colSpan - 1 > cols) return false;
    return !layout.some(x => x.id !== itm.id && rectsOverlap(itm, x));
}


function bfsPush(layout, moving, rows, cols) {
    // Pusht **nur**, wenn moving tatsächlich kollidiert. Freie Plätze bleiben frei.
    if (canPlace(layout, moving, rows, cols)) return layout;
    const q = [moving]; const seen = new Set([moving.id]);
    while (q.length) {
        const cur = q.shift();
        for (const other of layout) {
            if (other.id === cur.id) continue;
            if (rectsOverlap(cur, other)) {
                // Erst nach unten, dann rechts, mit begrenzter Suche – minimiert "Wegfliegen"
                const opts = [{ row: cur.row + cur.rowSpan, col: other.col }, { row: other.row, col: cur.col + cur.colSpan }];
                let placed = false;
                for (const t of opts) {
                    let r = Math.max(1, t.row), c = Math.max(1, t.col);
                    while (r <= rows && !placed) {
                        while (c <= cols) {
                            const cand = { ...other, row: r, col: c };
                            if (canPlace(layout.map(x => x.id === other.id ? cand : x), cand, rows, cols)) { other.row = cand.row; other.col = cand.col; placed = true; break; }
                            c++;
                        }
                        if (!placed) { c = 1; r++; }
                    }
                }
                if (!seen.has(other.id)) { q.push(other); seen.add(other.id); }
            }
        }
    }
    return layout;
}

function trySwapOrPush(layout, moving, rows, cols) {
    // 1) frei?
    if (canPlace(layout, moving, rows, cols)) return layout;

    // Items, die moving überlappt
    const overlappers = layout.filter(x => x.id !== moving.id && rectsOverlap(moving, x));

    // 2) Swap-Kandidat (nur einer, passt in alte Position des Movers?)
    if (overlappers.length === 1) {
        const target = overlappers[0];
        const moverOld = layout.find(x => x.id === moving.id);
        const candidateInOld = { ...target, row: moverOld.row, col: moverOld.col };
        if (canPlace(layout.map(x => x.id === target.id ? candidateInOld : x), candidateInOld, rows, cols)) {
            // Swap
            target.row = moverOld.row; target.col = moverOld.col;
            return layout;
        }
    }

    // 3) Push-Chain BFS
    return bfsPush(layout, moving, rows, cols);
}

function clearTransforms(gridEl) { gridEl.querySelectorAll('.mud-ex-grid-section').forEach(el => { el.style.transform = ''; el.style.transition = ''; }); }


const _bound = new WeakSet();

export const mudexGrid = {
    bind(gridEl, dotnet) {
        if (!gridEl._mudex) { gridEl._mudex = { dotnet }; }
        if (!gridEl._mudex.observer) {
            const obs = new MutationObserver(() => {
                [...gridEl.querySelectorAll('.mud-ex-grid-section')].forEach(el => {
                    if (!_bound.has(el)) { _bound.add(el); mudexGrid._wireOne(gridEl, el); }
                });
            });
            obs.observe(gridEl, { childList: true, subtree: true });
            gridEl._mudex.observer = obs;
        }
        [...gridEl.querySelectorAll('.mud-ex-grid-section')].forEach(el => { if (!_bound.has(el)) { _bound.add(el); mudexGrid._wireOne(gridEl, el); } });
    },

    _wireOne(gridEl, itemEl) {
        // Drag
        mudexGrid.wireDrag(gridEl, itemEl);
        // Resize-Handles suchen
        const hE = itemEl.querySelector('.mud-ex-grid-handle-e');
        const hS = itemEl.querySelector('.mud-ex-grid-handle-s');
        const hSE = itemEl.querySelector('.mud-ex-grid-handle-se');
        mudexGrid.wireResize(gridEl, itemEl, [hE, hS, hSE]);
    },


    wireDrag(gridEl, itemEl) {
        const overlay = gridEl.querySelector('.mud-ex-grid-overlay');
        const placeholder = gridEl.querySelector('.mud-ex-grid-placeholder');
        let start = null, ctx = null, lastProposal = null;


        const onDown = (e) => {
            if (e.button !== 0) return; itemEl.setPointerCapture(e.pointerId); start = { x: e.clientX, y: e.clientY };
            const base = buildModel(gridEl); const me = base.find(x => x.el === itemEl); const others = base.filter(x => x !== me);
            const m = measure(gridEl);
            overlay.textContent = '';
            const clone = itemEl.cloneNode(true); clone.classList.add('mud-ex-grid-clone'); Object.assign(clone.style, { position: 'absolute', pointerEvents: 'none', zIndex: 30, left: 0, top: 0 }); overlay.appendChild(clone);
            const mePx = { ...px(me, m) }; Object.assign(clone.style, { width: `${mePx.w}px`, height: `${mePx.h}px`, transform: `translate(${mePx.x}px,${mePx.y}px)` });
            placeholder.style.display = 'block'; Object.assign(placeholder.style, { width: `${mePx.w}px`, height: `${mePx.h}px`, transform: `translate(${mePx.x}px,${mePx.y}px)` });
            ctx = { base, me, others, m, clone, placeholder };
        };

        const onMove = (e) => {
            if (!start || !ctx) return;
            const dx = e.clientX - start.x, dy = e.clientY - start.y;
            const snapX = Math.trunc((dx + Math.sign(dx) * 0.4 * ctx.m.cw) / ctx.m.cw);
            const snapY = Math.trunc((dy + Math.sign(dy) * 0.4 * ctx.m.ch) / ctx.m.ch);
            const rows = ctx.m.rows, cols = ctx.m.cols;
            const target = { ...ctx.me, row: Math.min(rows - ctx.me.rowSpan + 1, Math.max(1, ctx.me.row + snapY)), col: Math.min(cols - ctx.me.colSpan + 1, Math.max(1, ctx.me.col + snapX)) };
            let proposal = [target, ...ctx.others.map(o => ({ ...o }))];
            // **Nur schieben, wenn wirklich überlappt**
            proposal = bfsPush(proposal, target, rows, cols);
            lastProposal = proposal;
            const tpx = px(target, ctx.m); ctx.clone.style.transform = `translate(${tpx.x}px,${tpx.y}px)`; ctx.placeholder.style.transform = `translate(${tpx.x}px,${tpx.y}px)`;
            // Vorschau nur via transform
            for (const it of proposal) {
                const baseIt = ctx.base.find(x => x.id === it.id); if (!baseIt) continue;
                const src = px(baseIt, ctx.m), dst = px(it, ctx.m); const tx = dst.x - src.x, ty = dst.y - src.y;
                it.el.style.transition = 'transform 70ms ease'; it.el.style.transform = `translate(${tx}px,${ty}px)`;
            }
        };


        const onUp = () => {
            if (!ctx) { start = null; return; }
            const changes = (lastProposal || []).map(x => ({ id: x.id, row: x.row, column: x.col, rowSpan: x.rowSpan, colSpan: x.colSpan }));
            const dotnet = gridEl._mudex?.dotnet; if (!dotnet) { cleanup(); return; }

            dotnet.invokeMethodAsync('MudEx_CommitLayout', changes).then(() => cleanup());


            function cleanup() {
                clearTransforms(gridEl); ctx?.clone?.remove(); ctx.placeholder.style.display = 'none'; start = null; ctx = null; lastProposal = null;
            }
        };


        itemEl.addEventListener('pointerdown', onDown);
        itemEl.addEventListener('pointermove', onMove);
        itemEl.addEventListener('pointerup', onUp);
        itemEl.addEventListener('pointercancel', onUp);
    },


    wireResize(gridEl, itemEl, handles) {
        const overlay = gridEl.querySelector('.mud-ex-grid-overlay');
        const placeholder = gridEl.querySelector('.mud-ex-grid-placeholder');
        const [hE, hS, hSE] = handles || [];


        const attach = (handle, mode) => {
            if (!handle) return; let start = null, ctx = null, lastProposal = null;


            const onDown = (e) => {
                e.stopPropagation(); handle.setPointerCapture(e.pointerId); start = { x: e.clientX, y: e.clientY };
                const base = buildModel(gridEl); const me = base.find(x => x.el === itemEl); const others = base.filter(x => x !== me); const m = measure(gridEl);
                overlay.textContent = '';
                const clone = itemEl.cloneNode(true); clone.classList.add('mud-ex-grid-clone'); Object.assign(clone.style, { position: 'absolute', pointerEvents: 'none', zIndex: 30, left: 0, top: 0 }); overlay.appendChild(clone);
                const mePx = px(me, m); Object.assign(clone.style, { width: `${mePx.w}px`, height: `${mePx.h}px`, transform: `translate(${mePx.x}px,${mePx.y}px)` });
                placeholder.style.display = 'block'; Object.assign(placeholder.style, { width: `${mePx.w}px`, height: `${mePx.h}px`, transform: `translate(${mePx.x}px,${mePx.y}px)` });
                ctx = { base, me, others, m, clone, placeholder };
            };
            const onMove = (e) => {
                if (!start || !ctx) return; const dx = e.clientX - start.x, dy = e.clientY - start.y;
                const stepCols = (mode === 's') ? 0 : Math.trunc((dx + Math.sign(dx) * 0.4 * ctx.m.cw) / ctx.m.cw);
                const stepRows = (mode === 'e') ? 0 : Math.trunc((dy + Math.sign(dy) * 0.4 * ctx.m.ch) / ctx.m.ch);
                const rows = ctx.m.rows, cols = ctx.m.cols;
                const target = { ...ctx.me, rowSpan: Math.max(1, Math.min(rows - ctx.me.row + 1, ctx.me.rowSpan + stepRows)), colSpan: Math.max(1, Math.min(cols - ctx.me.col + 1, ctx.me.colSpan + stepCols)) };
                let proposal = [target, ...ctx.others.map(o => ({ ...o }))];
                proposal = bfsPush(proposal, target, rows, cols); lastProposal = proposal;
                const tpx = px({ ...ctx.me, rowSpan: target.rowSpan, colSpan: target.colSpan }, ctx.m);
                ctx.clone.style.width = `${tpx.w}px`; ctx.clone.style.height = `${tpx.h}px`;
                placeholder.style.width = `${tpx.w}px`; placeholder.style.height = `${tpx.h}px`;
                for (const it of proposal) {
                    const baseIt = ctx.base.find(x => x.id === it.id); if (!baseIt) continue;
                    const src = px(baseIt, ctx.m), dst = px(it, ctx.m); const tx = dst.x - src.x, ty = dst.y - src.y;
                    it.el.style.transition = 'transform 70ms ease'; it.el.style.transform = `translate(${tx}px,${ty}px)`;
                }
            };


            const onUp = () => {
                if (!ctx) { start = null; return; }
                const changes = (lastProposal || []).map(x => ({ id: x.id, row: x.row, column: x.col, rowSpan: x.rowSpan, colSpan: x.colSpan }));
                const dotnet = gridEl._mudex?.dotnet; if (!dotnet) { cleanup(); return; }
                dotnet.invokeMethodAsync('MudEx_CommitLayout', changes).then(() => cleanup());


                function cleanup() { clearTransforms(gridEl); ctx.clone?.remove(); ctx.placeholder.style.display = 'none'; start = null; ctx = null; lastProposal = null; }
            };


            handle.addEventListener('pointerdown', onDown);
            handle.addEventListener('pointermove', onMove);
            handle.addEventListener('pointerup', onUp);
            handle.addEventListener('pointercancel', onUp);
        };


        attach(hE, 'e'); attach(hS, 's'); attach(hSE, 'se');
    }
};