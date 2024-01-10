class MudExImageViewer {
    elementRef;
    dotnet;

    constructor(elementRef, dotNet, options) {
        this.elementRef = elementRef;
        this.dotnet = dotNet;
        this.options = options;
        //this.elementRef.parentElement.addEventListener('wheel', this.preventScroll.bind(this));
        if (options && options.src) {
            this.createViewer(options);
        }
    }

    createViewer(options) {
        this.destroyViewer();
        //OSD init
        if (window.MudExImageView) {
            try {
                this.viewer = window.MudExImageView({
                    id: options.id,
                    prefixUrl: "",
                    maxZoomPixelRatio: options.maxZoomPixelRatio,
                    minZoomLevel: options.minZoomLevel,
                    //maxZoomLevel: 2,
                    animationTime: options.animationTime,
                    tileSources: {
                        type: 'image',
                        url: options.src
                    },
                    showNavigator: options.showNavigator,
                    navigatorPosition: options.navigatorPosition,
                    navigatorSizeRatio: options.navigatorSizeRatio,
                    navigatorMaintainSizeRatio: false,
                    navigatorAutoResize: false,
                    showNavigationControl: false
                });

                //mud-ex-transparent-indicator-bg
                if (options.showNavigator) {
                    this.viewer.addHandler('open',
                        () => {
                            var navigatorElement = this.elementRef.querySelector(".navigator");
                            if (navigatorElement) {
                                if (options.navigatorClass) {
                                    navigatorElement.style.backgroundColor = navigatorElement.style.background = null;
                                    navigatorElement.classList.add(options.navigatorClass);
                                }
                                if (options.navigatorRectangleColor) {
                                    var rectangle = navigatorElement.querySelector('.displayregion'); 
                                    if(rectangle) {
                                        rectangle.style.border = "2px solid " + options.navigatorRectangleColor;
                                    }
                                }
                            }
                        });
                }
                this.dotnet.invokeMethodAsync('OnViewerCreated');
            } catch (e) {
            }
        }
    }

    zoomBy(value) {
        this.viewer.viewport.zoomBy(value);
        this.viewer.viewport.applyConstraints();
    }

    reset() {
        this.viewer.viewport.goHome();
        this.viewer.viewport.applyConstraints();
    }

    toggleFullScreen() {
        this.viewer.setFullScreen(!this.viewer.isFullPage()); 
    }

    destroyViewer() {
        if (this.viewer) {
            this.viewer.destroy();
            this.viewer = null;
        }
    }

    //preventScroll(e) {
    //   e.preventDefault();
    //}

    dispose() {
        this.destroyViewer();
    }
}

window.MudExImageViewer = MudExImageViewer;

export function initializeMudExImageViewer(elementRef, dotnet, options) {
    return new MudExImageViewer(elementRef, dotnet, options);
}