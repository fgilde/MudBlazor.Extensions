import { disableIframePointEvents } from '../dom';
import { addDisposableListener, Emitter } from '../events';
import { CompositeDisposable, MutableDisposable, } from '../lifecycle';
export class DragHandler extends CompositeDisposable {
    constructor(el, disabled) {
        super();
        this.el = el;
        this.disabled = disabled;
        this.dataDisposable = new MutableDisposable();
        this.pointerEventsDisposable = new MutableDisposable();
        this._onDragStart = new Emitter();
        this.onDragStart = this._onDragStart.event;
        this.addDisposables(this._onDragStart, this.dataDisposable, this.pointerEventsDisposable);
        this.configure();
    }
    setDisabled(disabled) {
        this.disabled = disabled;
    }
    isCancelled(_event) {
        return false;
    }
    configure() {
        this.addDisposables(this._onDragStart, addDisposableListener(this.el, 'dragstart', (event) => {
            if (event.defaultPrevented || this.isCancelled(event) || this.disabled) {
                event.preventDefault();
                return;
            }
            const iframes = disableIframePointEvents();
            this.pointerEventsDisposable.value = {
                dispose: () => {
                    iframes.release();
                },
            };
            this.el.classList.add('dv-dragged');
            setTimeout(() => this.el.classList.remove('dv-dragged'), 0);
            this.dataDisposable.value = this.getData(event);
            this._onDragStart.fire(event);
            if (event.dataTransfer) {
                event.dataTransfer.effectAllowed = 'move';
                const hasData = event.dataTransfer.items.length > 0;
                if (!hasData) {
                    /**
                     * Although this is not used by dockview many third party dnd libraries will check
                     * dataTransfer.types to determine valid drag events.
                     *
                     * For example: in react-dnd if dataTransfer.types is not set then the dragStart event will be cancelled
                     * through .preventDefault(). Since this is applied globally to all drag events this would break dockviews
                     * dnd logic. You can see the code at
                 P    * https://github.com/react-dnd/react-dnd/blob/main/packages/backend-html5/src/HTML5BackendImpl.ts#L542
                     */
                    event.dataTransfer.setData('text/plain', '');
                }
            }
        }), addDisposableListener(this.el, 'dragend', () => {
            this.pointerEventsDisposable.dispose();
            setTimeout(() => {
                this.dataDisposable.dispose(); // allow the data to be read by other handlers before disposing
            }, 0);
        }));
    }
}
