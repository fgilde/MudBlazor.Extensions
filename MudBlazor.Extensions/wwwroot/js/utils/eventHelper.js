class EventHelper {
    static isWithin(event, element) {
        let rect = element.getBoundingClientRect();
        return (event.clientX > rect.left &&
        event.clientX < rect.right &&
        event.clientY < rect.bottom &&
        event.clientY > rect.top);
    }
}