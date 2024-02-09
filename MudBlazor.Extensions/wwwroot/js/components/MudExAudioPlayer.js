class MudExAudioPlayer {
    elementRef;
    dotnet;
    audioMotion;
    constructor(elementRef, dotNet, options) {
        this.elementRef = elementRef;
        this.dotnet = dotNet;
        this.options = options;
        this.createVisualizer(options);
    }

    async createVisualizer(options) {
        //var audioContext = new AudioContext();
        this.audioMotion = new (window.AudioMotionAnalyzer || (window.AudioMotionAnalyzer = options.audioMotion.default))(
            this.visualizer = options.visualizer,
            this.options = options,
            //{
            //    //height: 400,
            //    ansiBands: false,
            //    showScaleX: false,
            //    bgAlpha: 0,
            //    overlay: true,
            //    mode: 4,
            //    frequencyScale: "log",
            //    radial: true,
            //    showPeaks: true,

            //    smoothing: 0.7
            //}
        );

        options.audioElements.forEach(el => this.audioMotion.connectInput(el) );
    }

    setOptions(options) {
        this.audioMotion.setOptions(this.options = options);
    }
    
    dispose() {
    }
}

window.MudExAudioPlayer = MudExAudioPlayer;

export function initializeMudExAudioPlayer(elementRef, dotnet, options) {
    return new MudExAudioPlayer(elementRef, dotnet, options);
}