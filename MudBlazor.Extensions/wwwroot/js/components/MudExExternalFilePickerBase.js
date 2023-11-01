class MudExExternalFilePickerBase {
    elementRef;
    dotnet;
    options;
    accessToken = null;
    pickerInited = false;
    externalJsFiles = []; 

    constructor(elementRef, dotNet, options) {
        this.elementRef = elementRef;
        this.dotnet = dotNet;
        if (options.externalJsFiles) {
            this.externalJsFiles = options.externalJsFiles;
        }
        this.setOptions(options);
    }

    setOptions(options) {
        this.options = { ...options, ...options.jsOptions || {} };
        delete this.options.jsOptions;
        if (this.options.clientId || this.options.apiKey) {
            this.loadApi();
        }
    }


    async loadApi() {
        await this.loadExternalJsFiles();
        this.apiLoaded();
    }

    loadExternalJsFiles() {
        const promises = this.externalJsFiles.map(jsFile => {
            return this.loadJsFileIfNotExists(jsFile);
        });
        return Promise.all(promises);
    }

    loadJsFileIfNotExists(jsFile) {
        return new Promise((resolve, reject) => {
            if (!document.querySelector(`script[src="${jsFile}"]`)) {
                const script = document.createElement('script');
                script.src = jsFile;
                script.onload = resolve;
                script.onerror = reject;
                document.head.appendChild(script);
            } else {
                resolve();  // Datei ist bereits geladen
            }
        });
    }

    apiLoaded() {
        this.pickerInited = true;
        this.dotnet.invokeMethodAsync(this.options.onReadyCallback);
    }

    openPicker() {
        // Implementierung in abgeleiteten Klassen
    }

    dispose() {
        // Cleanup if necessary
    }
}

window.MudExExternalFilePickerBase = MudExExternalFilePickerBase;
