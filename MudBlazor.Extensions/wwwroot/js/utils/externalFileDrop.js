/**
 * External file drops for any component that renders drop targets.
 *
 * Blazor's DragEventArgs only carries file names, never the files, so the only way to turn a drop from the
 * operating system into an IBrowserFile is to put the dropped files on a real file input and dispatch its
 * change event. That is what this does, and it is the same trick MudExUploadEdit uses.
 *
 * The listeners sit on the document in the capture phase, so they run before the target's own Blazor drop
 * handler and can claim the event. Targets are found by their data attributes, so no element reference per
 * target is needed - which is what lets one zone serve any number of them.
 */
window.MudExExternalFileDrop = {
    attach: function (zoneId, inputElement, dotnet) {
        const selector = '[data-mudex-drop-zone="' + zoneId + '"][data-mudex-drop-target]';

        function targetKey(e) {
            const el = e.target instanceof Element ? e.target.closest(selector) : null;
            return el ? el.getAttribute('data-mudex-drop-target') : null;
        }

        function hasFiles(e) {
            return !!e.dataTransfer && Array.from(e.dataTransfer.types || []).indexOf('Files') >= 0;
        }

        function onDragOver(e) {
            if (!hasFiles(e) || targetKey(e) === null) {
                return;
            }
            e.preventDefault();
        }

        async function onDrop(e) {
            if (!hasFiles(e)) {
                return;
            }
            const key = targetKey(e);
            if (key === null) {
                return;
            }

            e.preventDefault();
            e.stopPropagation();

            // Tell .NET which target was hit before the change event fires, otherwise the files arrive
            // without one.
            await dotnet.invokeMethodAsync('SetExternalDropTargetKey', key);

            const container = new DataTransfer();
            Array.from(e.dataTransfer.files).forEach(f => container.items.add(f));
            inputElement.files = container.files;
            inputElement.dispatchEvent(new Event('change', { bubbles: true }));
        }

        document.addEventListener('dragover', onDragOver, true);
        document.addEventListener('drop', onDrop, true);

        return {
            dispose: () => {
                document.removeEventListener('dragover', onDragOver, true);
                document.removeEventListener('drop', onDrop, true);
            }
        };
    }
};
