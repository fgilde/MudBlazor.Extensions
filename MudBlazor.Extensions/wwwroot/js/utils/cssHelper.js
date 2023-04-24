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
    
}
window.MudExCssHelper = MudExCssHelper;