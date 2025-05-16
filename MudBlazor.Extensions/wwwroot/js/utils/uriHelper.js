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
        return await this.blobToByteArray(blob);

        // The following code works in general, but raises errors on serverside rendering
        //const arrayBuffer = await blob.arrayBuffer();
        //return Array.from(new Uint8Array(arrayBuffer));
    }

    static blobToByteArray(blob) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = function () {
                const arrayBuffer = this.result;
                const byteArray = new Uint8Array(arrayBuffer);
                resolve(byteArray);
            };
            reader.onerror = reject;
            reader.readAsArrayBuffer(blob);
        });
    }

    static blobToBase64(blob) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = function () {
                const base64String = btoa(String.fromCharCode(...new Uint8Array(this.result)));
                resolve(base64String);
            };
            reader.onerror = reject;
            reader.readAsArrayBuffer(blob);
        });
    }

    static revokeBlobUrl(blobUrl) {
        URL.revokeObjectURL(blobUrl);
    }
}

window.MudExUriHelper = MudExUriHelper;