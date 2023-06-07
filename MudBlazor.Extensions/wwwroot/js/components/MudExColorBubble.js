class MudExColorBubble {
    elementRef;
    options;
    dotnet;
    canvas;
    _canvasContainer;
    _previewControl;
    color;
    _el;
    
    constructor(elementRef, canvasContainer, dotNet, options) {
        this.elementRef = elementRef;
        // this.elementRef.onclick = this._onClick.bind(this);
        this._canvasContainer = canvasContainer;
        this.dotnet = dotNet;
        this.setOptions(options);
    }

    canvasContainer() {
        return new MudExDomHelper(this._canvasContainer);
    }
    
    el() {
        return this._el ||(this._el = new MudExDomHelper(this.elementRef));
    }

    init() {
        document.body.addEventListener("mousedown", this._onBodyClick.bind(this));
    }

    setOptions(options) {
        this.options = options ||
        {
            selectorSize: 161,
            minLuminance: 17,
            maxLuminance: 86,
            showColorPreview: true
        };
        this.options.selectorSize = this.options.selectorSize % 2 !== 0 ? this.options.selectorSize : this.options.selectorSize + 1;
        this.color = options.color;
    }

    _onBodyClick(e) {
        if (!MudExEventHelper.isWithin(e, this._canvasContainer) && !MudExEventHelper.isWithin(e, this._previewControl)) {
            this.hideSelector();
        }
    }

    setColor (color) {
        this.color = color;
        this.el().setStyle({
            backgroundColor: this.color,
            borderColor: MudExColorHelper.perceivedBrightness(this.color) > 230 ? MudExColorHelper.setLuminance(this.color, 0.4) : 'transparent'
        });
        this._updateLocator();
        this.dotnet.invokeMethodAsync('OnColorChanged', this.color);
    }

    dispose() {
        document.body.removeEventListener("mousedown", this._onBodyClick.bind(this));
    }

    showSelector(evt) {
        if (!this._canvasContainer.classList.contains('mud-ex-color-bubble-canvas-container-small')) {
            this._canvasContainer.classList.add('mud-ex-color-bubble-canvas-container-small');
        }
        this.canvasContainer().show();

        this.canvas = this._buildCanvas();
        
        this._canvasContainer.insertBefore(this.canvas, this._canvasContainer.firstChild);
        if (this.options.showColorPreview) {
            this._previewControl = document.createElement('div');
            this._previewControl.className = 'mud-ex-color-bubble-preview mud-ex-transparent';
            this._previewControl.style.backgroundColor = this.elementRef.style.backgroundColor;
            if (this.options.allowSelectOnPreviewClick) {
                this._previewControl.style.cursor = 'pointer';
                this._previewControl.onclick = this._onPreviewClick.bind(this);
            }
            this._previewControl.onmousemove = (e) => {
                e.stopPropagation();
                this._previewControl.style.backgroundColor = this.elementRef.style.backgroundColor;
            }
            this._canvasContainer.insertBefore(this._previewControl, this._canvasContainer.firstChild);
        }

        this._canvasContainer.ontransitionend = this._onShowEnd.bind(this);

        this._updateLocator();
    
        this.canvasContainer().
            setStyle({
                //transform: `translate(-${this.options.selectorSize / 2}px, -${this.options.selectorSize / 2}px)`
                left: this.el().getX() + this.el().getWidth() / 2 - this.options.selectorSize / 2 + 'px',
                top: this.el().getY() + this.el().getHeight() / 2 - this.options.selectorSize / 2 + 'px'
            }).
            show().
            removeCls('mud-ex-color-bubble-canvas-container-small');
    }

    _onPreviewClick(evt) {
        evt.stopPropagation();
        this.hideSelector();
        this.dotnet.invokeMethodAsync('OnColorPreviewClick');
    }

    _onClick(evt) {
        this.showSelector(evt);
    }

    _buildCanvas() {
        var canvas = document.createElement('canvas');
        canvas.height = this.options.selectorSize;
        canvas.width = this.options.selectorSize;
        
        var context = canvas.getContext('2d'),
            radius = this.options.selectorSize / 2,
            toRad = (2 * Math.PI) / 360,
            step = 360 / (4 * Math.PI * this.options.selectorSize),
            midLuminance = (this.options.maxLuminance + this.options.minLuminance) / 2,
            angle, ix, toX, toY, gradient;

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

    _onCanvasMouseOut(evt) {
        new MudExDomHelper(this._previewControl).setStyle({ backgroundColor: this.color });
    }

    _onCanvasMouseMove(evt) {
        new MudExDomHelper(this._previewControl).setStyle({ backgroundColor: this._getColorFromEvent(evt) || this.color });
    }

    _getColorFromPosition (position) {
        var radius = this.options.selectorSize / 2,
            localY = position.y - radius,
            localX = position.x - radius,
            hue = 360 - ((Math.atan2(-localY, localX) + 2 * Math.PI) * 180 / Math.PI) % 360,
            distance = Math.sqrt(localX * localX + localY * localY),
            luminance = this.options.minLuminance + (this.options.maxLuminance - this.options.minLuminance) * (-distance / radius + 1);
        return MudExColorHelper.hslToHex({
            h: MudExNumber.constrain(hue, 0, 360) / 360,
            s: 1,
            l: MudExNumber.constrain(luminance, 0, 100) / 100
        });
    }

    _getPositionFromColor (color) {
        var hsl = MudExColorHelper.hexToHsl(color),
            radius = this.options.selectorSize / 2,
            angle = (360 - hsl.h * 360) * Math.PI / 180,
            distance = radius * (1 - (hsl.l * 100 - this.options.minLuminance) / (this.options.maxLuminance - this.options.minLuminance)),
            normalizedDistance = MudExNumber.constrain(distance, 0, radius);
        return {
            x: Math.round(normalizedDistance * Math.cos(angle) + radius),
            y: Math.round(radius - normalizedDistance * Math.sin(angle))
        };
    }

    _getColorFromEvent(evt) {
        if (MudExEventHelper.isWithin(evt, this.canvas)) {
            var res = this._getColorFromPosition({
                x: evt.clientX - this.canvasContainer().getX(),
                y: evt.clientY - this.canvasContainer().getY()
            });
            return res;
        }
        return null;
    }

    _updateLocator () {
        if (this._canvasContainer && this.canvasContainer().isVisible()) {
            var position = this._getPositionFromColor(this.color);
            var bubbleLocator = this.canvasContainer().down('.mud-ex-color-bubble-locator');

            bubbleLocator.setStyle({
                left: position.x + 'px',
                top: position.y + 'px',
                borderColor: MudExColorHelper.getOptimalForegroundColor(this.color)
            });
        }
    }
    
    _onCanvasClick (evt) {
        if (!MudExEventHelper.isWithin(evt, this._previewControl)) {
            var color = this._getColorFromEvent(evt);
            if (color) {
                this.setColor(color);
            }
            if (this.options.closeAfterSelect) {
                this.hideSelector();
            }
        }
    }

    hideSelector() {
        if (this.options.showColorPreview) {
            this._previewControl?.remove();
        }

        this._canvasContainer.ontransitionend = this._onHideEnd.bind(this);

        this.canvasContainer().addCls('mud-ex-color-bubble-canvas-container-small');
    }

    _onHideEnd() {
        this.canvasContainer().hide();
        this.canvas.remove();
        this._canvasContainer.ontransitionend = null;
        this._canvasContainer.onmousemove = null;
        this._canvasContainer.onmouseout = null;
        this._canvasContainer.onclick = null;
    }

    _onShowEnd() {
        if (this.options.showColorPreview) {
            new MudExDomHelper(this._previewControl).removeCls('mud-ex-transparent');
            this._canvasContainer.onmousemove = this._onCanvasMouseMove.bind(this);
            this._canvasContainer.onmouseout = this._onCanvasMouseOut.bind(this);
        }
        this._canvasContainer.onclick = this._onCanvasClick.bind(this);
        this._canvasContainer.ontransitionend = null;
    }
    
}

window.MudExColorBubble = MudExColorBubble;

export function initializeMudExColorBubble(elementRef, canvasContainer, dotnet, options) {
    return new MudExColorBubble(elementRef, canvasContainer, dotnet, options);
}