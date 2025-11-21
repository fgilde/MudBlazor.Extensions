
export function observeSection(element, dotNetRef) {
    if (!element) return;

    const options = {
        root: null,
        rootMargin: "-30% 0px -70% 0px",
        threshold: 0
    };

    const callback = (entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                dotNetRef.invokeMethodAsync("SectionBecameActive");
            }
        });
    };

    const observer = new IntersectionObserver(callback, options);
    observer.observe(element);

    element._sectionObserver = observer;
}

export function unobserveSection(element) {
    if (!element) return;

    const observer = element._sectionObserver;
    if (observer) {
        observer.unobserve(element);
        observer.disconnect();
        delete element._sectionObserver;
    }
}
