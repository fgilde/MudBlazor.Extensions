/*---------------------------------------------------------------------------------------------
 * Accreditation: This file is largly based upon the MIT licenced VSCode sourcecode found at:
 * https://github.com/microsoft/vscode/tree/main/src/vs/base/browser/ui/grid
 *--------------------------------------------------------------------------------------------*/
import { Orientation } from '../splitview/splitview';
import { Emitter } from '../events';
export class LeafNode {
    get minimumWidth() {
        return this.view.minimumWidth;
    }
    get maximumWidth() {
        return this.view.maximumWidth;
    }
    get minimumHeight() {
        return this.view.minimumHeight;
    }
    get maximumHeight() {
        return this.view.maximumHeight;
    }
    get priority() {
        return this.view.priority;
    }
    get snap() {
        return this.view.snap;
    }
    get minimumSize() {
        return this.orientation === Orientation.HORIZONTAL
            ? this.minimumHeight
            : this.minimumWidth;
    }
    get maximumSize() {
        return this.orientation === Orientation.HORIZONTAL
            ? this.maximumHeight
            : this.maximumWidth;
    }
    get minimumOrthogonalSize() {
        return this.orientation === Orientation.HORIZONTAL
            ? this.minimumWidth
            : this.minimumHeight;
    }
    get maximumOrthogonalSize() {
        return this.orientation === Orientation.HORIZONTAL
            ? this.maximumWidth
            : this.maximumHeight;
    }
    get orthogonalSize() {
        return this._orthogonalSize;
    }
    get size() {
        return this._size;
    }
    get element() {
        return this.view.element;
    }
    get width() {
        return this.orientation === Orientation.HORIZONTAL
            ? this.orthogonalSize
            : this.size;
    }
    get height() {
        return this.orientation === Orientation.HORIZONTAL
            ? this.size
            : this.orthogonalSize;
    }
    constructor(view, orientation, orthogonalSize, size = 0) {
        this.view = view;
        this.orientation = orientation;
        this._onDidChange = new Emitter();
        this.onDidChange = this._onDidChange.event;
        this._orthogonalSize = orthogonalSize;
        this._size = size;
        this._disposable = this.view.onDidChange((event) => {
            if (event) {
                this._onDidChange.fire({
                    size: this.orientation === Orientation.VERTICAL
                        ? event.width
                        : event.height,
                    orthogonalSize: this.orientation === Orientation.VERTICAL
                        ? event.height
                        : event.width,
                });
            }
            else {
                this._onDidChange.fire({});
            }
        });
    }
    setVisible(visible) {
        if (this.view.setVisible) {
            this.view.setVisible(visible);
        }
    }
    layout(size, orthogonalSize) {
        this._size = size;
        this._orthogonalSize = orthogonalSize;
        this.view.layout(this.width, this.height);
    }
    dispose() {
        this._onDidChange.dispose();
        this._disposable.dispose();
    }
}
