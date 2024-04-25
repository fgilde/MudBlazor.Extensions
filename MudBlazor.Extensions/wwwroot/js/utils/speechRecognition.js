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
        const { lang = 'en-US', continuous = true, interimResults = true } = options || {};
        const recognition = new webkitSpeechRecognition();
        recognition.lang = lang;
        recognition.continuous = continuous;
        recognition.interimResults = interimResults;

        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        return { recognition, stream };
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
            callback.invokeMethodAsync('Invoke', { ...this.recordings[id].lastParam, audioData: audioByteArray });
        }

        delete this.recordings[id];
    }

    static handleRecognitionResult(event, id, callback) {
        let options = this.recordings[id].options;
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

        this.recordings[id].lastParam = result;

        if (!options.finalResultsOnly || isFinal) {
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
