class MudExGoogleFilePicker extends MudExExternalFilePickerBase {
    gisInited = false;
    tokenClient;

 
    loadApi() {
        const gapiScript = document.createElement('script');
        gapiScript.async = true;
        gapiScript.defer = true;
        gapiScript.src = 'https://apis.google.com/js/api.js';
        gapiScript.onload = this.gapiLoaded.bind(this);
        document.body.appendChild(gapiScript);

        const gisScript = document.createElement('script');
        gisScript.async = true;
        gisScript.defer = true;
        gisScript.src = 'https://accounts.google.com/gsi/client';
        gisScript.onload = this.gisLoaded.bind(this);
        document.body.appendChild(gisScript);
    }

    gapiLoaded() {
        gapi.load('client:picker', this.initializePicker.bind(this));
    }

    async initializePicker() {
        await gapi.client.load('https://www.googleapis.com/discovery/v1/apis/drive/v3/rest');
        this.pickerInited = true;
        this.maybeEnableButtons();
    }

    gisLoaded() {
        this.tokenClient = google.accounts.oauth2.initTokenClient({
            client_id: this.options.clientId,
            scope: 'https://www.googleapis.com/auth/drive.readonly',
            callback: '', // defined later
        });
        this.gisInited = true;
        this.maybeEnableButtons();
    }

    maybeEnableButtons() {
        if (this.pickerInited && this.gisInited) {
            this.dotnet.invokeMethodAsync(this.options.onReadyCallback);
        }
    }

    openPicker() {
        this.tokenClient.callback = async (response) => {
            if (response.error !== undefined) {
                throw (response);
            }
            this.accessToken = response.access_token;
            this.dotnet.invokeMethodAsync(this.options.onAuthorizedCallback, this.accessToken);
            // Update your UI accordingly
            await this.createPicker();
        };

        if (this.accessToken === null) {
            this.tokenClient.requestAccessToken({ prompt: 'consent' });
        } else {
            this.tokenClient.requestAccessToken({ prompt: '' });
        }
    }

    handleSignoutClick() {
        if (this.accessToken) {
            this.accessToken = null;
            google.accounts.oauth2.revoke(this.accessToken);
            // Reset or update your UI accordingly
        }
    }

    createPicker() {
        //const view = new google.picker.View(google.picker.ViewId.DOCS);
        const view = new google.picker.DocsView()
            .setIncludeFolders(this.options.allowFolderNavigation || this.options.allowFolderSelect)
            .setSelectFolderEnabled(this.options.allowFolderSelect && this.options.multiSelect);

        if (this.options.allowedMimeTypes) {
            view.setMimeTypes(this.options.allowedMimeTypes);
        }

        const pickerBuilder = new google.picker.PickerBuilder()
            //.enableFeature(google.picker.Feature.NAV_HIDDEN)
            .setOAuthToken(this.accessToken)
            .addView(view)
            .setCallback(this.pickerCallback.bind(this));
        if (this.options.allowUpload) {
            pickerBuilder.addView(new google.picker.DocsUploadView());
            pickerBuilder.enableFeature(google.picker.Feature.SIMPLE_UPLOAD_ENABLED);
        }
        if (this.options.multiSelect) {
            pickerBuilder.enableFeature(google.picker.Feature.MULTISELECT_ENABLED);
        }
        if (this.options.apiKey) {
            pickerBuilder.setDeveloperKey(this.options.apiKey);
        }
        if (this.options.appId) {
            pickerBuilder.setAppId(this.options.appId);
        }
        const picker = pickerBuilder.build();
        picker.setVisible(true);
    }

    async pickerCallback(data) {
        if (data.action === google.picker.Action.PICKED) {
            let fileInfoArray = [];
            for (const doc of data.docs) {
                const fileId = doc.id;
                const fileName = doc.name;
                const mimeType = doc.mimeType;

                if (mimeType === 'application/vnd.google-apps.folder') {
                    // If it's a folder, get all files within it
                    const folderFiles = await this.getAllFilesInFolder(fileId, fileName + '/');
                    for (const folderFile of folderFiles) {
                        const fileInfo = await this.processFile(folderFile.id, folderFile.name, folderFile.path);
                        fileInfoArray.push(fileInfo);
                    }
                } else {
                    const fileInfo = await this.processFile(fileId, fileName, '');
                    fileInfoArray.push(fileInfo);
                }
            }

            this.dotnet.invokeMethodAsync(this.options.onFilesSelectedCallback, fileInfoArray);
        } else if (data.action === google.picker.Action.CANCEL) {
            this.dotnet.invokeMethodAsync(this.options.onFilesSelectedCallback, []);
        }
    }

    async processFile(fileId, fileName, path) {
        const extension = fileName.split('.').pop();

        const res = await gapi.client.drive.files.get({
            fileId: fileId,
            fields: 'webViewLink, webContentLink, parents, mimeType, size'
        });
        const parentIds = res.result.parents || [];
        if (this.options.alwaysLoadPath) {
            path = (await this.getFolderPath(parentIds[0]));
        }

        // Building the file info object
        let fileInfo = {
            id: fileId,
            accessToken: this.accessToken,
            fileName: fileName,
            extension: extension,
            contentType: res.result.mimeType,
            webViewLink: res.result.webViewLink,
            webContentLink: res.result.webContentLink,
            path: path
        };

        if (res.result.size) {
            fileInfo.size = parseFloat(res.result.size);
        }

        if (this.options.autoLoadFileDataBytes) {
            const blobResponse = await fetch(`https://www.googleapis.com/drive/v3/files/${fileId}?alt=media`,
                {
                    headers: new Headers({ Authorization: `Bearer ${this.accessToken}` })
                });
            const blob = await blobResponse.blob();

            fileInfo.data = await MudExUriHelper.blobToByteArray(blob);
        }
        
        return fileInfo;
    }


    async getFolderPath(folderId) {
        let folderPath = '';
        let currentFolderId = folderId;

        while (currentFolderId) {
            const folderRes = await gapi.client.drive.files.get({
                fileId: currentFolderId,
                fields: 'name, parents'
            });

            // Stop if the current folder has no parents (root folder) "My Drive"
            if (!folderRes.result.parents) {
                break;
            }

            folderPath = folderRes.result.name + (folderPath ? '/' + folderPath : '');
            currentFolderId = folderRes.result.parents[0];
        }

        return folderPath;
    }

    async getAllFilesInFolder(folderId, path) {
        let allFiles = [];
        let pageToken = null;

        do {
            const response = await gapi.client.drive.files.list({
                q: `'${folderId}' in parents and trashed=false`,
                fields: 'nextPageToken, files(id, name, mimeType)',
                pageToken: pageToken
            });

            for (const file of response.result.files) {
                if (file.mimeType === 'application/vnd.google-apps.folder') {
                    // If it's a folder, recursively get its files
                    const subFiles = await this.getAllFilesInFolder(file.id, path + file.name + '/');
                    allFiles = allFiles.concat(subFiles);
                } else {
                    // If it's a file, add it to the list
                    file.path = path;
                    allFiles.push(file);
                }
            }

            pageToken = response.result.nextPageToken;
        } while (pageToken);

        return allFiles;
    }
    

    dispose() {
        // Cleanup if necessary
    }
}

window.MudExGoogleFilePicker = MudExGoogleFilePicker;

export function initializeMudExGoogleFilePicker(elementRef, dotnet, options) {
    return new MudExGoogleFilePicker(elementRef, dotnet, options);
}