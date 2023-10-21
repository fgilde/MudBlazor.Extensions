class MudExUriHelper {
    static createBlobUrlFromByteArray(byteArray, mimeType) {
        const blob = new Blob([new Uint8Array(byteArray)], { type: mimeType });
        return URL.createObjectURL(blob);
    }

    static async readBlobAsText(blobUrl) {
        const response = await fetch(blobUrl);
        const blob = await response.blob();
        return await blob.text();
    }

    static async readBlobAsByteArray(blobUrl) {
        const response = await fetch(blobUrl);
        const blob = await response.blob();
        const arrayBuffer = await blob.arrayBuffer();
        return Array.from(new Uint8Array(arrayBuffer));
    }

    static revokeBlobUrl(blobUrl) {
        URL.revokeObjectURL(blobUrl);
    }
}

window.MudExUriHelper = MudExUriHelper;