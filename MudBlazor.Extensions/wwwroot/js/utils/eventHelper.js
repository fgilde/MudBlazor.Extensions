class MudExEventHelper {
    static isWithin(event, element) {
        if (!element || !event) {
            return false;
        }
        let rect = element.getBoundingClientRect();
        return (event.clientX > rect.left &&
        event.clientX < rect.right &&
        event.clientY < rect.bottom &&
        event.clientY > rect.top);
    }
}

window.MudExEventHelper = MudExEventHelper;