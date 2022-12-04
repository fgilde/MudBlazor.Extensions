class MudExColorBubble {
    elementRef;
    options;
    dotnet;
    canvas;
    _canvasContainer;
    _previewControl;
    color = '#FF0000';

    constructor(elementRef, canvasContainer, dotNet, options) {
        this.elementRef = elementRef;
        this._canvasContainer = canvasContainer;
        this.dotnet = dotNet;
        this.options = options;
    }

    canvasContainer() {
        return new DomHelper(this._canvasContainer);
    }
    
    el() {
        return new DomHelper(this.elementRef);
    }

    init() {
        this.elementRef?.addEventListener("click", this._onClick.bind(this));
    }

    setColor (color) {
        this.color = color;
        this.el().setStyle({
            backgroundColor: this.color,
            borderColor: ColorHelper.perceivedBrightness(this.color) > 230 ?
                ColorHelper.setLuminance(this.color, 0.4) :
                'transparent'
        });
        this._updateLocator();
    }

    dispose() {
        this.elementRef?.removeEventListener("click", this._onClick.bind(this));
    }

    showSelector() {
        this.canvas = this.canvas || (this.canvas = this._buildCanvas());
        
        this._canvasContainer.insertBefore(this.canvas, this._canvasContainer.firstChild);
        if (this.options.showColorPreview) {
            this._previewControl = document.createElement('div');
            this._previewControl.className = 'cp-color-bubble-preview cp-transparent';
            this._previewControl.style.backgroundColor = this.elementRef.style.backgroundColor;
            this._canvasContainer.insertBefore(this._previewControl, this._canvasContainer.firstChild);
        }

        this._canvasContainer.addEventListener('transitionend', evt => {
            // evt.browserEvent.propertyName
            if (this.options.showColorPreview) {
                new DomHelper(this._previewControl).removeCls('cp-transparent');
                this._canvasContainer.addEventListener('mousemove', this._onCanvasMouseMove.bind(this));
            }
            this._canvasContainer.addEventListener('click', this._onCanvasClick.bind(this));
            // TODO: Detach
        });

        this._updateLocator();
    
        this.canvasContainer().
            setStyle({
                left: this.el().getX() + this.el().getWidth() / 2 - this.options.selectorSize / 2 + 'px',
                top: this.el().getY() + this.el().getHeight() / 2 - this.options.selectorSize / 2 + 'px'
            }).
            show().
            removeCls('cp-color-bubble-canvas-container-small');
    }

    _onClick() {
        this.showSelector();
    }

    _buildCanvas() {
        var canvas = document.createElement('canvas');
        canvas.height = this.options.selectorSize;
        canvas.width = this.options.selectorSize;
        
        var hslFormat = 'hsl({0}, 100%, {1}%)',
        context = canvas.getContext('2d'),
            radius = this.options.selectorSize / 2,
        toRad = (2 * Math.PI) / 360,
            step = 360 / (4 * Math.PI * this.options.selectorSize),
            midLuminance = (this.options.maxLuminance + this.options.minLuminance) / 2,
        angle,
        ix,
        toX,
        toY,
        gradient;

        for (ix = 0; ix < 360; ix += step) {
            angle = ix * toRad;
            toX = radius + radius * Math.cos(angle);
            toY = radius + radius * Math.sin(angle);

            gradient = context.createLinearGradient(radius, radius, toX, toY);
            gradient.addColorStop(0, `hsl(${ix}, 100%, ${this.options.maxLuminance}%)`);
            gradient.addColorStop((100 - midLuminance) / 100, `hsl(${ix}, 100%, ${midLuminance}%)`);
            gradient.addColorStop(1, `hsl(${ix}, 100%, ${this.options.minLuminance}%)`);
            context.strokeStyle = gradient;

            context.beginPath();
            context.moveTo(radius, radius);
            context.lineTo(toX, toY);
            context.stroke();
            context.closePath();
        }

        return canvas;
    }

    _onCanvasMouseMove(evt) {
        new DomHelper(this._previewControl).setStyle({
            backgroundColor: this._getColorFromEvent(evt) || this.color
        });
    }

    _getColorFromPosition (position) {
        var radius = this.options.selectorSize / 2,
            localY = position.y - radius,
            localX = position.x - radius,
            hue = 360 - ((Math.atan2(-localY, localX) + 2 * Math.PI) * 180 / Math.PI) % 360,
            distance = Math.sqrt(localX * localX + localY * localY),
            luminance = this.options.minLuminance + (this.options.maxLuminance - this.options.minLuminance) * (-distance / radius + 1);
        return ColorHelper.hslToHex({
            h: Number.constrain(hue, 0, 360) / 360,
            s: 1,
            l: Number.constrain(luminance, 0, 100) / 100
        });
    }

    _getPositionFromColor (color) {
        var hsl = ColorHelper.hexToHsl(color),
            radius = this.options.selectorSize / 2,
            angle = (360 - hsl.h * 360) * Math.PI / 180,
            distance = radius * (1 - (hsl.l * 100 - this.options.minLuminance) / (this.options.maxLuminance - this.options.minLuminance)),
            normalizedDistance = Number.constrain(distance, 0, radius);
        return {
            x: Math.round(normalizedDistance * Math.cos(angle) + radius),
            y: Math.round(radius - normalizedDistance * Math.sin(angle))
        };
    }

    _getColorFromEvent(evt) {
        if (EventHelper.isWithin(evt, this.canvas)) {
            var res = this._getColorFromPosition({
                x: evt.clientX - this.canvasContainer().getX(),
                y: evt.clientY - this.canvasContainer().getY()
            });
            //console.log(res);
            return res;
        }
        return null;
    }

    _updateLocator () {
        if (this._canvasContainer && this.canvasContainer().isVisible()) {
            var position = this._getPositionFromColor(this.color);
            var bubbleLocator = this.canvasContainer().down('.cp-color-bubble-locator');

            bubbleLocator.setStyle({
                left: position.x + 'px',
                top: position.y + 'px',
                borderColor: ColorHelper.getOptimalForegroundColor(this.color)
            });
        }
    }
    
    _onCanvasClick (evt) {
        //evt.stopEvent();

        var color = this._getColorFromEvent(evt);
        if (color) {
            this.setColor(color);
           // this.fireEvent('colorChanged', this.color, this);
        }

        this.hideSelector();
    }

    hideSelector () {
        this._canvasContainer.removeEventListener('click', this._onCanvasClick.bind(this));
        if (this.options.showColorPreview) {
            this._canvasContainer.removeEventListener('mousemove', this._onCanvasMouseMove.bind(this));
            this._previewControl.remove();
            //if (this._canvasContainer.isAncestor(this._previewControl)) {
            //    this._canvasContainer.removeChild(this._previewControl);
            //}
        }

        this._canvasContainer.addEventListener('transitionend', evt => {
            this.canvasContainer().hide();
            this.canvas.remove();
            // TODO: Detach
            //    if (this._canvasContainer.isAncestor(this.canvas)) {
            //        this._canvasContainer.removeChild(this.canvas);
            //    }
        });

        this.canvasContainer().addCls('cp-color-bubble-canvas-container-small');
    }
    
}

export function initializeMudExColorBubble(elementRef, canvasContainer, dotnet, options) {
    return new MudExColorBubble(elementRef, canvasContainer, dotnet, options);
}