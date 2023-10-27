class MudExGoogleFilePicker {
    elementRef;
    dotnet;
    options;
    accessToken = null;
    pickerInited = false;
    gisInited = false;
    tokenClient;

    constructor(elementRef, dotNet, options) {
        this.elementRef = elementRef;
        this.dotnet = dotNet;
        this.options = options;

        this.loadGoogleApis();
    }

    loadGoogleApis() {
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
            scope: 'https://www.googleapis.com/auth/drive.metadata.readonly',
            callback: '', // defined later
        });
        this.gisInited = true;
        this.maybeEnableButtons();
    }

    maybeEnableButtons() {
        if (this.pickerInited && this.gisInited) {
            debugger;
            this.dotnet.invokeMethodAsync('OnReady');
            // Enable your buttons or trigger your UI updates here
        }
    }

    handleAuthClick() {
        this.tokenClient.callback = async (response) => {
            if (response.error !== undefined) {
                throw (response);
            }
            this.accessToken = response.access_token;
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
        const view = new google.picker.View(google.picker.ViewId.DOCS);
       // view.setMimeTypes('image/png,image/jpeg,image/jpg');
        const picker = new google.picker.PickerBuilder()
           // .enableFeature(google.picker.Feature.NAV_HIDDEN)
            .enableFeature(google.picker.Feature.MULTISELECT_ENABLED)
            .setDeveloperKey(this.options.apiKey)
            .setAppId(this.options.appId)
            .setOAuthToken(this.accessToken)
            .addView(view)
            .addView(new google.picker.DocsUploadView())
            .setCallback(this.pickerCallback.bind(this))
            .build();
        picker.setVisible(true);
    }

    async pickerCallback(data) {
        if (data.action === google.picker.Action.PICKED) {
            const document = data[google.picker.Response.DOCUMENTS][0];
            const fileId = document[google.picker.Document.ID];
            const res = await gapi.client.drive.files.get({
                'fileId': fileId,
                'fields': '*',
            });

            //if (data.action === google.picker.Action.PICKED) {
            //    let text = `Picker response: \n${JSON.stringify(data, null, 2)}\n`;
            //    const document = data[google.picker.Response.DOCUMENTS][0];
            //    const fileId = document[google.picker.Document.ID];
            //    console.log(fileId);
            //    const res = await gapi.client.drive.files.get({
            //        'fileId': fileId,
            //        'fields': '*',
            //    });
            //    text += `Drive API response for first document: \n${JSON.stringify(res.result, null, 2)}\n`;
            //    window.document.getElementById('content').innerText = text;
            //}

            // Here you can invoke your .NET callback with the file information
            // For example: this.dotnet.invokeMethodAsync('YourDotNetCallbackMethod', res.result);
        }
    }

    // Add other methods from the original script here, but make sure to adapt them to the class structure.
    // For example, use `this.accessToken` instead of `accessToken`, and so on.

    dispose() {
        // Cleanup if necessary
    }
}

window.MudExGoogleFilePicker = MudExGoogleFilePicker;

export function initializeMudExGoogleFilePicker(elementRef, dotnet, options) {
    return new MudExGoogleFilePicker(elementRef, dotnet, options);
}
