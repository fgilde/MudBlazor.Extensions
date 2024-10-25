class MudExObserver {
    static observer = null;
    static observedElements = new Map();

    static initObserver() {
        if (MudExObserver.observer == null) {
            MudExObserver.observer = new IntersectionObserver((entries) => {
                entries.forEach(async entry => {
                    const dotNetHelper = MudExObserver.observedElements.get(entry.target);
                    const applyStylesFirst = entry.target.getAttribute('data-apply-styles-first') === 'true';
                    const isVirtualized = entry.target.getAttribute('data-virtualize') === 'true';

                    if (dotNetHelper && (!applyStylesFirst || !isVirtualized)) {
                        await dotNetHelper.invokeMethodAsync('OnVisibilityChanged', entry.isIntersecting);
                    }

                    if (!isVirtualized) return;

                    const intersectStyle = entry.target.getAttribute('data-intersect-style') || '';
                    const noIntersectStyle = entry.target.getAttribute('data-no-intersect-style') || '';
                    const intersectClass = entry.target.getAttribute('data-intersect-class') || '';
                    const noIntersectClass = entry.target.getAttribute('data-no-intersect-class') || '';

                    entry.target.setAttribute('data-intersecting', entry.isIntersecting);

                    if (entry.isIntersecting) {
                        entry.target.style.cssText += intersectStyle;
                        entry.target.style.cssText = entry.target.style.cssText.replace(noIntersectStyle, '');
                        if(intersectClass)
                            entry.target.classList.add(...intersectClass.split(' '));
                        if(noIntersectClass)
                            entry.target.classList.remove(...noIntersectClass.split(' '));
                    }
                    else {
                        entry.target.style.cssText += noIntersectStyle;
                        entry.target.style.cssText = entry.target.style.cssText.replace(intersectStyle, '');
                        if(noIntersectClass)
                            entry.target.classList.add(...noIntersectClass.split(' '));
                        if(intersectClass)
                            entry.target.classList.remove(...intersectClass.split(' '));
                    }

                    if (dotNetHelper && applyStylesFirst) {
                        await dotNetHelper.invokeMethodAsync('OnVisibilityChanged', entry.isIntersecting);
                    }

                });
            });
        }
    }



    static observeVisibility(elementOrSelector, dotNetHelper) {
        MudExObserver.initObserver();

        const element = MudExObserver.getElement(elementOrSelector);
        if (!element) return;

        MudExObserver.observedElements.set(element, dotNetHelper);
        MudExObserver.observer.observe(element);
    }

    static unObserveVisibility(elementOrSelector) {
        const element = MudExObserver.getElement(elementOrSelector);
        if (!element || !MudExObserver.observedElements.has(element)) return;

        MudExObserver.observer.unobserve(element);
        MudExObserver.observedElements.delete(element);
    }

    static getElement(elementOrSelector) {
        if (typeof elementOrSelector === 'string') {
            return document.querySelector(elementOrSelector) || document.getElementById(elementOrSelector);
        } else if (elementOrSelector instanceof Element) {
            return elementOrSelector;
        } else {
            return null;
        }
    }
}

window.MudExObserver = MudExObserver;