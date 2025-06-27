class MudExOneDriveFilePicker extends MudExExternalFilePickerBase {

    openPicker() {
        const odOptions = {
            clientId: this.options.clientId,
            action: "query",
            multiSelect: this.options.multiSelect || false,
            viewType: this.options.allowFolderSelect ? 'all' : 'files',
            advanced: {
                redirectUri: this.options.redirectUri || window.location.origin,
                accessToken: this.accessToken || undefined
            },
            success: (files) => {
                this.accessToken = files.accessToken;
                this.apiEndpoint = files.apiEndpoint;
                this.dotnet.invokeMethodAsync(this.options.onAuthorizedCallback, this.accessToken);
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
                const folderFiles = await this.getAllFilesInFolder(file.id);
                fileDataArray.push(...folderFiles);
            } else {
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
            apiPath: this.apiEndpoint,
            fileName: fileName,
            extension: extension,
            contentType: fileDetails.file.mimeType,
            webUrl: fileDetails.webUrl,
            url: fileDetails["@microsoft.graph.downloadUrl"],
            downloadUrl: fileDetails["@microsoft.graph.downloadUrl"],
            path: (fileDetails.parentReference?.path || '').split(':').pop() // Path might not be directly available
        };

        if (this.options.autoLoadFileDataBytes) {
            //const blobResponse = await fetch(fileDetails["@microsoft.graph.downloadUrl"], {
            //    headers: new Headers({ Authorization: `Bearer ${this.accessToken}` })
            //});
            const blobResponse = await fetch(fileDetails["@microsoft.graph.downloadUrl"]);
            const blob = await blobResponse.blob();

            fileInfo.data = await MudExUriHelper.blobToByteArray(blob);
        }

        return fileInfo;
    }
    
    async getAllFilesInFolder(folderId) {
        let files = [];
        let nextPageUrl = `${this.apiEndpoint}me/drive/items/${folderId}/children`;

        while (nextPageUrl) {
            const response = await fetch(nextPageUrl, {
                headers: new Headers({ Authorization: `Bearer ${this.accessToken}` })
            });
            const data = await response.json();

            for (const item of data.value) {
                if (item.folder) {
                    // If it's a folder, fetch files within it recursively
                    const subFolderFiles = await this.getAllFilesInFolder(item.id);
                    files.push(...subFolderFiles);
                } else {
                    files.push(await this.processFile(item));
                }
            }

            nextPageUrl = data['@odata.nextLink']; // Check if there's a next page
        }

        return files;
    }
    
    dispose() {
        // Cleanup if necessary
    }
}

window.MudExOneDriveFilePicker = MudExOneDriveFilePicker;

export function initializeMudExOneDriveFilePicker(elementRef, dotnet, options) {
    return new MudExOneDriveFilePicker(elementRef, dotnet, options);
}
