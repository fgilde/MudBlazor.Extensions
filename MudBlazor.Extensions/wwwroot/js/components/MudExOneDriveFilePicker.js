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
        this.options = { ...options, ...options.jsOptions || {} };
        delete this.options.jsOptions;
        if (this.options.clientId) {
            this.loadApi();
        }
    }

    loadApi() {
        const existingScript = document.getElementById('onedrivejs');
        if (existingScript) {
            this.apiLoaded();
        } else {
            const oneDriveScript = document.createElement('script');
            oneDriveScript.module = true;
            oneDriveScript.id = 'onedrivejs';
            oneDriveScript.src = 'https://js.live.net/v7.2/OneDrive.js';
            oneDriveScript.onload = this.apiLoaded.bind(this);
            document.body.appendChild(oneDriveScript);
        }
    }

    apiLoaded() {
        this.pickerInited = true;
        this.dotnet.invokeMethodAsync(this.options.onReadyCallback);
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
                this.accessToken = files.accessToken;
                this.apiEndpoint = files.apiEndpoint;
                this.processResult(files);
            },
            cancel: () => {
                this.dotnet.invokeMethodAsync(this.options.onFilesSelectedCallback, []);
            },
            error: (error) => {
                console.error(error);
                // Handle the error
            }
        };

        OneDrive.open(odOptions);
    }

    async processResult(files) {
        const fileDataArray = [];

        for (const file of files.value) {
            if (file.folder) {
                // If it's a folder, get all files within it
                const folderFiles = await this.getAllFilesInFolder(file.id);
                fileDataArray.push(...folderFiles);
            } else {
                // If it's a file, process it
                const fileInfo = await this.processFile(file);
                fileDataArray.push(fileInfo);
            }
        }

        this.dotnet.invokeMethodAsync(this.options.onFilesSelectedCallback, fileDataArray);
    }

    async getFileDetails(fileId) {
        const response = await fetch(`${this.apiEndpoint}me/drive/items/${fileId}`, {
            headers: new Headers({ Authorization: `Bearer ${this.accessToken}` })
        });
        return response.json();
    }

    async processFile(file) {
        const fileId = file.id;

        const fileDetails = await this.getFileDetails(fileId);

        const fileName = fileDetails.name;
        const extension = fileName.split('.').pop();

        let fileInfo = {
            id: fileId,
            size: fileDetails.size,
            accessToken: this.accessToken,
            fileName: fileName,
            extension: extension,
            contentType: fileDetails.file.mimeType,
            webViewLink: fileDetails.webUrl,
            webContentLink: null, // This might not be directly available
            path: '' // Path might not be directly available
        };

        if (this.options.autoLoadFileDataBytes) {
            const blobResponse = await fetch(fileDetails["@microsoft.graph.downloadUrl"], {
                headers: new Headers({ Authorization: `Bearer ${this.accessToken}` })
            });
            const blob = await blobResponse.blob();

            fileInfo.data = await MudExUriHelper.blobToByteArray(blob);
        }

        return fileInfo;
    }
    
    async getAllFilesInFolder(folderId) {
        // Function to recursively fetch all files within a folder
        // Additional implementation may be required here
        return [];
    }

    async getFilePath(driveId, fileId) {
        // Function to build the file path
        // Additional implementation may be required here
        return '';
    }

    dispose() {
        // Cleanup if necessary
    }
}

window.MudExOneDriveFilePicker = MudExOneDriveFilePicker;

export function initializeMudExOneDriveFilePicker(elementRef, dotnet, options) {
    return new MudExOneDriveFilePicker(elementRef, dotnet, options);
}
