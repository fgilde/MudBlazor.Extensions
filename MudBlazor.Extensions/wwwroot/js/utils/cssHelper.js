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

    /**
     * Positions a popup element (popupId) at the bottom-left of an anchor element (anchorId)
     * using position:fixed so it escapes any ancestor's overflow / clipping.
     * Flips upward when there isn't enough room below, clamps to viewport horizontally.
     */
    static floatPopupAtAnchor(anchorId, popupId) {
        const anchor = document.getElementById(anchorId);
        const popup = document.getElementById(popupId);
        if (!anchor || !popup) return;

        // Reset to natural size so offsetHeight/Width are accurate
        popup.style.maxWidth = '';
        const rect = anchor.getBoundingClientRect();
        const viewportH = window.innerHeight || document.documentElement.clientHeight;
        const viewportW = window.innerWidth || document.documentElement.clientWidth;
        const margin = 8;

        popup.style.position = 'fixed';

        // Make sure popup is at least as wide as the anchor
        const minWidth = Math.max(rect.width, popup.offsetWidth || 0);
        popup.style.minWidth = minWidth + 'px';

        // Cap height to remaining vertical space if needed
        const spaceBelow = viewportH - rect.bottom - margin;
        const spaceAbove = rect.top - margin;
        const popupH = popup.offsetHeight;

        let openUpward = false;
        if (popupH > spaceBelow && spaceAbove > spaceBelow) openUpward = true;

        if (openUpward) {
            const maxH = Math.max(120, spaceAbove);
            popup.style.maxHeight = maxH + 'px';
            popup.style.top = Math.max(margin, rect.top - 2 - Math.min(popupH, maxH)) + 'px';
        } else {
            const maxH = Math.max(120, spaceBelow);
            popup.style.maxHeight = maxH + 'px';
            popup.style.top = (rect.bottom + 2) + 'px';
        }

        // Horizontal: clamp so the popup stays in the viewport
        let left = rect.left;
        const popupW = popup.offsetWidth;
        if (left + popupW > viewportW - margin) left = Math.max(margin, viewportW - popupW - margin);
        if (left < margin) left = margin;
        popup.style.left = left + 'px';
        popup.style.right = 'auto';
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

    /**
     * Returns all available CSS class names declared in loaded stylesheets.
     * Result groups class names by their source (stylesheet href / 'inline').
     * Cross-origin stylesheets are silently skipped (CORS would throw on cssRules access).
     */
    static getAllCssClasses(options) {
        options = options || {};
        const includeStyles = options.includeStyles !== false;
        const prefix = (options.prefix || '').toString();
        const sourceFilter = (options.sourceFilter || '').toString().toLowerCase();
        const classRegex = /\.([a-zA-Z_][\w-]*)/g;
        const groups = new Map();
        const collected = new Map();

        const sourceLabel = sheet => {
            if (!sheet) return 'inline';
            if (sheet.href) {
                try {
                    const u = new URL(sheet.href, window.location.href);
                    return (u.pathname || sheet.href).split('/').pop() || sheet.href;
                } catch { return sheet.href; }
            }
            return sheet.ownerNode && sheet.ownerNode.id ? '#' + sheet.ownerNode.id : 'inline';
        };

        const handleRule = (rule, source) => {
            if (!rule) return;
            // CSSGroupingRule (media, supports, layer, ...) -> recurse
            if (rule.cssRules && rule.cssRules.length && !rule.selectorText) {
                Array.from(rule.cssRules).forEach(r => handleRule(r, source));
                return;
            }
            const sel = rule.selectorText;
            if (!sel) return;
            let m;
            classRegex.lastIndex = 0;
            while ((m = classRegex.exec(sel)) !== null) {
                const cls = m[1];
                if (prefix && !cls.startsWith(prefix)) continue;
                const key = cls;
                let entry = collected.get(key);
                if (!entry) {
                    entry = { name: cls, selectors: [], source: source, styles: '' };
                    collected.set(key, entry);
                }
                if (entry.selectors.indexOf(sel) === -1) entry.selectors.push(sel);
                if (includeStyles && rule.style && rule.style.cssText && !entry.styles) {
                    entry.styles = rule.style.cssText.trim();
                }
            }
        };

        Array.from(document.styleSheets).forEach(sheet => {
            const src = sourceLabel(sheet);
            if (sourceFilter && src.toLowerCase().indexOf(sourceFilter) === -1) return;
            let rules;
            try { rules = sheet.cssRules; } catch { return; }
            if (!rules) return;
            Array.from(rules).forEach(rule => handleRule(rule, src));
        });

        collected.forEach(entry => {
            const list = groups.get(entry.source) || [];
            list.push(entry);
            groups.set(entry.source, list);
        });

        const result = [];
        groups.forEach((items, source) => {
            items.sort((a, b) => a.name.localeCompare(b.name));
            result.push({ source: source, classes: items });
        });
        result.sort((a, b) => a.source.localeCompare(b.source));
        return result;
    }

    /**
     * Returns the inline cssText for the first rule whose selector matches `.className`
     * exactly or starts with `.className` followed by a non-class boundary character.
     */
    static getClassStyles(className) {
        if (!className) return '';
        if (className.startsWith('.')) className = className.slice(1);
        const dotted = '.' + className;
        const sheets = Array.from(document.styleSheets);
        const result = [];
        const visit = rule => {
            if (!rule) return;
            if (rule.cssRules && !rule.selectorText) {
                Array.from(rule.cssRules).forEach(visit);
                return;
            }
            if (!rule.selectorText) return;
            const selectors = rule.selectorText.split(',').map(s => s.trim());
            for (const s of selectors) {
                if (s === dotted) {
                    if (rule.style && rule.style.cssText) result.push(rule.style.cssText.trim());
                    return;
                }
            }
        };
        for (const sheet of sheets) {
            let rules;
            try { rules = sheet.cssRules; } catch { continue; }
            if (!rules) continue;
            Array.from(rules).forEach(visit);
        }
        return result.join(' ');
    }

    /**
     * Returns the computed style cssText of the first element matching the selector.
     * Useful for previewing what a combined class string actually resolves to.
     */
    static getComputedStyleFor(selector, propertyList) {
        const el = document.querySelector(selector);
        if (!el) return '';
        const cs = window.getComputedStyle(el);
        if (propertyList && propertyList.length) {
            return propertyList.map(p => `${p}: ${cs.getPropertyValue(p)}`).join('; ');
        }
        const out = [];
        for (let i = 0; i < cs.length; i++) {
            const p = cs[i];
            out.push(`${p}: ${cs.getPropertyValue(p)}`);
        }
        return out.join('; ');
    }
}
window.MudExCssHelper = MudExCssHelper;