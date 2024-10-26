class MudExCapture {
    static recordings = {};

    static async selectCaptureSource() {
        try {
            const stream = await navigator.mediaDevices.getDisplayMedia({ video: true });
            const selectedTrack = stream.getVideoTracks()[0];
            stream.getTracks().forEach(track => track.stop()); // Beende den Stream direkt nach der Auswahl
            var result = {
                id: selectedTrack.id,
                label: selectedTrack.label,
                kind: selectedTrack.kind,
                deviceId: selectedTrack.getSettings().deviceId,
                stats: selectedTrack.stats,
                enabled: selectedTrack.enabled,
                muted: selectedTrack.muted,
                readyState: selectedTrack.readyState
            };
            return result;
        } catch (err) {
            console.error("Fehler beim Anzeigen der Bildschirmquelle.", err);
            return null;
        }
    }

    static async startCapture(options, callback) {
        const id = this.generateUniqueId();
        const { videoStream, audioStream, combinedStream, mediaRecorders } = await this.setupCapture(options, id, callback);

        // Eventlistener für das Stoppen der Freigabe durch den Benutzer
        combinedStream.getVideoTracks()[0].addEventListener("ended", () => {
            this.stopCapture(id);
            if (callback['invokeMethodAsync']) {
                callback.invokeMethodAsync('OnStopped', id);
            }
        });

        // Start all media recorders
        mediaRecorders.forEach(recorder => recorder.start());

        this.recordings[id] = { mediaRecorders, options, videoStream, audioStream, combinedStream };
        return id;
    }

    static stopCapture(id) {
        const recording = this.recordings[id];
        if (recording) {
            recording.mediaRecorders.forEach(recorder => recorder.stop());
            recording.combinedStream.getTracks().forEach(track => track.stop());
            if (recording.videoStream) recording.videoStream.getTracks().forEach(track => track.stop());
            if (recording.audioStream) recording.audioStream.getTracks().forEach(track => track.stop());
        }
    }

    static async setupCapture(options, id, callback) {
        options.contentType = options.contentType || 'video/webm; codecs=vp9';
        const videoConstraints = options.captureScreen
            ? { video: true }
            : { video: { deviceId: options.videoDeviceId ? { exact: options.videoDeviceId } : undefined } };

        const audioStreams = await this.getAudioStreams(options.audioDevices);
        const videoStream = options.captureScreen
            ? await navigator.mediaDevices.getDisplayMedia({ video: true })
            : await navigator.mediaDevices.getUserMedia({ video: videoConstraints });

        const audioStream = this.mergeAudioStreams(audioStreams);
        const combinedStream = this.combineStreams(videoStream, audioStream);

        const videoChunks = [];
        const audioChunks = [];
        const combinedChunks = [];

        // Create MediaRecorder for each stream
        const videoRecorder = new MediaRecorder(videoStream, { mimeType: options.contentType });
        const audioRecorder = new MediaRecorder(audioStream, { mimeType: 'audio/webm' });
        const combinedRecorder = new MediaRecorder(combinedStream, { mimeType: options.contentType });

        // Collect data for each recorder
        videoRecorder.ondataavailable = event => videoChunks.push(event.data);
        audioRecorder.ondataavailable = event => audioChunks.push(event.data);
        combinedRecorder.ondataavailable = event => combinedChunks.push(event.data);

        // Handle stop event for all recorders
        combinedRecorder.onstop = async () => this.saveVideoData(videoChunks, audioChunks, combinedChunks, callback, id, options);

        return { videoStream, audioStream, combinedStream, mediaRecorders: [videoRecorder, audioRecorder, combinedRecorder] };
    }

    static async getAudioStreams(audioDeviceIds) {
        if (!audioDeviceIds || audioDeviceIds.length === 0) {
            return [];
        }

        const audioStreams = await Promise.all(
            audioDeviceIds.map(async deviceId => {
                try {
                    return await navigator.mediaDevices.getUserMedia({
                        audio: { deviceId: { exact: deviceId } }
                    });
                } catch (error) {
                    console.warn(`Audio device with ID ${deviceId} konnte nicht abgerufen werden.`, error);
                    return null;
                }
            })
        );

        return audioStreams.filter(stream => stream !== null); // Filter out failed streams
    }

    static mergeAudioStreams(audioStreams) {
        const combinedAudioStream = new MediaStream();
        audioStreams.forEach(audioStream => {
            audioStream.getAudioTracks().forEach(track => combinedAudioStream.addTrack(track));
        });
        return combinedAudioStream;
    }

    static combineStreams(videoStream, audioStream) {
        const combinedStream = new MediaStream();
        videoStream.getTracks().forEach(track => combinedStream.addTrack(track));
        if (audioStream) {
            audioStream.getTracks().forEach(track => combinedStream.addTrack(track));
        }
        return combinedStream;
    }

    static generateUniqueId() {
        return `${new Date().getTime()}`;
    }

    static async saveVideoData(videoChunks, audioChunks, combinedChunks, callback, id, options) {
        const combinedBlob = new Blob(combinedChunks, { type: options.contentType });
        const videoBlob = new Blob(videoChunks, { type: options.contentType });
        const audioBlob = new Blob(audioChunks, { type: 'audio/webm' });

        const combinedUrl = URL.createObjectURL(combinedBlob);
        const videoUrl = URL.createObjectURL(videoBlob);
        const audioUrl = URL.createObjectURL(audioBlob);

        const combinedArrayBuffer = await combinedBlob.arrayBuffer();
        const combinedByteArray = new Uint8Array(combinedArrayBuffer);

        const videoArrayBuffer = await videoBlob.arrayBuffer();
        const videoByteArray = new Uint8Array(videoArrayBuffer);

        const audioArrayBuffer = await audioBlob.arrayBuffer();
        const audioByteArray = new Uint8Array(audioArrayBuffer);

        if (callback['invokeMethodAsync']) {
            callback.invokeMethodAsync('Invoke',
                {
                    videoData: { bytes: videoByteArray, blobUrl: videoUrl },
                    audioData: { bytes: audioByteArray, blobUrl: audioUrl },
                    combinedData: { bytes: combinedByteArray, blobUrl: combinedUrl },
                    options: options,
                    captureId: id
                });
        }

        delete this.recordings[id];
    }

    static async getDevices() {
        return await navigator.mediaDevices.enumerateDevices();
    }

    static async getAvailableAudioDevices() {
        const devices = await navigator.mediaDevices.enumerateDevices();
        return devices.filter(device => device.kind === 'audioinput');
    }

    static async getAvailableVideoDevices() {
        const devices = await navigator.mediaDevices.enumerateDevices();
        return devices.filter(device => device.kind === 'videoinput');
    }
}

window.MudExCapture = MudExCapture;
