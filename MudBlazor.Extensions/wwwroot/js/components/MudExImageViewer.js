class MudExImageViewer {
    elementRef;
    dotnet;
    startPoint;
    endPoint;
    _isSelecting;
    _selectionMode;
    _loadVersion = 0;
    _decodedObjectUrl = null;
    _preparedSrc = null;
    _psd = null;
    _psdLayers = new Map();

    static AG_PSD_URL = 'https://esm.sh/ag-psd@31.0.2?bundle';
    static HEIC2ANY_URL = 'https://esm.sh/heic2any@0.0.4?bundle';
    static UTIF_URL = 'https://esm.sh/utif2@4.1.0?bundle';
    static GIFENC_URL = 'https://cdn.jsdelivr.net/npm/gifenc@1.0.3/dist/gifenc.esm.js';
    static RASTER_FORMATS = ['tif', 'tiff', 'tga', 'qoi', 'pbm', 'pgm', 'ppm', 'pnm'];
    static NATIVE_EXPORT_TYPES = { png: 'image/png', jpeg: 'image/jpeg', webp: 'image/webp' };

    static async importModuleWithoutAmd(url) {
        const loader = await import('./MudExModuleLoader.js');
        return await loader.importModuleWithoutAmd(url);
    }

    constructor(elementRef, dotNet, options) {
        this.elementRef = elementRef;
        this.dotnet = dotNet;
        this.options = options;
        if (options && options.src) {
            this.createViewer(options);
        }
    }

    async createViewer(options, container, rubberBand, selectionToolBar) {
        const loadVersion = ++this._loadVersion;
        this.container = container;
        this.buttonContainer = selectionToolBar;
        this.selectionDiv = rubberBand;
        this.options = options;
        this.destroyViewer();
        this.clearDecodedImage();
        document.removeEventListener('keyup', this._onKeyUp);
        if (window.MudExImageView) {
            try {
                const prepared = await this.prepareSource(options.src, options.format);
                if (loadVersion !== this._loadVersion) return;
                this._preparedSrc = prepared.src;
                this.dotnet.invokeMethodAsync('OnImagePrepared', prepared.width || 0, prepared.height || 0, prepared.layers || []);
                this.viewer = window.MudExImageView({
                    id: options.id,
                    prefixUrl: "",
                    maxZoomPixelRatio: options.maxZoomPixelRatio,
                    minZoomLevel: options.minZoomLevel,
                    animationTime: options.animationTime,
                    tileSources: {
                        type: 'image',
                        url: prepared.src
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
                this.dotnet.invokeMethodAsync('OnImageLoadError', e && e.message ? e.message : 'Unable to decode image');
            }
        }
    }

    async prepareSource(src, format) {
        const normalizedFormat = (format || '').toLowerCase();
        if (normalizedFormat === 'psd' || normalizedFormat === 'psb')
            return await this.preparePhotoshop(src);
        if (normalizedFormat === 'heic' || normalizedFormat === 'heif')
            return await this.prepareHeif(src, normalizedFormat);
        if (MudExImageViewer.RASTER_FORMATS.includes(normalizedFormat))
            return await this.prepareRaster(src, normalizedFormat);
        return { src, width: 0, height: 0, layers: [] };
    }

    async preparePhotoshop(src) {
        const module = await MudExImageViewer.importModuleWithoutAmd(MudExImageViewer.AG_PSD_URL);
        if (module.initializeCanvas) {
            module.initializeCanvas((width, height) => {
                const canvas = document.createElement('canvas');
                canvas.width = width;
                canvas.height = height;
                return canvas;
            });
        }

        const bytes = await this.readSourceBytes(src);
        this._psd = module.readPsd(bytes.buffer, {
            skipLayerImageData: false,
            skipCompositeImageData: false,
            skipThumbnail: true
        });
        this._psdLayers.clear();
        const layers = this.collectLayers(this._psd.children || []);
        const canvas = this._psd.canvas || this.renderPsdComposite();
        return {
            src: await this.canvasToObjectUrl(canvas),
            width: this._psd.width || canvas.width,
            height: this._psd.height || canvas.height,
            layers
        };
    }

    async prepareHeif(src, format) {
        const bytes = await this.readSourceBytes(src);
        const module = await MudExImageViewer.importModuleWithoutAmd(MudExImageViewer.HEIC2ANY_URL);
        const heic2any = module.default || module.heic2any || module;
        const converted = await heic2any({
            blob: new Blob([bytes], { type: format === 'heif' ? 'image/heif' : 'image/heic' }),
            toType: 'image/png'
        });
        const blob = Array.isArray(converted) ? converted[0] : converted;
        const dimensions = await this.readImageDimensions(blob);
        return {
            src: this.setDecodedObjectUrl(URL.createObjectURL(blob)),
            width: dimensions.width,
            height: dimensions.height,
            layers: []
        };
    }

    async prepareRaster(src, format) {
        const bytes = await this.readSourceBytes(src);
        const codecs = await import('./MudExImageCodecs.js');
        const image = format === 'tif' || format === 'tiff'
            ? codecs.decodeTiff(await MudExImageViewer.importLibrary(MudExImageViewer.UTIF_URL), bytes)
            : codecs.decode(format, bytes);
        const canvas = document.createElement('canvas');
        canvas.width = image.width;
        canvas.height = image.height;
        canvas.getContext('2d').putImageData(new ImageData(image.data, image.width, image.height), 0, 0);
        return { src: await this.canvasToObjectUrl(canvas), width: image.width, height: image.height, layers: [] };
    }

    static async importLibrary(url) {
        const module = await MudExImageViewer.importModuleWithoutAmd(url);
        return module.default || module;
    }

    /** Encodes the image at url (the displayed image when url is the viewer source) and returns an object URL. */
    async exportImage(url, format) {
        const source = !url || url === this.options?.src ? this._preparedSrc || url : url;
        const image = await new Promise((resolve, reject) => {
            const element = new Image();
            element.crossOrigin = 'anonymous';
            element.onload = () => resolve(element);
            element.onerror = () => reject(new Error('The image could not be loaded for export.'));
            element.src = source;
        });
        const canvas = document.createElement('canvas');
        canvas.width = image.naturalWidth;
        canvas.height = image.naturalHeight;
        const context = canvas.getContext('2d');
        if (format === 'jpeg') {
            // JPEG has no alpha, transparent areas would turn black
            context.fillStyle = '#fff';
            context.fillRect(0, 0, canvas.width, canvas.height);
        }
        context.drawImage(image, 0, 0);

        let blob;
        const nativeType = MudExImageViewer.NATIVE_EXPORT_TYPES[format];
        if (nativeType) {
            blob = await new Promise(resolve => canvas.toBlob(resolve, nativeType, 0.92));
            if (!blob || blob.type !== nativeType) throw new Error(`This browser cannot encode ${format} images.`);
        } else {
            const codecs = await import('./MudExImageCodecs.js');
            const pixels = context.getImageData(0, 0, canvas.width, canvas.height);
            const bytes = format === 'tiff' ? codecs.encodeTiff(await MudExImageViewer.importLibrary(MudExImageViewer.UTIF_URL), pixels)
                : format === 'gif' ? codecs.encodeGif(await MudExImageViewer.importModuleWithoutAmd(MudExImageViewer.GIFENC_URL), pixels)
                : codecs.encode(format, pixels);
            blob = new Blob([bytes]);
        }
        const result = URL.createObjectURL(blob);
        setTimeout(() => URL.revokeObjectURL(result), 60000);
        return result;
    }

    async readSourceBytes(src) {
        const response = await fetch(src);
        if (!response.ok) throw new Error(`Unable to load image source (${response.status})`);
        return new Uint8Array(await response.arrayBuffer());
    }

    readImageDimensions(blob) {
        return new Promise((resolve, reject) => {
            const url = URL.createObjectURL(blob);
            const image = new Image();
            image.onload = () => {
                URL.revokeObjectURL(url);
                resolve({ width: image.naturalWidth, height: image.naturalHeight });
            };
            image.onerror = () => {
                URL.revokeObjectURL(url);
                reject(new Error('The decoded image could not be loaded.'));
            };
            image.src = url;
        });
    }

    collectLayers(layers, depth = 0, prefix = '') {
        const result = [];
        layers.forEach((layer, index) => {
            const id = prefix ? `${prefix}/${index}` : `${index}`;
            this._psdLayers.set(id, layer);
            result.push({
                id,
                name: layer.name || `Layer ${index + 1}`,
                visible: layer.hidden !== true,
                depth,
                isGroup: Array.isArray(layer.children)
            });
            if (Array.isArray(layer.children))
                result.push(...this.collectLayers(layer.children, depth + 1, id));
        });
        return result;
    }

    async setLayerVisibility(id, visible) {
        const layer = this._psdLayers.get(id);
        if (!layer || !this._psd || !this.viewer) return;
        layer.hidden = !visible;
        const nextUrl = await this.canvasToObjectUrl(this.renderPsdComposite());
        this.viewer.open({ type: 'image', url: nextUrl });
    }

    renderPsdComposite() {
        const canvas = document.createElement('canvas');
        canvas.width = this._psd.width;
        canvas.height = this._psd.height;
        this.drawPsdLayers(canvas.getContext('2d'), this._psd.children || []);
        return canvas;
    }

    drawPsdLayers(context, layers) {
        for (let index = layers.length - 1; index >= 0; index--) {
            const layer = layers[index];
            if (!layer || layer.hidden) continue;

            context.save();
            context.globalAlpha = layer.opacity == null ? 1 : layer.opacity;
            context.globalCompositeOperation = this.canvasBlendMode(layer.blendMode);
            if (Array.isArray(layer.children)) {
                const groupCanvas = document.createElement('canvas');
                groupCanvas.width = this._psd.width;
                groupCanvas.height = this._psd.height;
                this.drawPsdLayers(groupCanvas.getContext('2d'), layer.children);
                context.drawImage(groupCanvas, 0, 0);
            } else if (layer.canvas) {
                context.drawImage(layer.canvas, layer.left || 0, layer.top || 0);
            }
            context.restore();
        }
    }

    canvasBlendMode(mode) {
        const modes = {
            'normal': 'source-over', 'pass through': 'source-over', 'multiply': 'multiply', 'screen': 'screen',
            'overlay': 'overlay', 'darken': 'darken', 'lighten': 'lighten', 'color dodge': 'color-dodge',
            'color burn': 'color-burn', 'hard light': 'hard-light', 'soft light': 'soft-light',
            'difference': 'difference', 'exclusion': 'exclusion', 'hue': 'hue', 'saturation': 'saturation',
            'color': 'color', 'luminosity': 'luminosity', 'linear dodge': 'lighter', 'lighter color': 'lighter'
        };
        return modes[mode] || 'source-over';
    }

    canvasToObjectUrl(canvas) {
        return new Promise((resolve, reject) => canvas.toBlob(blob => {
            if (!blob) reject(new Error('Unable to render the decoded image.'));
            else resolve(this.setDecodedObjectUrl(URL.createObjectURL(blob)));
        }, 'image/png'));
    }

    setDecodedObjectUrl(url) {
        const previous = this._decodedObjectUrl;
        this._decodedObjectUrl = url;
        if (previous) setTimeout(() => URL.revokeObjectURL(previous), 10000);
        return url;
    }

    clearDecodedImage() {
        if (this._decodedObjectUrl) URL.revokeObjectURL(this._decodedObjectUrl);
        this._decodedObjectUrl = null;
        this._preparedSrc = null;
        this._psd = null;
        this._psdLayers.clear();
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
        this._loadVersion++;
        this.hideRubberBand();
        this.selectionDiv?.remove();
        this.buttonContainer?.remove();
        this.destroyViewer();
        this.clearDecodedImage();
    }
}

window.MudExImageViewer = MudExImageViewer;

export function initializeMudExImageViewer(elementRef, dotnet, options) {
    return new MudExImageViewer(elementRef, dotnet, options);
}
