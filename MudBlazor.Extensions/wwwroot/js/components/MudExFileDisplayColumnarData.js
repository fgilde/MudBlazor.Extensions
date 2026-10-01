import { importModuleWithoutAmd } from './MudExModuleLoader.js';

const HYPARQUET_URL = 'https://esm.sh/hyparquet@1.31.2?bundle';
const ARROW_URL = 'https://esm.sh/apache-arrow@21.2.0?bundle';
const LZ4_URL = 'https://esm.sh/lz4js@0.2.0?bundle';
const ZSTD_URL = 'https://esm.sh/zstd-codec@0.1.5?bundle';

let arrowCodecsPromise;

class MudExFileDisplayColumnarData {
    constructor(elementRef, dotNet, containerId) { this.elementRef = elementRef; this.dotnet = dotNet; this.containerId = containerId; }

    async render(fileBytes, format, maxRows) {
        try {
            const bytes = new Uint8Array(fileBytes);
            let columns, rows, totalRows;
            if (format === 'parquet') {
                const parquet = await importModuleWithoutAmd(HYPARQUET_URL);
                const objects = await parquet.parquetReadObjects({ file: bytes.buffer });
                rows = objects.slice(0, maxRows); totalRows = objects.length;
                columns = rows.length ? Object.keys(rows[0]) : [];
            } else {
                const arrow = await importModuleWithoutAmd(ARROW_URL);
                await this.ensureArrowCodecs(arrow);
                const table = arrow.tableFromIPC(bytes);
                columns = table.schema.fields.map(field => field.name);
                totalRows = table.numRows;
                rows = [];
                for (let i = 0; i < Math.min(totalRows, maxRows); i++) {
                    const row = table.get(i); const object = {};
                    columns.forEach((column, index) => object[column] = row.get ? row.get(index) : row[column]);
                    rows.push(object);
                }
            }
            this.renderTable(columns, rows, totalRows, maxRows);
            this.dotnet.invokeMethodAsync('OnRendered', totalRows, columns.length);
        } catch (error) {
            console.error('Columnar data parsing failed', error);
            this.dotnet.invokeMethodAsync('OnError', error && error.message ? error.message : 'Unable to parse columnar data');
        }
    }

    ensureArrowCodecs(arrow) {
        if (arrowCodecsPromise) return arrowCodecsPromise;
        arrowCodecsPromise = (async () => {
            if (!arrow.compressionRegistry || !arrow.CompressionType) return;

            if (!arrow.compressionRegistry.get(arrow.CompressionType.LZ4_FRAME)) {
                const lz4 = await importModuleWithoutAmd(LZ4_URL);
                arrow.compressionRegistry.set(arrow.CompressionType.LZ4_FRAME, {
                    encode: data => lz4.compress(data),
                    decode: data => lz4.decompress(data)
                });
            }

            if (!arrow.compressionRegistry.get(arrow.CompressionType.ZSTD)) {
                try {
                    const zstdModule = await importModuleWithoutAmd(ZSTD_URL);
                    const ZstdCodec = zstdModule.ZstdCodec || zstdModule.default?.ZstdCodec;
                    if (ZstdCodec) {
                        await new Promise(resolve => ZstdCodec.run(zstd => {
                            const codec = new zstd.Simple();
                            arrow.compressionRegistry.set(arrow.CompressionType.ZSTD, {
                                encode: data => codec.compress(data),
                                decode: data => codec.decompress(data)
                            });
                            resolve();
                        }));
                    }
                } catch (error) {
                    console.warn('ZSTD codec could not be initialized', error);
                }
            }
        })();
        return arrowCodecsPromise;
    }

    renderTable(columns, rows, totalRows, maxRows) {
        const host = document.getElementById(this.containerId); host.replaceChildren();
        const table = document.createElement('table');
        const head = table.createTHead().insertRow(); columns.forEach(column => { const th = document.createElement('th'); th.textContent = column; head.appendChild(th); });
        const body = table.createTBody();
        rows.forEach(row => { const tr = body.insertRow(); columns.forEach(column => { const td = tr.insertCell(); td.textContent = this.format(row[column]); }); });
        host.appendChild(table);
        if (totalRows > maxRows) { const note = document.createElement('div'); note.className = 'pa-2 mud-typography mud-typography-caption'; note.textContent = `Showing ${maxRows.toLocaleString()} of ${totalRows.toLocaleString()} rows`; host.appendChild(note); }
    }

    format(value) {
        if (value == null) return '';
        if (typeof value === 'bigint') return value.toString();
        if (value instanceof Uint8Array) return `[${value.length} bytes]`;
        if (typeof value === 'object') { try { return JSON.stringify(value, (_, v) => typeof v === 'bigint' ? v.toString() : v); } catch (_) { return String(value); } }
        return String(value);
    }
    dispose() { const host = document.getElementById(this.containerId); if (host) host.replaceChildren(); }
}

window.MudExFileDisplayColumnarData = MudExFileDisplayColumnarData;
export function initializeMudExFileDisplayColumnarData(elementRef, dotnet, containerId) { return new MudExFileDisplayColumnarData(elementRef, dotnet, containerId); }
