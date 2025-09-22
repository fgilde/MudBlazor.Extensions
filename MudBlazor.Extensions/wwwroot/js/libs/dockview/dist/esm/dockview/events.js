export class WillShowOverlayLocationEvent {
    get kind() {
        return this.options.kind;
    }
    get nativeEvent() {
        return this.event.nativeEvent;
    }
    get position() {
        return this.event.position;
    }
    get defaultPrevented() {
        return this.event.defaultPrevented;
    }
    get panel() {
        return this.options.panel;
    }
    get api() {
        return this.options.api;
    }
    get group() {
        return this.options.group;
    }
    preventDefault() {
        this.event.preventDefault();
    }
    getData() {
        return this.options.getData();
    }
    constructor(event, options) {
        this.event = event;
        this.options = options;
    }
}
