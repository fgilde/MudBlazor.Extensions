class MudExDropBoxFilePicker {
    elementRef;
    dotnet;
    options;

    constructor(elementRef, dotNet, options) {
        this.elementRef = elementRef;
        this.dotnet = dotNet;
        this.setOptions(options);
    }

    setOptions(options) {
        this.options = options;
        if (this.options.apiKey) {
            this.loadDropboxApi();
        }
    }

    loadDropboxApi() {
        const existingScript = document.getElementById('dropboxjs');
        if (existingScript) {
            existingScript.setAttribute('data-app-key', this.options.apiKey);
            this.apiLoaded(); 
        } else {
            // Load the Dropbox script if it's not already present
            const dropboxScript = document.createElement('script');
            dropboxScript.async = true;
            dropboxScript.defer = true;
            dropboxScript.src = 'https://www.dropbox.com/static/api/2/dropins.js';
            dropboxScript.id = 'dropboxjs';
            dropboxScript.setAttribute('data-app-key', this.options.apiKey);
            dropboxScript.onload = this.apiLoaded.bind(this);
            document.body.appendChild(dropboxScript);
        }
    }


    apiLoaded() {
        this.dotnet.invokeMethodAsync('OnReady');
    }

    showPicker() {
        Dropbox.choose({
            success: (files) => {
                const fileDataArray = files.map(file => ({
                    url: file.link,
                    fileName: file.name,
                    size: file.bytes,
                    isDir: file.isDir,
                    icon: file.icon
                }));
                this.dotnet.invokeMethodAsync('OnFilesSelected', fileDataArray);
            },
            cancel: () => {
                this.dotnet.invokeMethodAsync('OnFilesSelected', []);
            },
            //linkType: this.options.allowFolderSelect ? "preview" : "direct", // direct or preview
            linkType: 'direct',
            multiselect: this.options.multiSelect || false,
            sizeLimit: this.options.maxFileSize || undefined,
            extensions: this.options.allowedExtensions || [],
            folderselect: false
        });
    }

    dispose() {
        // Cleanup if necessary
    }
}

window.MudExDropBoxFilePicker = MudExDropBoxFilePicker;

export function initializeMudExDropBoxFilePicker(elementRef, dotnet, options) {
    return new MudExDropBoxFilePicker(elementRef, dotnet, options);
}
