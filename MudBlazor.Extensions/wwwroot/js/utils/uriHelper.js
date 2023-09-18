class MudExUriHelper {
    static createBlobUrlFromByteArray(byteArray, mimeType) {
        const blob = new Blob([new Uint8Array(byteArray)], { type: mimeType });
        return URL.createObjectURL(blob);
    }

    static revokeBlobUrl(blobUrl) {
        URL.revokeObjectURL(blobUrl);
    }
}

window.MudExUriHelper = MudExUriHelper;