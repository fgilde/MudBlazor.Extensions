class MudExDropBoxFilePicker extends MudExExternalFilePickerBase {
  
 
    setOptions(options) {
        super.setOptions(options);
        if (this.options.apiKey) {
            this.loadApi();
        }
    }

    loadApi() {
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
    
    openPicker() {
        Dropbox.choose({
            success: async (files) => {
                const fileDataArray = await Promise.all(files.map(async file => {
                    const path = await this.getPath(file.link);
                    return {
                        url: file.link,
                        fileName: file.name,
                        size: file.bytes,
                        isDir: file.isDir,
                        icon: file.icon,
                        path: path
                    };
                }));
                this.dotnet.invokeMethodAsync('OnFilesSelected', fileDataArray);
            },
            cancel: () => {
                this.dotnet.invokeMethodAsync('OnFilesSelected', []);
            },
            linkType: 'direct',
            multiselect: this.options.multiSelect || false,
            sizeLimit: this.options.maxFileSize || undefined,
            extensions: this.options.allowedExtensions || [],
            folderselect: false
        });
    }

    extractFilePath(url) {
        const urlObj = new URL(url);
        const pathname = urlObj.pathname;
        const pathSegments = pathname.split('/');
        // Exclude the last segment (filename) and join the remaining segments
        const folderPath = '/' + pathSegments.slice(4, -1).join('/');
        return folderPath;
    }

    async getPath(fileLink) {
        return this.extractFilePath(fileLink);
        //const accessToken = this.options.apiKey; // Currently I only use the api and didn't have the access token
        //const headers = new Headers({
        //    'Authorization': 'Bearer ' + accessToken,
        //    'Content-Type': 'application/json'
        //});

        //const response = await fetch('https://api.dropboxapi.com/2/sharing/get_shared_link_metadata', {
        //    method: 'POST',
        //    headers: headers,
        //    body: JSON.stringify({
        //        url: fileLink
        //    })
        //});

        //const data = await response.json();
        //return data.path_lower;
    }

    dispose() {
        // Cleanup if necessary
    }
}

window.MudExDropBoxFilePicker = MudExDropBoxFilePicker;

export function initializeMudExDropBoxFilePicker(elementRef, dotnet, options) {
    return new MudExDropBoxFilePicker(elementRef, dotnet, options);
}
