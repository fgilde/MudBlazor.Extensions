class MudExCapture {
    static recordings = {};

    static async startCapture(options, callback) {
        const id = this.generateUniqueId();
        const { stream, mediaRecorder } = await this.setupCapture(options, id, callback);

        mediaRecorder.start();

        this.recordings[id] = { mediaRecorder, options, stream };
        return id;
    }

    static stopCapture(id) {
        const recording = this.recordings[id];
        if (recording) {
            recording.mediaRecorder.stop();
            recording.stream.getTracks().forEach(track => track.stop());
        }
    }

    static async setupCapture(options, id, callback) {
        // Build video and audio constraints
        const videoConstraints = options.captureScreen
            ? { video: true }
            : { video: { deviceId: options.videoDeviceId ? { exact: options.videoDeviceId } : undefined } };

        const audioStreams = await this.getAudioStreams(options.audioDevices);
        const videoStream = options.captureScreen
            ? await navigator.mediaDevices.getDisplayMedia({ video: true })
            : await navigator.mediaDevices.getUserMedia({ video: videoConstraints });

        // Combine video stream and audio streams
        const combinedStream = this.combineStreams(videoStream, audioStreams);

        const mediaRecorder = new MediaRecorder(combinedStream, { mimeType: 'video/webm; codecs=vp9' });
        const recordedChunks = [];

        mediaRecorder.ondataavailable = event => recordedChunks.push(event.data);
        mediaRecorder.onstop = async () => this.saveVideoData(recordedChunks, callback, id);

        return { stream: combinedStream, mediaRecorder };
    }

    static async getAudioStreams(audioDeviceIds) {
        if (!audioDeviceIds || audioDeviceIds.length === 0) {
            return [];
        }

        const audioStreams = await Promise.all(
            audioDeviceIds.map(async deviceId => {
                return await navigator.mediaDevices.getUserMedia({
                    audio: { deviceId: { exact: deviceId } }
                });
            })
        );

        return audioStreams;
    }

    static combineStreams(videoStream, audioStreams) {
        // Create a new MediaStream to hold the combined tracks
        const combinedStream = new MediaStream();

        // Add video tracks
        videoStream.getTracks().forEach(track => combinedStream.addTrack(track));

        // Add all audio tracks from each audio stream
        audioStreams.forEach(audioStream => {
            audioStream.getAudioTracks().forEach(track => combinedStream.addTrack(track));
        });

        return combinedStream;
    }

    static generateUniqueId() {
        return `${new Date().getTime()}`;
    }

    static async saveVideoData(recordedChunks, callback, id) {
        const videoBlob = new Blob(recordedChunks, { type: 'video/webm' });

        // Generate a Blob URL and log it to the console
        const videoUrl = URL.createObjectURL(videoBlob);
        console.log(`Recorded video available at: ${videoUrl}`);

        const videoArrayBuffer = await videoBlob.arrayBuffer();
        const videoByteArray = new Uint8Array(videoArrayBuffer);

        if (callback['invokeMethodAsync']) {
            callback.invokeMethodAsync('Invoke', { videoData: videoByteArray });
        }

        delete this.recordings[id];
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
