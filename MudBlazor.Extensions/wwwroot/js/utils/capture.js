class MudExCapture {
    static recordings = {};

    static async selectCaptureSource() {
        try {
            const stream = await navigator.mediaDevices.getDisplayMedia({ video: true });
            const selectedTrack = stream.getVideoTracks()[0];
            stream.getTracks().forEach(track => track.stop());
            return {
                id: selectedTrack.id,
                label: selectedTrack.label,
                kind: selectedTrack.kind,
                deviceId: selectedTrack.getSettings().deviceId,
                stats: selectedTrack.stats,
                enabled: selectedTrack.enabled,
                muted: selectedTrack.muted,
                readyState: selectedTrack.readyState
            };
        } catch (err) {
            console.error("Fehler beim Anzeigen der Bildschirmquelle.", err);
            return null;
        }
    }

    static async startCapture(options, callback) {
        const id = this.generateUniqueId();
        const capture = await this.setupCapture(options, id, callback);

        // Event listener für das manuelle Stoppen der Aufnahme
        if (capture.screenStream) {
            capture.screenStream.getVideoTracks()[0].addEventListener("ended", () => {
                this.stopCapture(id);
                if (callback['invokeMethodAsync']) {
                    callback.invokeMethodAsync('OnStopped', id);
                }
            });
        }

        // Starte alle Recorder
        capture.recorders.forEach(recorder => recorder.start());

        this.recordings[id] = capture;
        return id;
    }

    static stopCapture(id) {
        const recording = this.recordings[id];
        if (recording) {
            recording.recorders.forEach(recorder => recorder.stop());
            recording.streams.forEach(stream => {
                if (stream) stream.getTracks().forEach(track => track.stop());
            });
            delete this.recordings[id];
        }
    }

    static async setupCapture(options, id, callback) {
        debugger;
        options.contentType = options.contentType || 'video/webm; codecs=vp9';
        const audioContentType = options.audioContentType || 'audio/webm';

        // Streams sammeln
        const streams = {
            screen: options.captureScreen ? await navigator.mediaDevices.getDisplayMedia({ video: true }) : null,
            camera: options.videoDeviceId ? await navigator.mediaDevices.getUserMedia({
                video: { deviceId: { exact: options.videoDeviceId } }
            }) : null,
            audio: null
        };

        // Audio Streams zusammenführen
        const audioStreams = await this.getAudioStreams(options.audioDevices);
        streams.audio = this.mergeAudioStreams(audioStreams);

        // Canvas für Picture-in-Picture Setup
        const { combinedStream, canvas } = this.createCombinedStream(streams);

        // Chunks für jeden Stream
        const chunks = {
            screen: [],
            camera: [],
            audio: [],
            combined: []
        };

        // MediaRecorder erstellen
        const recorders = [];

        // Haupt-Video Recorder (Screen oder Kamera)
        if (streams.screen) {
            const screenRecorder = new MediaRecorder(streams.screen, { mimeType: options.contentType });
            screenRecorder.ondataavailable = event => chunks.screen.push(event.data);
            recorders.push(screenRecorder);
        }

        // Kamera Recorder (wenn vorhanden)
        if (streams.camera) {
            const cameraRecorder = new MediaRecorder(streams.camera, { mimeType: options.contentType });
            cameraRecorder.ondataavailable = event => chunks.camera.push(event.data);
            recorders.push(cameraRecorder);
        }

        // Audio Recorder
        if (streams.audio) {
            const audioRecorder = new MediaRecorder(streams.audio, { mimeType: audioContentType });
            audioRecorder.ondataavailable = event => chunks.audio.push(event.data);
            recorders.push(audioRecorder);
        }

        // Combined Recorder
        if (combinedStream) {
            const combinedRecorder = new MediaRecorder(combinedStream, { mimeType: options.contentType });
            combinedRecorder.ondataavailable = event => chunks.combined.push(event.data);
            combinedRecorder.onstop = async () => {
                if (canvas && canvas.stream) {
                    canvas.stream.getTracks().forEach(track => track.stop());
                }
                await this.saveVideoData(chunks, callback, id, options);
            };
            recorders.push(combinedRecorder);
        }

        return {
            streams: Object.values(streams).filter(stream => stream !== null),
            recorders,
            screenStream: streams.screen,
            cameraStream: streams.camera,
            audioStream: streams.audio,
            combinedStream,
            canvas
        };
    }

    static createCombinedStream(streams) {
        const { screen, camera, audio } = streams;

        // Wenn wir keine visuellen Streams haben, return null
        if (!screen && !camera) {
            return { combinedStream: audio, canvas: null };
        }

        // Wenn wir nur einen visuellen Stream haben
        if (!screen || !camera) {
            const videoStream = screen || camera;
            return {
                combinedStream: this.combineStreams(videoStream, audio),
                canvas: null
            };
        }

        // Picture-in-Picture Setup für Screen + Camera
        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d');

        // Screen-Größe als Basis verwenden
        const screenTrack = screen.getVideoTracks()[0];
        const settings = screenTrack.getSettings();
        canvas.width = settings.width;
        canvas.height = settings.height;

        // Video Elemente für beide Streams
        const screenVideo = document.createElement('video');
        const cameraVideo = document.createElement('video');
        screenVideo.srcObject = screen;
        cameraVideo.srcObject = camera;
        screenVideo.play();
        cameraVideo.play();

        // Picture-in-Picture Rendering
        const draw = () => {
            // Hauptbild (Screen)
            ctx.drawImage(screenVideo, 0, 0, canvas.width, canvas.height);

            // Kamera als Overlay (unten rechts, 20% der Gesamtgröße)
            const pipWidth = canvas.width * 0.2;
            const pipHeight = (canvas.width * 0.2) * (9 / 16); // 16:9 Aspekt
            ctx.drawImage(cameraVideo,
                canvas.width - pipWidth - 20,  // 20px Abstand vom rechten Rand
                canvas.height - pipHeight - 20, // 20px Abstand vom unteren Rand
                pipWidth,
                pipHeight
            );

            requestAnimationFrame(draw);
        };
        requestAnimationFrame(draw);

        // Canvas als Stream
        const canvasStream = canvas.captureStream();

        // Audio hinzufügen wenn vorhanden
        if (audio) {
            audio.getAudioTracks().forEach(track => canvasStream.addTrack(track));
        }

        return {
            combinedStream: canvasStream,
            canvas: {
                element: canvas,
                stream: canvasStream,
                videos: [screenVideo, cameraVideo]
            }
        };
    }

    static async getAudioStreams(audioDeviceIds) {
        if (!audioDeviceIds || audioDeviceIds.length === 0) return [];

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

        return audioStreams.filter(stream => stream !== null);
    }

    static combineStreams(videoStream, audioStream) {
        const combinedStream = new MediaStream();
        if (videoStream) {
            videoStream.getVideoTracks().forEach(track => combinedStream.addTrack(track));
        }
        if (audioStream) {
            audioStream.getAudioTracks().forEach(track => combinedStream.addTrack(track));
        }
        return combinedStream;
    }

    static mergeAudioStreams(audioStreams) {
        if (!audioStreams || audioStreams.length === 0) return null;

        const combinedAudioStream = new MediaStream();
        audioStreams.forEach(audioStream => {
            audioStream.getAudioTracks().forEach(track => combinedAudioStream.addTrack(track));
        });
        return combinedAudioStream;
    }

    static generateUniqueId() {
        return `${new Date().getTime()}`;
    }

    static async saveVideoData(chunks, callback, id, options) {
        const { screen, camera, audio, combined } = chunks;

        // Blobs erstellen
        const createBlobData = async (chunks, contentType) => {
            if (!chunks || chunks.length === 0) return null;
            const blob = new Blob(chunks, { type: contentType });
            const arrayBuffer = await blob.arrayBuffer();
            return {
                bytes: new Uint8Array(arrayBuffer),
                blobUrl: URL.createObjectURL(blob),
                contentType
            };
        };

        const result = {
            captureData: await createBlobData(screen, options.contentType),
            cameraData: await createBlobData(camera, options.contentType),
            audioData: await createBlobData(audio, options.audioContentType || 'audio/webm'),
            combinedData: await createBlobData(combined, options.contentType),
            options: options,
            captureId: id
        };

        if (callback['invokeMethodAsync']) {
            callback.invokeMethodAsync('Invoke', result);
        }
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