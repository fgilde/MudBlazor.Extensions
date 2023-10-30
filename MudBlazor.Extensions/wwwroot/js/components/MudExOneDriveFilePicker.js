class MudExOneDriveFilePicker {
    elementRef;
    dotnet;
    options;
    accessToken = null;
    pickerInited = false;

    constructor(elementRef, dotNet, options) {
        this.elementRef = elementRef;
        this.dotnet = dotNet;
        this.setOptions(options);
    }

    setOptions(options) {
        this.options = options;
        if (this.options.clientId) {
            this.loadOneDriveApi(); 
        }
    }

    loadOneDriveApi() {
        const existingScript = document.getElementById('onedrivejs');
        if (existingScript) {
            this.apiLoaded();
        } else {
            const oneDriveScript = document.createElement('script');
            oneDriveScript.async = true;
            oneDriveScript.defer = true;
            oneDriveScript.id = 'onedrivejs';
            oneDriveScript.src = 'https://js.live.net/v7.2/OneDrive.js';
            oneDriveScript.onload = this.apiLoaded.bind(this);
            document.body.appendChild(oneDriveScript);
        }
    }

    apiLoaded() {
        this.pickerInited = true;
        this.dotnet.invokeMethodAsync('OnReady');
    }

    openPicker() {
        const odOptions = {
            clientId: this.options.clientId,
            action: "query",
            multiSelect: this.options.multiSelect || false,
            advanced: {
                redirectUri: this.options.redirectUri || window.location.origin,
                accessToken: this.accessToken || undefined
            },
            success: (files) => {
                const fileDataArray = files.value.map(file => {
                    const extension = file.name.split('.').pop();

                    return {
                        id: file.id,
                        accessToken: this.accessToken,
                        fileName: file.name,
                        extension: extension,
                        contentType: file.file ? file.file.mimeType : null,
                        webViewLink: file.webUrl,
                        webContentLink: null, // This might not be directly available
                        data: null, // Byte data, requires additional handling to fetch file content
                        size: file.size
                    };
                });
                this.dotnet.invokeMethodAsync('OnFilesSelected', fileDataArray);
            },
            cancel: () => {
                this.dotnet.invokeMethodAsync('OnFilesSelected', []);
            },
            error: (error) => {
                console.error(error);
                // Handle the error
            }
        };

        OneDrive.open(odOptions);
    }

    // Additional methods for handling authentication, file data loading, etc., may be needed

    dispose() {
        // Cleanup if necessary
    }
}

window.MudExOneDriveFilePicker = MudExOneDriveFilePicker;

export function initializeMudExOneDriveFilePicker(elementRef, dotnet, options) {
    return new MudExOneDriveFilePicker(elementRef, dotnet, options);
}
