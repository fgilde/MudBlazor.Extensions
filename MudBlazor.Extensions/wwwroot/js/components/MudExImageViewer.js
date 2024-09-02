class MudExImageViewer {
    elementRef;
    dotnet;
    startPoint;
    endPoint;
    _isSelecting;
    _selectionMode;

    constructor(elementRef, dotNet, options) {
        this.elementRef = elementRef;
        this.dotnet = dotNet;
        this.options = options;
        if (options && options.src) {
            this.createViewer(options);
        }
    }

    createViewer(options, container, rubberBand, selectionToolBar) {
        this.container = container;
        this.buttonContainer = selectionToolBar;
        this.selectionDiv = rubberBand;
        this.options = options;
        this.destroyViewer();
        document.removeEventListener('keyup', this._onKeyUp);
        if (window.MudExImageView) {
            try {
                this.viewer = window.MudExImageView({
                    id: options.id,
                    prefixUrl: "",
                    maxZoomPixelRatio: options.maxZoomPixelRatio,
                    minZoomLevel: options.minZoomLevel,
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
                this.createRubberBandSelection();
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
                                    if (rectangle) {
                                        rectangle.style.border = "2px solid " + options.navigatorRectangleColor;
                                    }
                                }
                            }
                        });
                }

                document.addEventListener('keyup', this._onKeyUp);

                this.dotnet.invokeMethodAsync('OnViewerCreated');
            } catch (e) {
                console.error(e);
            }
        }
    }

    _onKeyUp = (event) => {
        if ((this._isSelecting && event.key === 'Control') || event.key === 'Escape') {
            if (this.selectionDiv.style.display !== 'none') {
                this.hideRubberBand();
                event.preventDefaultAction = true;
            } else {
                this.reset();
            }
        }
    }

    getCanvas() {
        return this.viewer.canvas.querySelector('canvas');
    }

    toggleRubberBandSelection(value) {
        this._isSelecting = this._selectionMode = value;
        if (!value && !this.options.allowInteractingUnderRubberBand) {
            this.hideRubberBand();
        }
    }

    setRubberBandDimensions(x, y, width, height, display) {
        this.selectionDiv.style.left = `${x}px`;
        this.selectionDiv.style.top = `${y}px`;
        this.selectionDiv.style.width = `${width}px`;
        this.selectionDiv.style.height = `${height}px`;
        if (display)
            this.selectionDiv.style.display = display;
    }

    hideRubberBand() {
        if (this.selectionDiv) {
            this.selectionDiv.style.display = 'none';
        }
        if (this.buttonContainer) {
            this.buttonContainer.style.display = 'none';
        }
        this.startPoint = null;
        this._isSelecting = false;
    }

    createRubberBandSelection() {
        if (!this.options.allowRubberBandSelection) {
            return;
        }

        this.buttonContainer.style.position = 'absolute';
        this.buttonContainer.style.display = 'none';

        if (!this.options.allowInteractingUnderRubberBand) {
            this.viewer.addHandler('zoom',
                (event) => {
                    this.hideRubberBand();
                });
        }

        this.viewer.addHandler('canvas-press', (event) => {
            if (event.originalEvent.ctrlKey || this._selectionMode) {
                this.hideRubberBand();

                this._isSelecting = true;
                var position = MudExImageView.getMousePosition(event.originalEvent);
                position = this.relativePosition(position, this.container);
                this.startPoint = new MudExImageView.Point(position.x, position.y);  
                this.setRubberBandDimensions(position.x, position.y, 0, 0, 'block');
                event.preventDefaultAction = true;
            } else if (!this.options.allowInteractingUnderRubberBand) {
                this.hideRubberBand();
            }
        });

        this.viewer.addHandler('canvas-drag', (event) => {
            if (this._isSelecting && this.startPoint) {
                let currentPos = MudExImageView.getMousePosition(event.originalEvent);
                currentPos = this.relativePosition(currentPos, this.container);

                const x = Math.min(this.startPoint.x, currentPos.x);
                const y = Math.min(this.startPoint.y, currentPos.y);
                const width = Math.abs(this.startPoint.x - currentPos.x);
                const height = Math.abs(this.startPoint.y - currentPos.y);

                this.setRubberBandDimensions(x, y, width, height);

                event.preventDefaultAction = true;
            }
        });

        this.viewer.addHandler('canvas-drag-end', async (event) => {
            if (this._isSelecting && this.startPoint) {
                let endPoint = MudExImageView.getMousePosition(event.originalEvent);
                endPoint = this.relativePosition(endPoint, this.container);
                this.endPoint = new MudExImageView.Point(endPoint.x, endPoint.y); 

                const viewportStart = this.viewer.viewport.pointFromPixel(this.startPoint);
                const viewportEnd = this.viewer.viewport.pointFromPixel(this.endPoint);
                const imageStart = this.viewer.viewport.viewportToImageCoordinates(viewportStart);
                const imageEnd = this.viewer.viewport.viewportToImageCoordinates(viewportEnd);

                var viewportBounds = new MudExImageView.Rect(
                    Math.min(imageStart.x, imageEnd.x),
                    Math.min(imageStart.y, imageEnd.y),
                    Math.abs(imageStart.x - imageEnd.x),
                    Math.abs(imageStart.y - imageEnd.y)
                );

                this._isSelecting = false;
                
                this.buttonContainer.style.display = 'flex';
                this.buttonContainer.style.top = `${parseInt(this.selectionDiv.style.top) + parseInt(this.selectionDiv.style.height) + 5}px`;
                this.buttonContainer.style.left = `${parseInt(this.selectionDiv.style.left) + parseInt(this.selectionDiv.style.width) - this.buttonContainer.getBoundingClientRect().width}px`;

                event.preventDefaultAction = true;
                this.dotnet.invokeMethodAsync('OnAreaSelected', viewportBounds, this.selectionDiv.getBoundingClientRect(), await this.getSelectedAreaImageData('bytes'), await this.getSelectedAreaImageData('blob'));
            }
        });

    }

    relativePosition(position, container) {
        const containerRect = container.getBoundingClientRect();
        return {
            x: position.x - containerRect.left,
            y: position.y - containerRect.top
        };
    }

    getSelectedAreaImageData(outputType = 'dataURL') {
        if (!this.startPoint || !this.endPoint) return null;

        const canvas = this.getCanvas();
        //const context = canvas.getContext('2d');

        const x = Math.min(this.startPoint.x, this.endPoint.x);
        const y = Math.min(this.startPoint.y, this.endPoint.y);
        const width = Math.abs(this.startPoint.x - this.endPoint.x);
        const height = Math.abs(this.startPoint.y - this.endPoint.y);

        // Create an off-screen canvas to extract the selected area
        const tempCanvas = document.createElement('canvas');
        tempCanvas.width = width;
        tempCanvas.height = height;
        const tempContext = tempCanvas.getContext('2d');

        // Draw the selected area on the off-screen canvas
        tempContext.drawImage(canvas, x, y, width, height, 0, 0, width, height);

        switch (outputType) {
        case 'blob':
            return new Promise((resolve) => {
                tempCanvas.toBlob(blob => resolve(URL.createObjectURL(blob)), 'image/png');
            });

        case 'bytes':
            return new Promise((resolve) => {
                tempCanvas.toBlob(blob => {
                    const reader = new FileReader();
                    reader.onload = function () {
                        resolve(new Uint8Array(this.result));
                    };
                    reader.readAsArrayBuffer(blob);
                }, 'image/png');
            });

        default: // 'dataURL'
            return tempCanvas.toDataURL('image/png');
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

    getCurrentViewImageDataUrl() {
        return this.viewer.drawer.canvas.toDataURL("image/png");
    }

    print(url = '') {
        var printWindow = window.open('', '_blank');
        printWindow.document.open();
        printWindow.document.write('<html><head></head><body>');
        var image = new Image();
        image.src = url || this.getCurrentViewImageDataUrl();
        image.onload = function () {
            printWindow.document.body.appendChild(image);

            image.onload = function () {
                printWindow.focus();
                printWindow.print();
                printWindow.close();
            };
        };
        printWindow.document.close();
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

    dispose() {
        this.hideRubberBand();
        this.selectionDiv?.remove();
        this.buttonContainer?.remove();
        this.destroyViewer();
    }
}

window.MudExImageViewer = MudExImageViewer;

export function initializeMudExImageViewer(elementRef, dotnet, options) {
    return new MudExImageViewer(elementRef, dotnet, options);
}
