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