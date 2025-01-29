class MudExCapture {
    static recordings = {};
    static _preselected = {};

    static setText(videoElement, text) {
        videoElement = MudExObserver.getElement(videoElement);
        const canvas = document.createElement('canvas');
        const context = canvas.getContext('2d');
        canvas.width = 640;
        canvas.height = 480;

        context.fillStyle = 'black';
        context.fillRect(0, 0, canvas.width, canvas.height);
        context.font = '48px Arial';
        context.fillStyle = 'white';
        context.translate(canvas.width / 2, canvas.height / 2);
        context.rotate(-Math.PI / 4);
        context.textAlign = 'center';
        context.fillText(text, 0, 0);

        const stream = canvas.captureStream(30); // 30 fps
        if (videoElement && typeof videoElement.play === 'function') {
            videoElement.autoplay = true;
            videoElement.muted = true;
            videoElement.srcObject = stream;
            videoElement.play();
        }
    }

    static switchSrcObject(videoElement1, videoElement2, condition) {
        const element1 = MudExObserver.getElement(videoElement1);
        const element2 = MudExObserver.getElement(videoElement2);
        if (element1 && element2 && condition) {
            var srcObject1 = element1.srcObject;
            element1.srcObject = element2.srcObject;
            element2.srcObject = srcObject1;
        }
    }

    static async selectCaptureSource(captureMediaOptions, videoElementForPreview) {
        try {
            var captureMediaOptionsWithoutNullProperties = this.removeOptionsWithoutNullProperties(captureMediaOptions);
            var stream = null;
            if (captureMediaOptions.video?.deviceId)
                stream = await navigator.mediaDevices.getUserMedia(this.prepareVideoConstraints(captureMediaOptions.video.deviceId, captureMediaOptions.video));
            else if (captureMediaOptions.audio?.deviceId)
                stream = await navigator.mediaDevices.getUserMedia(this.prepareAudioConstraints(captureMediaOptions.audio.deviceId, captureMediaOptions.audio));
            else
                stream = await navigator.mediaDevices.getDisplayMedia(captureMediaOptionsWithoutNullProperties);

            const selectedTrack = stream.getVideoTracks()[0];
            const element = MudExObserver.getElement(videoElementForPreview);
            if (element && typeof element.play === 'function') {
                element.autoplay = true;
                element.muted = true;
                element.srcObject = stream;
                element.play();
            }
            //stream.getTracks().forEach(track => track.stop());
            this._preselected[selectedTrack.id] = stream;
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
            console.error("Error selecting capture source", err);
            return null;
        }
    }



    static stopPreviewCapture(trackId) {
        const stream = this._preselected[trackId];
        if (stream) {
            stream.getTracks().forEach(track => track.stop());
            delete this._preselected[trackId];
        }
    }

    static async startCapture(options, callback) {
        const id = this.generateUniqueId();
        const capture = await this.setupCapture(options, id, callback);
        if (capture.screenStream) {
            capture.screenStream.addEventListener("inactive", () => {
                this.stopCapture(id, callback);
            });
            capture.screenStream.getVideoTracks()[0].addEventListener("ended", () => {
                this.stopCapture(id, callback);
            });
        }
        capture.recorders.forEach(recorder => {
            setTimeout(() => { recorder.start(); }, 1);
        });

        this.recordings[id] = capture;
        return id;
    }

    static stopCapture(id, callback) {
        if (callback && callback['invokeMethodAsync']) {
            callback.invokeMethodAsync('OnStopped', id);
        }
        const recording = this.recordings[id];
        if (recording) {
            recording.recorders.forEach(recorder => {
                try {
                    recorder.stop();
                } catch (e) {

                }
            });
            recording.streams.forEach(stream => {
                if (stream && typeof stream.getTracks === 'function') {
                    stream.getTracks().forEach(track => {
                        try {
                            track.stop();
                        } catch (e) {

                        }
                    });
                }
            });
            if (recording.audioContext) {
                try {
                    recording.audioContext.close();
                } catch (e) {

                }
            }
            delete this.recordings[id];
        }
        this._preselected[id] = null;
        delete this._preselected[id];
    }

    static removeOptionsWithoutNullProperties(captureMediaOptions) {
        var captureMediaOptionsWithoutNullProperties = {};
        for (var key in captureMediaOptions) {
            if (Object.prototype.hasOwnProperty.call(captureMediaOptions, key)) {
                if (captureMediaOptions[key] !== null) {
                    captureMediaOptionsWithoutNullProperties[key] = captureMediaOptions[key];
                }
            }
        }
        return captureMediaOptionsWithoutNullProperties;
    }

    static prepareAudioConstraints(deviceId, constraints) {
        return this.constraintsAsMediaOptions(deviceId, constraints, 'audio');
    }

    static prepareVideoConstraints(deviceId, constraints) {
        return this.constraintsAsMediaOptions(deviceId, constraints, 'video');
    }

    static constraintsAsMediaOptions(deviceId, constraints, section) {
        const constraintsCopy = constraints ? { ...constraints } : {};
        deviceId = deviceId || constraintsCopy.deviceId;

        const result = {};
        result[section] = { ...constraintsCopy }; 
        result[section].deviceId = deviceId && deviceId !== 'default' ? { exact: deviceId } : undefined;

        return result;
    }

    static async setupCapture(options, id, callback) {
        var captureMediaOptionsWithoutNullProperties = this.removeOptionsWithoutNullProperties(options.captureMediaOptions);
        options.contentType = options.contentType || 'video/webm; codecs=vp9';
        const audioContentType = options.audioContentType || 'audio/webm';
        if (!options.captureScreen && !options.videoDevice && !options.videoConstraints?.deviceId) {
            // if only audio is captured, set content type to audio
            options.contentType = audioContentType;
        }

        const streams = {
            screen: null,
            camera: null,
            audio: null,
            systemAudio: null,
            audioContext: null
        };

        if (options.captureScreen) {
            try {
                let screenStream;
                if (options.screenSource && this._preselected[options.screenSource.id]) {
                    screenStream = this._preselected[id] = this._preselected[options.screenSource.id];
                    delete this._preselected[options.screenSource.id];
                }
                else {
                    screenStream = await navigator.mediaDevices.getDisplayMedia(captureMediaOptionsWithoutNullProperties);
                }
                streams.screen = new MediaStream(screenStream.getVideoTracks());

                const systemAudioTracks = screenStream.getAudioTracks();
                if (systemAudioTracks.length > 0) {
                    streams.systemAudio = new MediaStream(systemAudioTracks);
                }
            } catch (error) {
                console.warn('System Audio konnte nicht erfasst werden:', error);
            }
        }

        // Camera Stream
        if (options.videoDevice) {
            let videoDeviceId = typeof options.videoDevice === 'string'
                ? options.videoDevice
                : options.videoDevice?.deviceId;

            const constraints = typeof options.videoDevice === 'string' ? {} : options.videoDevice;

            const videoParam = this.prepareVideoConstraints(videoDeviceId, constraints);

            try {
                streams.camera = await navigator.mediaDevices.getUserMedia(videoParam);

            } catch (e) {
                console.error('Error while accessing the camera:', e);
            }
        }


        // Audio Streams
        const audioStreams = await this.getAudioStreams(options.audioDevices);
        // Audio Streams mischen
        const { stream: mixedAudioStream, audioContext } = this.mixAudioStreams(audioStreams);
        streams.audio = mixedAudioStream;
        streams.audioContext = audioContext;

        // Canvas Picture-in-Picture Setup
        const { combinedStream, canvas } = this.createCombinedStream(streams, options, id);

        // Chunks
        const chunks = {
            screen: [],
            camera: [],
            audio: [],
            systemAudio: [],
            combined: []
        };

        const recorders = [];

        // Main recorder
        if (streams.screen) {
            const screenRecorder = new MediaRecorder(streams.screen, { mimeType: options.contentType });
            screenRecorder.ondataavailable = event => chunks.screen.push(event.data);
            recorders.push(screenRecorder);
        }

        // System Audio Recorder
        if (streams.systemAudio) {
            const systemAudioRecorder = new MediaRecorder(streams.systemAudio, { mimeType: audioContentType });
            systemAudioRecorder.ondataavailable = event => chunks.systemAudio.push(event.data);
            recorders.push(systemAudioRecorder);
        }

        // Cam Recorder
        if (streams.camera) {
            const cameraRecorder = new MediaRecorder(streams.camera, { mimeType: options.contentType });
            cameraRecorder.ondataavailable = event => chunks.camera.push(event.data);
            recorders.push(cameraRecorder);
        }

        // Mic Audio Recorder
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
            systemAudioStream: streams.systemAudio,
            combinedStream,
            canvas,
            audioContext: streams.audioContext
        };
    }

    static createCombinedStream(streams, options, id) {
        const { screen, camera, audio, systemAudio, audioContext } = streams;
        // Mix audio streams
        const audioStreamsToMix = [audio, systemAudio].filter(s => s);
        const { stream: mixedAudioStream } = this.mixAudioStreams(audioStreamsToMix);
        streams.audioContext = audioContext;

        // Wenn wir keine visuellen Streams haben, kombiniere nur Audio
        if (!screen && !camera) {
            return {
                combinedStream: mixedAudioStream,
                canvas: null
            };
        }

        // Wenn wir nur einen visuellen Stream haben
        if (!screen || !camera) {
            const videoStream = screen || camera;
            return {
                combinedStream: this.combineStreams(videoStream, mixedAudioStream),
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
        const mainVideo = document.createElement('video');
        const overlayVideo = document.createElement('video');

        var useVideoDeviceAsOverlay = options.overlaySource === 'VideoDevice';
        mainVideo.srcObject = useVideoDeviceAsOverlay ? screen : camera;
        overlayVideo.srcObject = useVideoDeviceAsOverlay ? camera : screen;
        mainVideo.play();
        overlayVideo.play();

        // Frame Rate einstellen
        const frameRate = options.frameRate || 30;
        const frameInterval = 1000 / frameRate;

        const calculateOverlayPosition = (position, size, canvasWidth, canvasHeight) => {
            let x = 0;
            let y = 0;

            // Overlay-Größe parsen
            let overlayWidth, overlayHeight;
            try {
                const sizeObj = typeof size === 'string' ? JSON.parse(size) : size;
                overlayWidth = sizeObj.width.cssValue.includes('%')
                    ? (canvasWidth * parseFloat(sizeObj.width.cssValue) / 100)
                    : parseFloat(sizeObj.width.cssValue);
                overlayHeight = sizeObj.height.cssValue.includes('%')
                    ? (canvasHeight * parseFloat(sizeObj.height.cssValue) / 100)
                    : parseFloat(sizeObj.height.cssValue);
            } catch (e) {
                // Fallback zu Standard-Größen
                overlayWidth = canvasWidth * 0.2;
                overlayHeight = (canvasWidth * 0.2) * (9 / 16);
            }

            // Position basierend auf Option berechnen
            if (position === 'Custom' && options.overlayCustomPosition) {
                try {
                    const customPos = typeof options.overlayCustomPosition === 'string'
                        ? JSON.parse(options.overlayCustomPosition)
                        : options.overlayCustomPosition;

                    x = customPos.left.cssValue.includes('%')
                        ? (canvasWidth * parseFloat(customPos.left.cssValue) / 100)
                        : parseFloat(customPos.left.cssValue);

                    y = customPos.top.cssValue.includes('%')
                        ? (canvasHeight * parseFloat(customPos.top.cssValue) / 100)
                        : parseFloat(customPos.top.cssValue);

                } catch (e) {
                    console.warn('Fehler beim Parsen der Custom Position:', e);
                    x = 20;
                    y = canvasHeight - overlayHeight - 20;
                }
            } else {
                switch (position) {
                    case 'Center':
                        x = (canvasWidth - overlayWidth) / 2;
                        y = (canvasHeight - overlayHeight) / 2;
                        break;
                    case 'CenterLeft':
                        x = 20;
                        y = (canvasHeight - overlayHeight) / 2;
                        break;
                    case 'CenterRight':
                        x = canvasWidth - overlayWidth - 20;
                        y = (canvasHeight - overlayHeight) / 2;
                        break;
                    case 'TopCenter':
                        x = (canvasWidth - overlayWidth) / 2;
                        y = 20;
                        break;
                    case 'TopLeft':
                        x = 20;
                        y = 20;
                        break;
                    case 'TopRight':
                        x = canvasWidth - overlayWidth - 20;
                        y = 20;
                        break;
                    case 'BottomCenter':
                        x = (canvasWidth - overlayWidth) / 2;
                        y = canvasHeight - overlayHeight - 20;
                        break;
                    case 'BottomLeft':
                        x = 20;
                        y = canvasHeight - overlayHeight - 20;
                        break;
                    case 'BottomRight':
                    default:
                        x = canvasWidth - overlayWidth - 20;
                        y = canvasHeight - overlayHeight - 20;
                        break;
                }
            }

            return { x, y, width: overlayWidth, height: overlayHeight };
        };

        // Optimiertes Picture-in-Picture Rendering
        let lastDrawTime = 0;
        const draw = (timestamp) => {
            if (!this.recordings[id]) {
                return;
            }
            if (!lastDrawTime || (timestamp - lastDrawTime) >= frameInterval) {
                lastDrawTime = timestamp;
                ctx.drawImage(mainVideo, 0, 0, canvas.width, canvas.height);
                // Overlay Position und Größe berechnen
                const overlay = calculateOverlayPosition(
                    options.overlayPosition,
                    options.overlaySize,
                    canvas.width,
                    canvas.height
                );

                // Kamera als Overlay zeichnen
                ctx.drawImage(overlayVideo,
                    overlay.x,
                    overlay.y,
                    overlay.width,
                    overlay.height
                );
            }
            requestAnimationFrame(draw);
        };
        requestAnimationFrame(draw);

        // Canvas als Stream mit begrenzter Frame Rate
        const canvasStream = canvas.captureStream(frameRate);

        // Gemischten Audio Stream hinzufügen
        if (mixedAudioStream) {
            mixedAudioStream.getAudioTracks().forEach(track => canvasStream.addTrack(track));
        }

        return {
            combinedStream: canvasStream,
            canvas: {
                element: canvas,
                stream: canvasStream,
                videos: [mainVideo, overlayVideo]
            }
        };
    }

    static async getAudioStreams(audioDevices) {
        if (!audioDevices || audioDevices.length === 0) return [];

        const audioStreams = await Promise.all(
            audioDevices.map(async device => {
                try {
                    const deviceId = typeof device === 'string' ? device : device.deviceId;
                    const audioConstraints = typeof device === 'string' ? {} : device;

                    return await navigator.mediaDevices.getUserMedia(this.prepareAudioConstraints(deviceId, audioConstraints));
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
            // Entferne Audio-Tracks vom VideoStream, wenn vorhanden
            videoStream.getAudioTracks().forEach(track => track.stop());
        }
        if (audioStream) {
            audioStream.getAudioTracks().forEach(track => combinedStream.addTrack(track));
        }
        return combinedStream;
    }

    static mixAudioStreams(audioStreams) {
        if (!audioStreams || audioStreams.length === 0) return { stream: null, audioContext: null };

        const audioContext = new (window.AudioContext || window.webkitAudioContext)();
        const destination = audioContext.createMediaStreamDestination();

        audioStreams.forEach(stream => {
            const source = audioContext.createMediaStreamSource(stream);
            source.connect(destination);
        });

        return { stream: destination.stream, audioContext };
    }

    static generateUniqueId() {
        return `${new Date().getTime()}`;
    }

    static async saveVideoData(chunks, callback, id, options) {
        const { screen, camera, audio, systemAudio, combined } = chunks;

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
            systemAudioData: await createBlobData(systemAudio, options.audioContentType || 'audio/webm'),
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
        try {
            await navigator.mediaDevices.getUserMedia({ video: false, audio: true });
        } catch (e) { }
        const devices = await navigator.mediaDevices.enumerateDevices();
        return devices.filter(device => device.kind === 'audioinput');
    }

    static async getAvailableVideoDevices() {
        try {
            await navigator.mediaDevices.getUserMedia({ video: true, audio: false });
        } catch (e) { }
        const devices = await navigator.mediaDevices.enumerateDevices();
        return devices.filter(device => device.kind === 'videoinput');
    }
}

window.MudExCapture = MudExCapture;

class MudExColorHelper {

    static ensureHex(v) {
        try {
            if (!this.isHex(v) && v.toLowerCase().startsWith('rgb')) {
                v = this.rgbaToHex(v);
            }
            return v;
        }
        catch (e) {
            return v;
        }
    }
    
    static isHex(h) {
        if (!h) {
            return false;
        } 
        h = h.replace('#', '');
        var a = parseInt(h, 16);
        return (a.toString(16) === h);
    }
    
    static hexToRgbA(hex) {
        try {
            var c;
            if (/^#([A-Fa-f0-9]{3}){1,2}$/.test(hex)) {
                c = hex.substring(1).split('');
                if (c.length == 3) {
                    c = [c[0], c[0], c[1], c[1], c[2], c[2]];
                }
                c = '0x' + c.join('');
                return 'rgba(' + [(c >> 16) & 255, (c >> 8) & 255, c & 255].join(',') + ',1)';
            }
        }
        catch (error) {
            return '';
        }
        return '';
    }
    
    static rgbaToHex(orig) {
        try {
            var a, isPercent, rgb = orig.replace(/\s/g, '').match(/^rgba?\((\d+),(\d+),(\d+),?([^,\s)]+)?/i), alpha = (rgb && rgb[4] || "").trim(), hex = rgb ?
                (rgb[1] | 1 << 8).toString(16).slice(1) +
                (rgb[2] | 1 << 8).toString(16).slice(1) +
                (rgb[3] | 1 << 8).toString(16).slice(1) : orig;
            if (alpha !== "") {
                a = alpha;
            }
            else {
                //a = 01;
                a = 0x1;
            }
            a = ((a * 255) | 1 << 8).toString(16).slice(1);
            hex = hex + a;
            return hex;
        }
        catch (error) {
            return '';
        }
    }
    
    static isTransparent(v) {
        return v && (v.toLowerCase() === 'transparent' || v.includes('NaN'));
    }
    
    static isNone(v) {
        return v && (v.toLowerCase() === 'none' || v.includes('ed'));
    }
    
    static isTransparentOrNone(v) {
        return this.isNone(v) || this.isTransparent(v);
    }
    
    static argbToHex (color) {
        return '#' + ('000000' + (color & 0xFFFFFF).toString(16)).slice(-6);
    }

    static hexToArgb (hexColor) {
        return parseInt(hexColor.replace('#', 'FF'), 16) << 32;
    }

    static hexToHsl(hex) {
        return this.rgbToHsl(this.hexToRgb(hex));
    }

    static hslToHex(hsl) {
        return this.rgbToHex(this.hslToRgb(hsl));
    }

    static setLuminance (hex, luminance) {
        if (!hex) {
            return hex;
        }
        var hsl = this.rgbToHsl(this.hexToRgb(hex));
        hsl.l = Math.max(Math.min(1, luminance), 0);
        return this.rgbToHex(this.hslToRgb(hsl));
    }

    static hexToRgb (hex) {
        const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);

        return result ? {
            r: parseInt(result[1], 16),
            g: parseInt(result[2], 16),
            b: parseInt(result[3], 16)
        } : null;
    }

    static rgbIntToHex (rgb) {
        let hex = Number(rgb).toString(16);

        if (hex.length < 6) {
            hex = '000000'.substring(hex.length) + hex;
        }
        return hex;
    }

    static hslToRgb (hsl) {
        const h = hsl.h,
            s = hsl.s,
            l = hsl.l;
        let hue2rgb,
            r,
            g,
            b,
            p,
            q;

        if (s === 0) {
            r = g = b = l;
        } else {
            hue2rgb = function (p, q, t) {
                if (t < 0) {
                    t += 1;
                }
                if (t > 1) {
                    t -= 1;
                }
                if (t < 1 / 6) {
                    return p + (q - p) * 6 * t;
                }
                if (t < 1 / 2) {
                    return q;
                }
                if (t < 2 / 3) {
                    return p + (q - p) * (2 / 3 - t) * 6;
                }
                return p;
            };

            q = l < 0.5 ? l * (1 + s) : l + s - l * s;
            p = 2 * l - q;
            r = hue2rgb(p, q, h + 1 / 3);
            g = hue2rgb(p, q, h);
            b = hue2rgb(p, q, h - 1 / 3);
        }

        return {
            r: MudExNumber.constrain(Math.round(r * 255), 0, 255),
            g: MudExNumber.constrain(Math.round(g * 255), 0, 255),
            b: MudExNumber.constrain(Math.round(b * 255), 0, 255)
        };
    }

    static rgbToHsl (rgb) {
        if (!rgb) {
            return rgb;
        }

        const r = rgb.r / 255,
            g = rgb.g / 255,
            b = rgb.b / 255,
            max = Math.max(r, g, b),
            min = Math.min(r, g, b),
            l = (max + min) / 2;
        let h,
            s,
            d;

        if (max === min) {
            h = s = 0;
        } else {
            d = max - min;
            s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            switch (max) {
            case r:
                h = (g - b) / d + (g < b ? 6 : 0);
                break;
            case g:
                h = (b - r) / d + 2;
                break;
            case b:
                h = (r - g) / d + 4;
                break;
            }
            h /= 6;
        }

        return { h: h, s: s, l: l };
    }

    static rgbToHex (rgb) {
        const tohex = function (value) {
            const hex = value.toString(16);

            return hex.length === 1 ? '0' + hex : hex;
        };

        return '#' + tohex(rgb.r) + tohex(rgb.g) + tohex(rgb.b);
    }

    static getOptimalForegroundColor (backgroundColor, lightforegroundResult, darkforegroundResult) {
        if (this.perceivedBrightness(backgroundColor) > 190) {
            return darkforegroundResult || '#000000';
        }
        return lightforegroundResult || '#FFFFFF';
    }

    static perceivedBrightness (color) {
        if (!color) {
            return 0;
        }
        if (typeof color !== 'object') {
            color = this.hexToRgb(color);
        }
        return Math.sqrt(color.r * color.r * 0.241 + color.g * color.g * 0.691 + color.b * color.b * 0.068);
    }
    
}
window.MudExColorHelper = MudExColorHelper;
class MudExCssHelper {
    static getCssVariables() {
        // Get CSS variables from stylesheets
        const sheetVariables = Array.from(document.styleSheets)
            .filter(sheet => sheet.href === null || sheet.href.startsWith(window.location.origin))
            .reduce((acc, sheet) => (acc = [...acc, ...Array.from(sheet.cssRules).reduce((def, rule) => (def = rule.selectorText === ":root"
                ? [...def, ...Array.from(rule.style).filter(name => name.startsWith("--"))]
                : def), [])]), []);

        // Get CSS variables from inline styles
        const inlineStyles = document.documentElement.style;
        const inlineVariables = Array.from(inlineStyles).filter(name => name.startsWith("--"));

        // Combine and remove duplicates
        const allVariables = Array.from(new Set([...sheetVariables, ...inlineVariables]));

        return allVariables.map(name => ({ key: name, value: this.getCssVariableValue(name) }));
    }
    
    static findCssVariables(value) {
        value = value.toLowerCase();
        const helper = MudExColorHelper;//window[window['___appJsNameSpace']]['ColorHelper'];
        return this.getCssVariables().filter(v => v.value.toLowerCase().includes(value) || helper.ensureHex(v.value).includes(helper.ensureHex(value)));
    }
    
    static getCssVariableValue(varName) {
        if (!varName.startsWith('--')) {
            varName = '--' + varName;
        }
        return getComputedStyle(document.documentElement).getPropertyValue(varName).trim();
    }
    
    static setCssVariableValue(varName, value) {
        if (!varName.startsWith('--')) {
            varName = '--' + varName;
        }
        document.documentElement.style.setProperty(varName, value);
    }

    static setElementAppearance(selector, className, style, keepExisting) {
        var element = document.querySelector(selector);
        this.setElementAppearanceOnElement(element, className, style, keepExisting);

    }

    static setElementAppearanceOnElement(element, className, style, keepExisting) {
        if (element) {      
            element.className = keepExisting && element.className ? element.className + ' ' + className : className;
            element.style.cssText = keepExisting && element.style.cssText ? element.style.cssText + ' ' + style : style;
        }
    }

    static removeElementAppearance(selector, className, style) {
        var element = document.querySelector(selector);
        this.removeElementAppearanceOnElement(element, className, style);
    }

    static removeElementAppearanceOnElement(element, className, style) {
        if (element) {
            if (className) {
                element.classList.remove(className);
            }
            if (style) {
                let styles = element.style.cssText.split(';')
                    .map(s => s.trim())
                    .filter(s => s !== style && !s.startsWith(style.split(':')[0]))
                    .join('; ');
                element.style.cssText = styles;
            }
        }
    }

    static createTemporaryClass(style, className) {
        className = className || 'class_' + Math.random().toString(36).substr(2, 9);

        // Remove leading dot if exists
        className = className.startsWith('.') ? className.slice(1) : className;

        let styleElement = document.getElementById('mud-ex-dynamic-styles');

        if (!styleElement) {
            styleElement = document.createElement('style');
            styleElement.type = 'text/css';
            styleElement.id = 'mud-ex-dynamic-styles';
            document.head.appendChild(styleElement);
        }

        styleElement.sheet.insertRule('.' + className + ' { ' + style + ' }', 0);

        return className;
    }

    static deleteClassRule(className) {
        let styleElement = document.getElementById('mud-ex-dynamic-styles');
        if (!styleElement) {
            return;
        }
        let styleSheet = styleElement.sheet;
        var rules = styleSheet.cssRules || styleSheet.rules;

        // Remove leading dot if exists
        className = className.startsWith('.') ? className.slice(1) : className;

        for (var i = 0; i < rules.length; i++) {
            if (rules[i].selectorText === '.' + className) {
                if (styleSheet.deleteRule) {
                    styleSheet.deleteRule(i);
                } else if (styleSheet.removeRule) {
                    styleSheet.removeRule(i);
                }
                break;
            }
        }
    }
    
}
window.MudExCssHelper = MudExCssHelper;
class MudExDomHelper {
    dom;
    
    constructor(dom) {
        this.dom = dom;
    }

    _asArray(val) {
        return Array.isArray(val) ? val : [val];
    }

    down(q) {
        var d = this.dom.querySelector(q);
        return d ? new MudExDomHelper(d) : null;
    }

    isVisible(deep) {
        if (!deep) {
            return this.dom.style.display !== 'none' && this.dom.style.visibility !== 'hidden';
        }
        var element = this.dom;
        if (element.offsetWidth === 0 || element.offsetHeight === 0) return false;
        var height = document.documentElement.clientHeight,
            rects = element.getClientRects(),
            onTop = function (r) {
                var x = (r.left + r.right) / 2, y = (r.top + r.bottom) / 2;
                return document.elementFromPoint(x, y) === element;
            };
        for (var i = 0, l = rects.length; i < l; i++) {
            var r = rects[i],
                inViewport = r.top > 0 ? r.top <= height : (r.bottom > 0 && r.bottom <= height);
            if (inViewport && onTop(r)) return true;
        }
        return false;
    }

    setStyle(styles) {
        Object.assign(this.dom.style, styles);
        return this;
    }
    
    getStyleValue(name) {
        return this.dom.style.getPropertyValue(name);
    }

    addCls(names) {
        this._asArray(names).forEach((css) => this.dom.classList.add(css));
        return this;
    }

    removeCls(names) {
        this._asArray(names).forEach((css) => this.dom.classList.remove(css));
        return this;
    }

    toggleCls(names) {
        this._asArray(names).forEach((css) => {
            if (this.dom.classList.contains(css)) {
                this.removeCls(css);
            } else {
                this.addCls(css);
            }
        });
        return this;
    }

    show() {
        this.dom.style.display = '';
        return this;
    }

    hide(animate) {
        this.dom.style.display = 'none';
        return this;
    }

    getBounds() { return this.dom.getBoundingClientRect(); }
    getWidth() { return this.getBounds().width; }
    getHeight() { return this.getBounds().height; }
    getX() { return this.getXY()[0]; }
    getY() { return this.getXY()[1]; }
    getXY() {
        var round = Math.round,
            body = document.body,
            dom = this.dom,
            x = 0,
            y = 0,
            bodyRect, rect;

        if (dom !== document && dom !== body) {
            // IE (including IE10) throws an error when getBoundingClientRect
            // is called on an element not attached to dom
            try {
                bodyRect = body.getBoundingClientRect();
                rect = dom.getBoundingClientRect();

                x = rect.left - bodyRect.left;
                y = rect.top - bodyRect.top;
            }
            catch (ex) {
                // This block is intentionally left blank
            }
        }
        return [round(x), round(y)];
    }

    forcefocus() {
        this.dom.focus({ focusVisible: true });        
    }

    focusDelayed(delay) {
        this.forcefocus();
        setTimeout(() => this.forcefocus(), delay || 50);        
        return this;
    }
    
    static focusElementDelayed(selectorOrElement, delay, eventToStop) {
        let result = this.create(selectorOrElement);
        if (eventToStop) {
            eventToStop.stopPropagation();
            eventToStop.preventDefault();
        }
        return result ? result.focusDelayed(delay) : null;
    }

    static ensureElement(selectorOrElement) {
        return typeof selectorOrElement === 'string' ?
            document.querySelector(selectorOrElement) : selectorOrElement;
    }

    static create(selectorOrElement) {
        let el = this.ensureElement(selectorOrElement);
        return el ? new MudExDomHelper(el) : null;
    }

    static toRelative(element, useWindowAsReference = false) {

        if (!element || !(element instanceof HTMLElement)) {
            console.error("Das angegebene Element ist ungültig.");
            return;
        }

        const parent = element.offsetParent;
        const referenceWidth = useWindowAsReference || !parent ? window.innerWidth : parent.offsetWidth;
        const referenceHeight = useWindowAsReference || !parent ? window.innerHeight : parent.offsetHeight;

        if (!referenceWidth || !referenceHeight) {
            console.warn("No reference to set relatives");
            return;
        }

        const computedStyle = getComputedStyle(element);
        const leftPx = parseFloat(computedStyle.left);
        const topPx = parseFloat(computedStyle.top);
        const widthPx = parseFloat(computedStyle.width);
        const heightPx = parseFloat(computedStyle.height);

        const leftPercent = (leftPx / referenceWidth) * 100;
        const topPercent = (topPx / referenceHeight) * 100;
        const widthPercent = (widthPx / referenceWidth) * 100;
        const heightPercent = (heightPx / referenceHeight) * 100;

        element.style.left = `${leftPercent}%`;
        element.style.top = `${topPercent}%`;
        
        if (element.style.width && element.style.width !== 'auto') {
            element.style.width = `${widthPercent}%`;
        }
        if (element.style.height && element.style.height !== 'auto') {
            element.style.height = `${heightPercent}%`;
        }

    }

    static toAbsolute(element, sizesAuto) {
        element.style.position = 'absolute';
        var rect = element.getBoundingClientRect();
        element.style.left = rect.left + "px";
        element.style.top = rect.top + "px";

        if (sizesAuto) {
            element.style.width = 'auto';
           // element.style.height = 'auto';
        } else {
            element.style.width = rect.width + "px";
           // element.style.height = rect.height + "px";
        }
    }

    static ensureElementIsInScreenBounds(element) {
        var rect = element.getBoundingClientRect();
        var rectIsEmpty = rect.width === 0 && rect.height === 0;
        if (rectIsEmpty) {
            const ro = new ResizeObserver(entries => {
                ro.disconnect();
                this.ensureElementIsInScreenBounds(element);
            });

            ro.observe(element);
            return;
        }

        var animationIsRunning = !!element.getAnimations().length;
        if (animationIsRunning) {
            element.addEventListener('animationend', (e) => this.ensureElementIsInScreenBounds(element), { once: true });
            return;
        }
        if (rect.left < 0) {
            element.style.left = '0px';
        }
        if (rect.top < 0) {
            element.style.top = '0px';
        }
        if (rect.right > window.innerWidth) {
            element.style.left = (window.innerWidth - element.offsetWidth) + 'px';
        }
        if (rect.bottom > window.innerHeight) {
            element.style.top = (window.innerHeight - element.offsetHeight) + 'px';
        }
    }

    static syncSize(main, target, options, callbackRef) {
        options = options || { width: true, height: false, live: true, minWidth: 'auto' };
        var mainEl = MudExObserver.getElement(main);
        var sourceEl = MudExObserver.getElement(target);
        mainEl = mainEl?.parentElement || mainEl;

        if (!mainEl || !sourceEl) return;

        var sourceRect = sourceEl.getBoundingClientRect();
        //var computedStyle = window.getComputedStyle(sourceEl);
        //var initialWidth = computedStyle.width;
        //var initialHeight = computedStyle.height;
        var initialWidth = sourceRect.width + 'px';
        var initialHeight = sourceRect.height + 'px';

        if (options.minWidth === 'auto') {
            options.minWidth = initialWidth;
        }
        if (options.minHeight === 'auto') {
            options.minHeight = initialHeight;
        }

        function updateSize() {
            var rect = mainEl.getBoundingClientRect();
            if (options.width) {
                var width = rect.width;
                sourceEl.style.width = width + 'px';
                if (options.minWidth) {
                    sourceEl.style.minWidth = options.minWidth;
                }
            }
            if (options.height) {
                var height = rect.height;
                sourceEl.style.height = height + 'px';
                if (options.minHeight) {
                    sourceEl.style.minHeight = options.minHeight;
                }
            }
            if (callbackRef) {
                callbackRef.invokeMethodAsync('OnSyncResized', rect, null, null); // TODO: pass mainEl and sourceEl
            }
        }

        updateSize();

        if (options.live) {
            var resizeObserver = new ResizeObserver(() => {
                if (!mainEl.isConnected) {
                    resizeObserver.disconnect();
                    return;
                }
                updateSize();
            });
            resizeObserver.observe(mainEl);
        }
    }

    
}

window.MudExDomHelper = MudExDomHelper;
class MudExEventHelper {
    static isWithin(event, element) {
        if (!element || !event) {
            return false;
        }
        let rect = element.getBoundingClientRect();
        return (event.clientX > rect.left &&
        event.clientX < rect.right &&
        event.clientY < rect.bottom &&
        event.clientY > rect.top);
    }

    static clickElementById(elementId) {
        var retries = 5;
        function tryClick() {
            var elem = document.querySelector('#' + elementId) || document.getElementById(elementId);
            if (elem) {
                elem.click();
            } else if (retries > 0) {
                retries--;
                setTimeout(tryClick, 100);  // try again after 100ms
            }
        }
        tryClick();
    }

    static stringifyEvent(e) {
        const obj = {};
        for (let k in e) {
            obj[k] = e[k];
        }
        return JSON.stringify(obj, (k, v) => {
            if (v instanceof Node)
                return 'Node';
            if (v instanceof Window)
                return 'Window';
            return v;
        }, ' ');
    }

    static stopFor(e, element, milliseconds) {
        if (e === undefined || e === null)
            return;
        e.__internalDispatched = true;
        e.preventDefault();
        e.stopPropagation();
        e.stopImmediatePropagation();
        if (milliseconds) {
            setTimeout(() => {
                //const newEvent = new MouseEvent('click', {
                //    bubbles: true,
                //    cancelable: true,
                //    view: window
                //});

                element.dispatchEvent(e);
            }, milliseconds);
        }
    }

    static cloneEvent(e, serializable) {
        if (serializable) {
            return JSON.parse(this.stringifyEvent(event));
        }
        if (e === undefined || e === null)
            return undefined;
        function ClonedEvent() { }
        ;
        let clone = new ClonedEvent();
        for (let p in e) {
            let d = Object.getOwnPropertyDescriptor(e, p);
            if (d && (d.get || d.set))
                Object.defineProperty(clone, p, d);
            else
                clone[p] = e[p];
        }
        Object.setPrototypeOf(clone, e);
        return clone;
    }
}

window.MudExEventHelper = MudExEventHelper;
class MudExNumber {
    static constrain(number, min, max) {
        var x = parseFloat(number);
        if (min === null) {
            min = number;
        }

        if (max === null) {
            max = number;
        }
        return (x < min) ? min : ((x > max) ? max : x);
    }

    static async md5(email) {
        return Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256', new TextEncoder().encode(email.trim().toLowerCase()))))
            .map(b => b.toString(16).padStart(2, '0'))
            .join('');
    }

}

window.MudExNumber = MudExNumber;
class MudExObserver {
    static observer = null;
    static observedElements = new Map();

    static initObserver() {
        if (MudExObserver.observer == null) {
            MudExObserver.observer = new IntersectionObserver((entries) => {
                entries.forEach(async entry => {
                    const dotNetHelper = MudExObserver.observedElements.get(entry.target);
                    const applyStylesFirst = entry.target.getAttribute('data-apply-styles-first') === 'true';
                    const isVirtualized = entry.target.getAttribute('data-virtualize') === 'true';

                    if (dotNetHelper && (!applyStylesFirst || !isVirtualized)) {
                        await dotNetHelper.invokeMethodAsync('OnVisibilityChanged', entry.isIntersecting);
                    }

                    if (!isVirtualized) return;

                    const intersectStyle = entry.target.getAttribute('data-intersect-style') || '';
                    const noIntersectStyle = entry.target.getAttribute('data-no-intersect-style') || '';
                    const intersectClass = entry.target.getAttribute('data-intersect-class') || '';
                    const noIntersectClass = entry.target.getAttribute('data-no-intersect-class') || '';

                    entry.target.setAttribute('data-intersecting', entry.isIntersecting);

                    if (entry.isIntersecting) {
                        entry.target.style.cssText += intersectStyle;
                        entry.target.style.cssText = entry.target.style.cssText.replace(noIntersectStyle, '');
                        if(intersectClass)
                            entry.target.classList.add(...intersectClass.split(' '));
                        if(noIntersectClass)
                            entry.target.classList.remove(...noIntersectClass.split(' '));
                    }
                    else {
                        entry.target.style.cssText += noIntersectStyle;
                        entry.target.style.cssText = entry.target.style.cssText.replace(intersectStyle, '');
                        if(noIntersectClass)
                            entry.target.classList.add(...noIntersectClass.split(' '));
                        if(intersectClass)
                            entry.target.classList.remove(...intersectClass.split(' '));
                    }

                    if (dotNetHelper && applyStylesFirst) {
                        await dotNetHelper.invokeMethodAsync('OnVisibilityChanged', entry.isIntersecting);
                    }

                });
            });
        }
    }



    static observeVisibility(elementOrSelector, dotNetHelper) {
        MudExObserver.initObserver();

        const element = MudExObserver.getElement(elementOrSelector);
        if (!element) return;

        MudExObserver.observedElements.set(element, dotNetHelper);
        MudExObserver.observer.observe(element);
    }

    static unObserveVisibility(elementOrSelector) {
        const element = MudExObserver.getElement(elementOrSelector);
        if (!element || !MudExObserver.observedElements.has(element)) return;

        MudExObserver.observer.unobserve(element);
        MudExObserver.observedElements.delete(element);
    }

    static getElement(elementOrSelector) {
        if (typeof elementOrSelector === 'string') {
            return document.querySelector(elementOrSelector) || document.getElementById(elementOrSelector);
        } else if (elementOrSelector instanceof Element) {
            return elementOrSelector;
        } else {
            return null;
        }
    }
}

window.MudExObserver = MudExObserver;
class MudExSpeechRecognition {
    static recordings = {};

    static async startRecording(options, callback) {
        if (!this.isSupported()) {
            console.error("Speech Recognition or MediaRecorder is not supported in this browser.");
            return null;
        }        
        const id = this.generateUniqueId();
        const { recognition, stream } = await this.setupRecognition(options, id, callback);
        const mediaRecorder = this.setupMediaRecorder(stream, callback, id);

        recognition.onresult = (event) => this.handleRecognitionResult(event, id, callback);
        recognition.onend = () => this.handleRecognitionEnd(mediaRecorder, id, callback);

        mediaRecorder.start();
        recognition.start();

        this.recordings[id] = { recognition, mediaRecorder, options };
        return id;
    }

    static isSupported() {
        return 'webkitSpeechRecognition' in window && navigator.mediaDevices && navigator.mediaDevices.getUserMedia;
    }

    static generateUniqueId() {
        return `${new Date().getTime()}`;
    }

    static async setupRecognition(options, id, callback) {
        const recognition = new webkitSpeechRecognition();
        if (options?.lang) { 
            recognition.lang = options.lang;
        }
        recognition.continuous = options.continuous;
        recognition.interimResults = options.interimResults;

        var deviceId = typeof options?.device === 'string' ? options.device : options.device?.deviceId;
        
        const audioOptions = deviceId ? { audio: { deviceId: { exact: deviceId } } } : { audio: true };
        const stream = await navigator.mediaDevices.getUserMedia(audioOptions);
        return { recognition, stream };
    }

    static async getAvailableAudioDevices() {
        try {
            await navigator.mediaDevices.getUserMedia({ video: false, audio: true });
        } catch (e) { } 
        const devices = await navigator.mediaDevices.enumerateDevices();
        return devices.filter(device => device.kind === 'audioinput');
    }

    static setupMediaRecorder(stream, callback, id) {
        const mediaRecorder = new MediaRecorder(stream);
        const audioChunks = [];

        mediaRecorder.ondataavailable = event => audioChunks.push(event.data);
        mediaRecorder.onstop = async () => this.saveAudioData(audioChunks, callback, id);

        return mediaRecorder;
    }

    static async saveAudioData(audioChunks, callback, id) {
        const audioBlob = new Blob(audioChunks);
        const audioArrayBuffer = await audioBlob.arrayBuffer();
        const audioByteArray = new Uint8Array(audioArrayBuffer);
        
        if (callback['invokeMethodAsync']) {
            callback.invokeMethodAsync('Invoke', { ...this.recordings[id]?.lastParam, audioData: audioByteArray });
        }

        delete this.recordings[id];
    }

    static handleRecognitionResult(event, id, callback) {
        let options = this.recordings[id]?.options;
        let transcript = '';
        let partialTranscript = '';
        let isFinal = false;

        for (let i = event.resultIndex; i < event.results.length; ++i) {
            transcript += (partialTranscript = event.results[i][0].transcript);
            if (event.results[i].isFinal) {
                isFinal = true;
            }
        }

        let result = {
            transcript: transcript,
            transcriptChanges: partialTranscript,
            isFinalResult: isFinal,
            options: options
        };

        if (this.recordings && this.recordings[id]) {
            this.recordings[id].lastParam = result;
        }

        if (!options || !options.finalResultsOnly || isFinal) {
            if (callback['invokeMethodAsync']) {
                callback.invokeMethodAsync('Invoke', result);
            }
        }
    }

    static handleRecognitionEnd(mediaRecorder, id, callback) {
        mediaRecorder.stop();
        if (callback['invokeMethodAsync']) {
            callback.invokeMethodAsync('OnStopped', id);
        }
    }

    static stopRecording(id) {
        const recording = this.recordings[id];
        if (recording) {
            recording.recognition.stop();
            recording.mediaRecorder.stop();
        }
    }

    static stopAllRecordings() {
        Object.keys(this.recordings).forEach(id => this.stopRecording(id));
    }
}

window.MudExSpeechRecognition = MudExSpeechRecognition;

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
class MudExDialogHandlerBase {
    constructor(options, dotNet, dotNetService, onDone) {
        this.options = options;
        this.dotNet = dotNet;
        this.dotNetService = dotNetService;
        this.onDone = onDone;

        this.mudDialogSelector = options.mudDialogSelector || '.mud-dialog:not([data-mud-extended=true])';
        this.mudDialogHeaderSelector = options.mudDialogHeaderSelector || '.mud-dialog-title';
        this._updateDialog(document.querySelector(this.mudDialogSelector));
        this.disposed = false;

    }

    order = 99;

    async raiseDialogEvent(eventName) {        
        // Get viewport dimensions
        var windowHeight = window.innerHeight || document.documentElement.clientHeight;
        var windowWidth = window.innerWidth || document.documentElement.clientWidth;

        // Get scroll positions
        var scrollX = window.pageXOffset || document.documentElement.scrollLeft;
        var scrollY = window.pageYOffset || document.documentElement.scrollTop;

        // Extend the rect object with new properties
        var extendedRect = {
            windowHeight: windowHeight,
            windowWidth: windowWidth,
            scrollX: scrollX,
            scrollY: scrollY
        };
        const rect = Object.assign(extendedRect, JSON.parse(JSON.stringify(this.dialog.getBoundingClientRect())));        
        if (this.dotNetService) {
            return await this.dotNetService.invokeMethodAsync('PublishEvent', eventName, this.dialog.id, this.dotNet, rect);
        }
    }

    isInternalHandler() {
        return this.dialog.getAttribute('data-mud-ex-internal-handler') === 'true';
    }

    setRelativeIf() {
        if (this.options.keepRelations && MudExDomHelper.toRelative) {
            this.dialog.setAttribute('data-mud-ex-internal-handler', 'true');

            var observer = this.getHandler(MudExDialogResizeHandler)?.resizeObserver;
            if (observer) {
                observer.unobserve(this.dialog);
            }
            MudExDomHelper.toRelative(this.dialog);
            if (observer) {
                observer.observe(this.dialog);
            }
            this.removeInternalHandler();
        }
    }

    removeInternalHandler() {
        setTimeout(() => this.dialog.removeAttribute('data-mud-ex-internal-handler'), 100);
    }

    getAnimationDuration() {
        // TODO: 
        return this.options.animationDurationInMs + 150;
    }

    awaitAnimation(callback) {
        setTimeout(() => callback(this.dialog), this.getAnimationDuration());
    }

    handle(dialog) {
        this._updateDialog(dialog);
    }

    handleAll(dialog) {
        const handlers = this.getHandlers();
        handlers.forEach(handlerInstance => {
            handlerInstance.handle(dialog);
            handlerInstance._handlersCache = this._handlersCache;
        });
    }

    dispose() {
        this.disposed = true;
        this._handlersCache.forEach(handlerInstance => {
            if (!handlerInstance.disposed) {
                handlerInstance.dispose();
            }
        });
        delete this._handlersCache;
        delete this.dialog;
        delete this.dialogHeader;
        delete this.dotNet;
        delete this.dotNetService;
        delete this.onDone;
        delete this.options;
        
    }

    getHandlers() {
        if (this._handlersCache) {
            return this._handlersCache;
        }

        const handlerInstances = [];

        for (const key in window) {
            if (window.hasOwnProperty(key) && typeof window[key] === 'function') {
                try {
                    const superClass = Object.getPrototypeOf(window[key].prototype);
                    if (superClass && superClass.constructor === MudExDialogHandlerBase && window[key].prototype.constructor !== this.constructor) {
                        const instance = new window[key](this.options, this.dotNet, this.dotNetService, this.onDone);
                        handlerInstances.push(instance);
                    }
                } catch (error) {
                    // Ignore errors caused by non-class objects
                }
            }
        }

        this._handlersCache = handlerInstances.sort((a, b) => a.order - b.order);
        return handlerInstances;
    }

    getHandler(HandlerClass) {
        return this.getHandlers().find(handlerInstance => handlerInstance instanceof HandlerClass);
    }

    _updateDialog(dialog) {
        this.dialog = dialog || this.dialog;
        if (this.dialog) {
            this.dialog.style.position = 'absolute';
            //this.setRelativeIf();
            this.dialog.options = this.options; 
            this.dialogHeader = this.dialog.querySelector(this.mudDialogHeaderSelector);
            this.dialogTitleEl = this.dialog.querySelector('.mud-dialog-title');
            this.dialogTitle = this.dialogTitleEl ? this.dialogTitleEl.innerText.trim() : '';
            this.dialogId = this.dialog.id;
            this.dialogContainerReference = this.dialog.parentElement;
            if (this.dialogContainerReference) { 
                this.dialogOverlay = this.dialogContainerReference.querySelector('.mud-overlay');
            }
        }
    }
}

window.MudExDialogHandlerBase = MudExDialogHandlerBase;
class MudExDialogAnimationHandler extends MudExDialogHandlerBase {
   
    handle(dialog) {
        super.handle(dialog);
        if (this.options.animations != null && Array.isArray(this.options.animations) && this.options.animations.length) {
            this.animate();
            this.awaitAnimation(() => this.raiseDialogEvent('OnAnimated'));
        } else {
            this.raiseDialogEvent('OnAnimated');
        }        

        this.extendCloseEvents();
        
    }

    async checkCanClose() {
        const callbackName = this.options.canCloseCallbackName;
        const reference = this.options.canCloseCallbackReference || this.dotNet;
        if (callbackName && reference) {
            try {
                const result = await reference.invokeMethodAsync(callbackName);
                if (result === false) {
                    return false;
                }
            } catch (e) {
                console.error(e);
            }
        }
        const closeEvent = await this.raiseDialogEvent('OnDialogClosing');
        if (closeEvent?.cancel === true) {
            return false;
        }
        return true;
    }


    extendCloseEvents() {
        const handleCloseEvent = (element) => {
            const handleClick = async (e) => {
                if (!e.__internalDispatched) {
                    e.__internalDispatched = true;
                    e.preventDefault();
                    e.stopPropagation();
                    const canClose = await this.checkCanClose();
                    if (canClose) {
                        setTimeout(() => {
                            element.dispatchEvent(e);
                        }, 1);
                    }
                    return;
                }
                
                if (this.options.animateClose) {
                    this.closeAnimation();
                    element.removeEventListener('click', handleClick);
                    MudExEventHelper.stopFor(e, element, this.options.animationDurationInMs);
                }
            };
            element.addEventListener('click', handleClick);
        };
        
        const closeButton = this.dialog.querySelector('.mud-button-close');
        if (closeButton) {
            handleCloseEvent(closeButton);
        }

        if (this.dialogOverlay && this.options.modal && this.options.backdropClick !== false) {
            handleCloseEvent(this.dialogOverlay);
        }
    }


    animate() {
       this.dialog.style.animation = `${this.options.animationStyle}`;
    }

    closeAnimation() {
        return MudExDialogAnimationHandler.playCloseAnimation(this.dialog);
    }

    static playCloseAnimation(dialog) {
        if (!dialog) {
            return Promise.resolve();
        }        
        var delay = dialog.options?.animationDurationInMs || 500;        
        dialog.style['animation-duration'] = `${delay}ms`;
        return new Promise((resolve) => {
            MudExDialogAnimationHandler._playCloseAnimation(dialog);
            setTimeout(() => {
                dialog.classList.add('mud-ex-dialog-initial');
                resolve();
            }, delay);
        });
    }

    static _playCloseAnimation(dialog) {        
        const n = dialog.style.animationName;
        dialog.style.animationName = '';
        dialog.style.animationDirection = 'reverse';
        dialog.style['animation-play-state'] = 'paused';
        requestAnimationFrame(() => {
            dialog.style.animationName = n;
            dialog.style['animation-play-state'] = 'running';
        });
    }
    

}


window.MudExDialogAnimationHandler = MudExDialogAnimationHandler;
class MudExDialogButtonHandler extends MudExDialogHandlerBase {

    handle(dialog) {
        super.handle(dialog);
        if (this.options.maximizeButton && this.dialogHeader) {
            this.dialogHeader.addEventListener('dblclick', this.onDoubleClick.bind(this));
        }
        if (this.options.buttons && this.options.buttons.length) {
            var dialogButtonWrapper = document.createElement('div');
            dialogButtonWrapper.classList.add('mud-ex-dialog-header-actions');
            if (!this.options.closeButton) {
                dialogButtonWrapper.style.right = '8px'; // No close button, so we need to move the buttons to the right (48 - button width of 40)
            }

            if (this.dialogHeader) {
                dialogButtonWrapper = this.dialogHeader.insertAdjacentElement('beforeend', dialogButtonWrapper);
            }
            this.options.buttons.reverse().forEach(b => {
                if (dialogButtonWrapper) {
                    dialogButtonWrapper.insertAdjacentHTML('beforeend', b.html);
                    var btnEl = dialogButtonWrapper.querySelector('#' + b.id);
                    btnEl.onclick = () => {
                        if (b.id.indexOf('mud-button-maximize') >= 0) {
                            this.getHandler(MudExDialogPositionHandler).maximize();
                            return;
                        }
                        if (b.id.indexOf('mud-button-minimize') >= 0) {
                            this.getHandler(MudExDialogPositionHandler).minimize();
                            return;

                        } else if (b.callBackReference && b.callbackName) {
                            b.callBackReference.invokeMethodAsync(b.callbackName);
                        }
                    }
                }
            });
        }
    }

    onDoubleClick(e) {
        if (this.dialogHeader && MudExEventHelper.isWithin(e, this.dialogHeader)) {
            this.getHandler(MudExDialogPositionHandler).maximize();
        }
    }

    dispose() {
        if (this.dialogHeader) {
            this.dialogHeader.removeEventListener('dblclick', this.onDoubleClick);
        }
    }
}


window.MudExDialogButtonHandler = MudExDialogButtonHandler;
class MudExDialogDragHandler extends MudExDialogHandlerBase  {
    
    handle(dialog) {
        super.handle(dialog);
        if (this.options.dragMode !== 0 && this.dialog) {
            this.dragElement(this.dialog, this.dialogHeader, document.body, this.options.dragMode === 2);
        }
    }

    dragElement(dialogEl, headerEl, container, disableBoundCheck) {
        const self = this;
        let startPos = { x: 0, y: 0 };
        let cursorPos = { x: 0, y: 0 };
        let startDrag;
        container = container || document.body;

        if (headerEl) {
            headerEl.style.cursor = 'move';
            headerEl.onmousedown = dragMouseDown;
        } else {
            dialogEl.onmousedown = dragMouseDown;
        }

        function dragMouseDown(e) {
            e = e || window.event;
            //e.preventDefault();
            startDrag = true;
            cursorPos = { x: e.clientX, y: e.clientY };
            document.onmouseup = closeDragElement;
            document.onmousemove = elementDrag;
        }

        function elementDrag(e) {
            if (startDrag) {
                startDrag = false;
                self.raiseDialogEvent('OnDragStart');
            }
            e = e || window.event;
            e.preventDefault();

            startPos = {
                x: cursorPos.x - e.clientX,
                y: cursorPos.y - e.clientY,
            };

            cursorPos = { x: e.clientX, y: e.clientY };

            const bounds = {
                x: container.offsetWidth - dialogEl.offsetWidth,
                y: container === document.body ? window.innerHeight - dialogEl.offsetHeight : container.offsetHeight - dialogEl.offsetHeight,
            };

            const newPosition = {
                x: dialogEl.offsetLeft - startPos.x,
                y: dialogEl.offsetTop - startPos.y,
            };

            dialogEl.style.position = 'absolute';

            if (disableBoundCheck || isWithinBounds(newPosition.x, bounds.x)) {
                dialogEl.style.left = newPosition.x + 'px';
            } else if (isOutOfBounds(newPosition.x, bounds.x)) {
                dialogEl.style.left = bounds.x + 'px';
            }

            if (disableBoundCheck || isWithinBounds(newPosition.y, bounds.y)) {
                dialogEl.style.top = newPosition.y + 'px';
            } else if (isOutOfBounds(newPosition.y, bounds.y)) {
                dialogEl.style.top = bounds.y + 'px';
            }
            self.raiseDialogEvent('OnDragging');
        }

        function closeDragElement() {            
            self.raiseDialogEvent('OnDragEnd');
            self.setRelativeIf();
            document.onmouseup = null;
            document.onmousemove = null;
        }

        function isWithinBounds(value, maxValue) {
            return value >= 0 && value <= maxValue;
        }

        function isOutOfBounds(value, maxValue) {
            return value > maxValue;
        }
    }
    
}


window.MudExDialogDragHandler = MudExDialogDragHandler;
class MudExDialogFinder {
    constructor(options) {
        this.options = options;
        this.mudDialogSelector = options.mudDialogSelector || '.mud-dialog:not([data-mud-extended=true])';
    }

    findDialog() {
        return Array.from(document.querySelectorAll(this.mudDialogSelector)).find(d => !d.__extended);
    }

    observeDialog(callback) {
        const observer = new MutationObserver((mutations) => {
            const dialog = this.findDialog();
            if (dialog) {
                const addedDialogMutation = mutations.find(mutation => mutation.addedNodes[0] === dialog);

                if (addedDialogMutation) {
                    observer.disconnect();
                    callback(dialog);
                }
            }
        });

        observer.observe(document, { characterData: true, childList: true, subtree: true });
    }
}

window.MudExDialogFinder = MudExDialogFinder;
class MudExDialogHandler extends MudExDialogHandlerBase {
    handle(dialog) {
        setTimeout(() => {
            super.handle(dialog);
            setTimeout(() => {
                dialog.classList.remove('mud-ex-dialog-initial');
            }, 50);
            dialog.__extended = true;
            dialog.setAttribute('data-mud-extended', true);
            dialog.classList.add('mud-ex-dialog');
            this.handleAll(dialog);           
            this.awaitAnimation(() => {
                window.MudBlazorExtensions.focusAllAutoFocusElements();
            });
            if (this.onDone) this.onDone();
        }, 50);
    }
}

window.MudExDialogHandler = MudExDialogHandler;
class MudExDialogNoModalHandler extends MudExDialogHandlerBase {

    static handled = [];

    handle(dialog) {
        super.handle(dialog);

        if (!this.options.modal) {
            this._updateHandledDialogs(dialog);
            this._modifyDialogAppearance();
            this.dialog.onmousedown = this._handleDialogMouseDown.bind(this);
        }
    }

    _updateHandledDialogs(dialog) {
        const index = MudExDialogNoModalHandler.handled.findIndex(h => h.id === dialog.id);
        if (index !== -1) {
            MudExDialogNoModalHandler.handled.splice(index, 1);
        }

        MudExDialogNoModalHandler.handled.push({
            id: this.dialog.id,
            dialog: this.dialog,
            options: this.options,
            dotNet: this.dotNet
        });
    }

    _modifyDialogAppearance() {
        this.changeCls();
        this.awaitAnimation(() => {
            this.dialog.style['animation-duration'] = '0s';
            MudExDomHelper.toAbsolute(this.dialog, false);
            Object.assign(this.dialogContainerReference.style, {
                'pointer-events': 'none'
            });
            Object.assign(this.dialog.style, {
                'pointer-events': 'all'
            });
        });
    }

    _handleDialogMouseDown(e) {
        if (!this.dialogHeader || !Array.from(this.dialogHeader.querySelectorAll('button')).some(b => MudExEventHelper.isWithin(e, b))) {
            MudExDialogNoModalHandler.bringToFront(this.dialogContainerReference);
        }
    }

    
    changeCls() {
        this.dialog.classList.add('mudex-dialog-no-modal');
        this.dialogContainerReference.classList.add('mudex-dialog-ref-no-modal');
        this.dialogContainerReference.setAttribute('data-modal', false);
        this.dialogContainerReference.setAttribute('data-dialog-id', this.dialog.id);
        this.dialogOverlay.remove();
    }

    static closeNonModal(dialogIdStr) {
        return;
        const dialog = document.querySelector(`#${dialogIdStr}`);
        this.reInitOtherDialogs(dialogIdStr);
    }

    static reInitOtherDialogs(exceptId) {
        const dialogsToReInit = Array.from(document.querySelectorAll('.mud-ex-dialog-initial'))
            .filter(d => d.id !== exceptId && d.getAttribute('data-mud-extended') !== 'true');

        dialogsToReInit.forEach(this.reInitDialog.bind(this));
    }

    static reInitDialog(d) {
        const dialogInfo = MudExDialogNoModalHandler.handled.find(h => h.id === d.id);
        if (dialogInfo) {
            const currentStyle = d.style;
            const savedPosition = {
                top: currentStyle.top,
                left: currentStyle.left,
                width: currentStyle.width,
                height: currentStyle.height,
                position: currentStyle.position
            };
            d.style.display = 'none';

            const handleInfo = { ...dialogInfo, options: { ...dialogInfo.options, animations: null } };
            const index = MudExDialogNoModalHandler.handled.indexOf(dialogInfo);
            MudExDialogNoModalHandler.handled.splice(index, 1);

            const handler = new MudExDialogHandler(handleInfo.options, handleInfo.dotNet, handleInfo.dotNetService, handleInfo.onDone);
            handler.handle(d);

            d.style.display = 'block';
            Object.assign(d.style, savedPosition);
        }
    }

    static bringToFront(dialogContainterRef) {
        if (!dialogContainterRef) return;
        if (dialogContainterRef.getAttribute('role') === 'dialog') {
            dialogContainterRef = dialogContainterRef.parentNode;
        }
        const all = MudExDialogNoModalHandler.getAllDialogReferences();
        let maxZ = 1400;
        for (const d of all) {
            const z = parseInt(window.getComputedStyle(d).zIndex) || 999;
            if (z > maxZ) {
                maxZ = z;
            }
        }
        dialogContainterRef.style.zIndex = (maxZ >= 1400 ? maxZ + 1 : 1400).toString();
    }

    static getDialogReference(dialog) {
        return MudExDialogNoModalHandler.getAllDialogReferences().filter(r => r && r.getAttribute('data-dialog-id') === dialog.id)[0] || dialog.parentElement;
    }

    static getAllDialogReferences() {
        return Array.from(document.querySelectorAll('.mud-dialog-container')).filter(c => c.getAttribute('data-modal') === 'false');
    }

    static getAllNonModalDialogs() {
        return Array.from(document.querySelectorAll('.mudex-dialog-no-modal'));
    }
}

window.MudExDialogNoModalHandler = MudExDialogNoModalHandler;

class MudExDialogPositionHandler extends MudExDialogHandlerBase {

    handle(dialog) {
        super.handle(dialog);        
        if (this.options.showAtCursor) {
            this.moveElementToMousePosition(dialog);
        } else if (this.options.customPosition) {
            this.dialog.style.position = 'absolute';
            this.dialog.style.left = this.options.customPosition.left.cssValue;
            this.dialog.style.top = this.options.customPosition.top.cssValue;
        }
                
        if (this.options.fullWidth && this.options.disableSizeMarginX) {
            this.dialog.classList.remove('mud-dialog-width-full');
            this.dialog.classList.add('mud-dialog-width-full-no-margin');
            if (this.dialog.classList.contains('mud-dialog-width-false')) {
                this.dialog.classList.remove('mud-dialog-width-false');
            }
        }
    }
    

    minimize() {
        let targetElement = document.querySelector(`.mud-ex-task-bar-item-for-${this.dialog.id}`);
        this.moveToElement(this.dialog, targetElement, () => {
            this.dialog.style.visibility = 'hidden';            
        }); 
    }


    moveToElement(sourceElement, targetElement, callback) {
        
        // Get the bounding client rectangles of the target element and the dialog
        var targetRect = targetElement.getBoundingClientRect();
        var dialogRect = sourceElement.getBoundingClientRect();

        // Calculate the scaling factors for width and height
        var scaleX = targetRect.width / dialogRect.width;
        var scaleY = targetRect.height / dialogRect.height;

        // Calculate the translation distances for X and Y
        var translateX = targetRect.left - dialogRect.left;
        var translateY = targetRect.top - dialogRect.top;

        var lastDuration = sourceElement.style['animation-duration'];
        sourceElement.style['animation-duration'] = '.3s';
        // Apply the transformation using the calculated scaling factors and translation distances
        sourceElement.style.transform = `translate(${translateX}px, ${translateY}px) scale(${scaleX}, ${scaleY})`;
        sourceElement.style.transition = 'transform 0.3s ease-in-out';

        // Remove the transition after the animation is done
        setTimeout(() => {
            sourceElement.style.removeProperty('transform');
            sourceElement.style.removeProperty('transition');
            sourceElement.style['animation-duration'] = lastDuration;
            if (callback) callback();
        }, 300);

    }

    restore() {
        this.dialog.style.visibility = 'visible';
    }

    maximize() {
        if (this._oldStyle) {
            this.dialog.style.cssText = this._oldStyle;
            delete this._oldStyle;
        } else {
            this._oldStyle = this.dialog.style.cssText;
            this.dialog.style.position = 'absolute';
            this.dialog.style.left = "0";
            this.dialog.style.top = "0";
            this.dialog.style.maxWidth = this.dialog.style.width = window.innerWidth + 'px';
            this.dialog.style.maxHeight = this.dialog.style.height = window.innerHeight + 'px';
        }
        this.getHandler(MudExDialogResizeHandler).checkResizeable();
    }
    
    moveElementToMousePosition(element) {
        var e = MudBlazorExtensions.getCurrentMousePosition();
        var x = e.clientX;
        var y = e.clientY;
        var origin = this.options.cursorPositionOriginName.split('-');

        var maxWidthFalseOrLargest = this.options.maxWidth === 6 || this.options.maxWidth === 4; // 4=xxl 6=false
        setTimeout(() => {
            if (!this.options.fullWidth || !maxWidthFalseOrLargest) {
                if (origin[1] === 'left') {
                    element.style.left = x + 'px';
                } else if (origin[1] === 'right') {
                    element.style.left = (x - element.offsetWidth) + 'px';
                } else if (origin[1] === 'center') {
                    element.style.left = (x - element.offsetWidth / 2) + 'px';
                }
            }
            if (!this.options.fullHeight) {
                if (origin[0] === 'top') {
                    element.style.top = y + 'px';
                } else if (origin[0] === 'bottom') {
                    element.style.top = (y - element.offsetHeight) + 'px';
                } else if (origin[0] === 'center') {
                    element.style.top = (y - element.offsetHeight / 2) + 'px';
                }
            }
            MudExDomHelper.ensureElementIsInScreenBounds(element);
        }, 50);

    }
}


window.MudExDialogPositionHandler = MudExDialogPositionHandler;
class MudExDialogResizeHandler extends MudExDialogHandlerBase {

    handle(dialog) {
        this.resizeTimeout = null;
        this.resizedSometimes = false;
        super.handle(dialog);
        this.dialog = dialog;
        this.dialog.addEventListener('mousedown', this.onMouseDown.bind(this));
        this.dialog.addEventListener('mouseup', this.onMouseUp.bind(this));
        

        this.resizeObserver = new ResizeObserver(entries => {
            for (let entry of entries) {
                if (entry.target === this.dialog) {
                    if (!this.isInternalHandler()) {
                        if (!this.resizedSometimes && this.mouseDown) {
                            this.resizedSometimes = true;
                            this.setBounds();
                        }
                        this.raiseDialogEvent('OnResizing');
                        this.debounceResizeCompleted();
                    }
                }
            }
        });
        this.awaitAnimation(() => this.checkResizeable());
    }

    onMouseUp() {
        this.mouseDown = false;
    }

    onMouseDown() {
        this.mouseDown = true;
    }

    debounceResizeCompleted() {
        if (this.resizeTimeout) {
            clearTimeout(this.resizeTimeout);
        }
        this.resizeTimeout = setTimeout(() => {
            this.setRelativeIf();
            this.raiseDialogEvent('OnResized');
        }, 500); // debounce
    }

    checkResizeable() {
        MudExDomHelper.toAbsolute(this.dialog, false);
        this.setRelativeIf();
        if (this.options.resizeable) {
            this.resizeObserver.observe(this.dialog);
            this.dialog.style['resize'] = 'both';
            this.dialog.style['overflow'] = 'auto';
        }
    }

    setBounds() {
        if (!this.options.keepMaxSizeConstraints) {
            if (!this.dialog.style.maxWidth)
                this.dialog.style.maxWidth = this.dialog.style.maxWidth || window.innerWidth + 'px';
            if (!this.dialog.style.maxHeight)
                this.dialog.style.maxHeight = this.dialog.style.maxHeight || window.innerHeight + 'px';
        }
        if (!this.dialog.style.minWidth)
            this.dialog.style.minWidth = this.dialog.style.minWidth || '150px';
        if (!this.dialog.style.minHeight)
            this.dialog.style.minHeight = this.dialog.style.minHeight || '150px';
    }

    dispose() {
        if (this.resizeObserver) {
            this.resizeObserver.unobserve(this.dialog);
        }
        if (this.resizeTimeout) {
            clearTimeout(this.resizeTimeout);
        }
        this.dialog.removeEventListener('mousedown', this.onMouseDown);
        this.dialog.removeEventListener('mouseup', this.onMouseUp);
    }
}

window.MudExDialogResizeHandler = MudExDialogResizeHandler;

class MudBlazorExtensionHelper {
    constructor(options, dotNet, dotNetService, onDone) {
        this.dialogFinder = new MudExDialogFinder(options);        
        this.dialogHandler = new MudExDialogHandler(options, dotNet, dotNetService, onDone);
    }

    init() {
        const dialog = this.dialogFinder.findDialog();
        if (dialog) {
            this.dialogHandler.handle(dialog);
        } else {
            this.dialogFinder.observeDialog(dialog => this.dialogHandler.handle(dialog));
        }
    }
}



window.MudBlazorExtensionHelper = MudBlazorExtensionHelper;


window.MudBlazorExtensions = {
    helper: null,
    currentMouseArgs: null,

    __bindEvents: function () {
        var onMouseUpdate = function(e) {
            window.MudBlazorExtensions.currentMouseArgs = e;
        }
        document.addEventListener('mousemove', onMouseUpdate, false);
        document.addEventListener('mouseenter', onMouseUpdate, false);
    },
    
    getCurrentMousePosition: function() {
        return window.MudBlazorExtensions.currentMouseArgs;
    },

    setNextDialogOptions: function (options, dotNet, dotNetService) {
        new MudBlazorExtensionHelper(options, dotNet, dotNetService, () => {
            MudBlazorExtensions.helper = null;
            delete MudBlazorExtensions.helper;
        }).init();
    },

    addCss: function (cssContent) {
        var css = cssContent,
            head = document.head || document.getElementsByTagName('head')[0],
            style = document.createElement('style');

        head.appendChild(style);

        style.type = 'text/css';
        if (style.styleSheet) {
            // This is required for IE8 and below.
            style.styleSheet.cssText = css;
        } else {
            style.appendChild(document.createTextNode(css));
        }
    },

    openWindowAndPostMessage: function(url, message) {
        var newWindow = window.open(url);
        newWindow.onload = function () {            
            newWindow.postMessage(message, url);
        };
    },

    downloadFile(options) {
        var fileUrl = options.url || "data:" + options.mimeType + ";base64," + options.base64String;
        fetch(fileUrl)
            .then(response => response.blob())
            .then(blob => {
                var link = window.document.createElement("a");
                //link.href = window.URL.createObjectURL(blob, { type: options.mimeType });
                link.href = window.URL.createObjectURL(blob);
                link.download = options.fileName;
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
            });
    },

    attachDialog(dialogId) {
        if (dialogId) {
            let dialog = document.getElementById(dialogId);            
            if (dialog) {
                let titleCmp = dialog.querySelector('.mud-dialog-title');
                let iconCmp = null;
                if (titleCmp) {
                    const svgElements = titleCmp.querySelectorAll('svg');
                    const filteredSvgElements = Array.from(svgElements).filter(c => !c.parentElement.classList.contains('mud-ex-dialog-header-actions'));

                    if (filteredSvgElements.length > 0) {
                        iconCmp = filteredSvgElements[0];
                    }
                }

                const res = {
                    title: titleCmp ? titleCmp.innerText : 'Unnamed window',
                    icon: iconCmp ? iconCmp.innerHTML : ''
                }
                return res;
            }
        }
        return null;
    },

    closeDialogAnimated(dialogId, checkOptions) {
        if (dialogId) {            
            let dialog = document.getElementById(dialogId);
            if (!checkOptions || dialog.options?.animateClose) {
                return MudExDialogAnimationHandler.playCloseAnimation(dialog);
            }
        }
        return Promise.resolve();
    },

    getElement(selector) {
        return document.querySelector(selector);
    },

    showDialog(dialogId) {
        if (dialogId) { 
            let dialog = document.getElementById(dialogId);
            if (dialog) {
                dialog.style.visibility = 'visible';
                MudExDialogNoModalHandler.bringToFront(dialog, true);
            }
        }
    },

    focusAllAutoFocusElements: function () {
        const elements = document.querySelectorAll('[data-auto-focus="true"]');
        elements.forEach(element => {
            element.removeAttribute('data-auto-focus');
            (window.__originalBlazorFocusMethod || window.Blazor._internal.domWrapper.focus)(element);
        });
    }


};

window.MudBlazorExtensions.__bindEvents();

(function () {

    if (window.__originalBlazorFocusMethod)
        return;
    window.__originalBlazorFocusMethod = window.Blazor._internal.domWrapper.focus;
    Blazor._internal.domWrapper.focus = function (element, preventScroll) {
        element.setAttribute('data-auto-focus', 'true');
        window.__originalBlazorFocusMethod(element, preventScroll);
    };
})();
