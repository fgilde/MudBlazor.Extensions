import { addDisposableListener } from '../events';
import { CompositeDisposable } from '../lifecycle';
export class DragAndDropObserver extends CompositeDisposable {
    constructor(element, callbacks) {
        super();
        this.element = element;
        this.callbacks = callbacks;
        this.target = null;
        this.registerListeners();
    }
    onDragEnter(e) {
        this.target = e.target;
        this.callbacks.onDragEnter(e);
    }
    onDragOver(e) {
        e.preventDefault(); // needed so that the drop event fires (https://stackoverflow.com/questions/21339924/drop-event-not-firing-in-chrome)
        if (this.callbacks.onDragOver) {
            this.callbacks.onDragOver(e);
        }
    }
    onDragLeave(e) {
        if (this.target === e.target) {
            this.target = null;
            this.callbacks.onDragLeave(e);
        }
    }
    onDragEnd(e) {
        this.target = null;
        this.callbacks.onDragEnd(e);
    }
    onDrop(e) {
        this.callbacks.onDrop(e);
    }
    registerListeners() {
        this.addDisposables(addDisposableListener(this.element, 'dragenter', (e) => {
            this.onDragEnter(e);
        }, true));
        this.addDisposables(addDisposableListener(this.element, 'dragover', (e) => {
            this.onDragOver(e);
        }, true));
        this.addDisposables(addDisposableListener(this.element, 'dragleave', (e) => {
            this.onDragLeave(e);
        }));
        this.addDisposables(addDisposableListener(this.element, 'dragend', (e) => {
            this.onDragEnd(e);
        }));
        this.addDisposables(addDisposableListener(this.element, 'drop', (e) => {
            this.onDrop(e);
        }));
    }
}
