/**
 * Rubber band selection and drag auto scrolling for MudExFileGrid.
 *
 * Pointer moves happen per frame, so drawing the rectangle, hit testing and the scrolling stay in the
 * browser - going through interop for every move would make the drag stutter. .NET only hears the result:
 * the keys of the entries the rectangle touched, once, when the drag ends.
 */
window.MudExFileGridSelection = {
    attach: function (container, dotnet, rubberBand) {
        if (!container) {
            return null;
        }

        const EDGE = 48;
        const SPEED = 18;
        let autoY = null;
        let frame = null;

        function scroller() {
            return container.querySelector('.mud-ex-file-grid-scroll') || container;
        }

        function step() {
            frame = null;
            if (autoY === null) {
                return;
            }

            const element = scroller();
            const box = element.getBoundingClientRect();
            const fromTop = autoY - box.top;
            const fromBottom = box.bottom - autoY;

            // The closer to the edge, the faster - the same feel as a file explorer.
            if (fromTop < EDGE) {
                element.scrollTop -= SPEED * (1 - Math.max(fromTop, 0) / EDGE);
            } else if (fromBottom < EDGE) {
                element.scrollTop += SPEED * (1 - Math.max(fromBottom, 0) / EDGE);
            }

            frame = requestAnimationFrame(step);
        }

        function autoScroll(y) {
            autoY = y;
            if (frame === null) {
                frame = requestAnimationFrame(step);
            }
        }

        function stopAutoScroll() {
            autoY = null;
            if (frame !== null) {
                cancelAnimationFrame(frame);
                frame = null;
            }
        }

        function onDragOver(e) {
            autoScroll(e.clientY);
        }

        function onDragLeave(e) {
            // Fires between children too, so only a target outside the grid ends the scrolling.
            if (!container.contains(e.relatedTarget)) {
                stopAutoScroll();
            }
        }

        container.addEventListener('dragover', onDragOver);
        container.addEventListener('dragleave', onDragLeave);
        document.addEventListener('dragend', stopAutoScroll);
        document.addEventListener('drop', stopAutoScroll);

        let band = null;
        let startX = 0;
        let startY = 0;
        let additive = false;

        function itemRects() {
            return Array.from(container.querySelectorAll('[data-file-grid-key]')).map(el => ({
                key: el.getAttribute('data-file-grid-key'),
                rect: el.getBoundingClientRect()
            }));
        }

        function intersects(a, b) {
            return a.left < b.right && a.right > b.left && a.top < b.bottom && a.bottom > b.top;
        }

        function bandRect() {
            const box = band.getBoundingClientRect();
            return { left: box.left, top: box.top, right: box.right, bottom: box.bottom };
        }

        function onPointerDown(e) {
            // Only a drag that starts on the free space is a rubber band - one that starts on an entry is a
            // drag of that entry.
            if (e.button !== 0 || (e.target instanceof Element && e.target.closest('[data-file-grid-key]'))) {
                return;
            }

            additive = e.ctrlKey || e.metaKey || e.shiftKey;
            startX = e.clientX;
            startY = e.clientY;

            band = document.createElement('div');
            band.className = 'mud-ex-file-grid-band';
            band.style.position = 'fixed';
            band.style.left = startX + 'px';
            band.style.top = startY + 'px';
            band.style.width = '0';
            band.style.height = '0';
            band.style.pointerEvents = 'none';
            band.style.zIndex = '10';
            document.body.appendChild(band);

            container.setPointerCapture?.(e.pointerId);
            window.addEventListener('pointermove', onPointerMove);
            window.addEventListener('pointerup', onPointerUp);
        }

        function onPointerMove(e) {
            if (!band) {
                return;
            }
            autoScroll(e.clientY);
            band.style.left = Math.min(startX, e.clientX) + 'px';
            band.style.top = Math.min(startY, e.clientY) + 'px';
            band.style.width = Math.abs(e.clientX - startX) + 'px';
            band.style.height = Math.abs(e.clientY - startY) + 'px';
        }

        async function onPointerUp() {
            window.removeEventListener('pointermove', onPointerMove);
            window.removeEventListener('pointerup', onPointerUp);
            stopAutoScroll();
            if (!band) {
                return;
            }

            const box = bandRect();
            const touched = itemRects().filter(i => intersects(box, i.rect)).map(i => i.key);

            band.remove();
            band = null;

            // A click without movement is not a band - let the normal click handling deal with it.
            const moved = box.right - box.left > 3 || box.bottom - box.top > 3;
            if (moved) {
                await dotnet.invokeMethodAsync('RubberBandSelected', touched, additive);
            }
        }

        if (rubberBand) {
            container.addEventListener('pointerdown', onPointerDown);
        }

        return {
            dispose: () => {
                container.removeEventListener('pointerdown', onPointerDown);
                container.removeEventListener('dragover', onDragOver);
                container.removeEventListener('dragleave', onDragLeave);
                document.removeEventListener('dragend', stopAutoScroll);
                document.removeEventListener('drop', stopAutoScroll);
                window.removeEventListener('pointermove', onPointerMove);
                window.removeEventListener('pointerup', onPointerUp);
                stopAutoScroll();
                band?.remove();
            }
        };
    }
};
