class MudExFileDisplayDocument {
    constructor(elementRef, dotNet, containerId) {
        this.elementRef = elementRef;
        this.dotnet = dotNet;
        this.containerId = containerId;
    }

    renderDocx(fileBytes) {
        var self = this;
        try {
            var container = document.getElementById(this.containerId);
            if (!container) {
                this.dotnet.invokeMethodAsync('OnError', 'Container element not found');
                return;
            }

            container.innerHTML = '';

            var arrayBuffer = new Uint8Array(fileBytes).buffer;

            var options = {
                inWrapper: true,
                breakPages: true,
                useBase64URL: true,
                renderHeaders: true,
                renderFooters: true,
                renderFootnotes: true,
                renderEndnotes: true
            };

            window.docx.renderAsync(arrayBuffer, container, null, options)
                .then(function () {
                    self.dotnet.invokeMethodAsync('OnDocumentRendered');
                })
                .catch(function (error) {
                    console.error('docx-preview error:', error);
                    self.dotnet.invokeMethodAsync('OnError', error.message || 'Failed to render DOCX');
                });
        } catch (e) {
            console.error('Failed to render DOCX:', e);
            this.dotnet.invokeMethodAsync('OnError', e.message || 'Failed to render DOCX');
        }
    }

    renderHtml(htmlString) {
        var self = this;
        try {
            var container = document.getElementById(this.containerId);
            if (!container) {
                this.dotnet.invokeMethodAsync('OnError', 'Container element not found');
                return;
            }

            container.innerHTML = '';

            var iframe = document.createElement('iframe');
            iframe.style.width = '100%';
            iframe.style.height = '100%';
            iframe.style.border = 'none';
            iframe.setAttribute('sandbox', 'allow-same-origin');
            container.appendChild(iframe);

            var doc = iframe.contentDocument || iframe.contentWindow.document;
            doc.open();
            doc.write('<!DOCTYPE html><html><head><meta charset="utf-8"><style>body{margin:0;padding:16px;font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,sans-serif;}</style></head><body>' + htmlString + '</body></html>');
            doc.close();

            self.dotnet.invokeMethodAsync('OnDocumentRendered');
        } catch (e) {
            console.error('Failed to render HTML:', e);
            this.dotnet.invokeMethodAsync('OnError', e.message || 'Failed to render HTML content');
        }
    }

    dispose() {
        var container = document.getElementById(this.containerId);
        if (container) {
            container.innerHTML = '';
        }
    }
}

window.MudExFileDisplayDocument = MudExFileDisplayDocument;

export function initializeMudExFileDisplayDocument(elementRef, dotnet, containerId) {
    return new MudExFileDisplayDocument(elementRef, dotnet, containerId);
}
