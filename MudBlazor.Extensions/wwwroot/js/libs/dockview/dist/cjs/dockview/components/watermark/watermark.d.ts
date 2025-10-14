import { IWatermarkRenderer, WatermarkRendererInitParameters } from '../../types';
import { CompositeDisposable } from '../../../lifecycle';
export declare class Watermark extends CompositeDisposable implements IWatermarkRenderer {
    private readonly _element;
    get element(): HTMLElement;
    constructor();
    init(_params: WatermarkRendererInitParameters): void;
}
