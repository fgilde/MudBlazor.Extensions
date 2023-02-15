class MudExColorHelper {
    
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