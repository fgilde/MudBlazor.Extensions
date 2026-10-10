// Raster codecs for the formats browsers cannot decode or encode natively.
// Every image is { width, height, data } with RGBA bytes in data. TIFF and GIF use a library that
// the caller loads and passes in, so this module stays dependency free and runs in node as well.

const MAX_PIXELS = 40_000_000;

export function decode(format, bytes) {
    switch (format) {
        case 'tga': return decodeTga(bytes);
        case 'qoi': return decodeQoi(bytes);
        case 'pbm': case 'pgm': case 'ppm': case 'pnm': return decodePnm(bytes);
        default: throw new Error(`No decoder for ${format} images.`);
    }
}

export function encode(format, image) {
    switch (format) {
        case 'bmp': return encodeBmp(image);
        case 'tga': return encodeTga(image);
        case 'qoi': return encodeQoi(image);
        case 'pbm': case 'ppm': return encodePpm(image);
        default: throw new Error(`No encoder for ${format} images.`);
    }
}

export function decodeTiff(UTIF, bytes) {
    const buffer = bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength);
    const ifd = UTIF.decode(buffer)[0];
    if (!ifd) throw new Error('The TIFF file contains no image.');
    allocate(ifd.t256?.[0], ifd.t257?.[0]); // ImageWidth/ImageLength, checked before decoding allocates
    UTIF.decodeImage(buffer, ifd);
    return { width: ifd.width, height: ifd.height, data: new Uint8ClampedArray(UTIF.toRGBA8(ifd).buffer) };
}

export function encodeTiff(UTIF, { width, height, data }) {
    return new Uint8Array(UTIF.encodeImage(data, width, height));
}

export function encodeGif({ GIFEncoder, quantize, applyPalette }, { width, height, data }) {
    const palette = quantize(data, 256, { format: 'rgba4444', oneBitAlpha: true });
    const transparentIndex = palette.findIndex(color => color[3] === 0);
    const gif = GIFEncoder();
    gif.writeFrame(applyPalette(data, palette, 'rgba4444'), width, height,
        { palette, transparent: transparentIndex >= 0, transparentIndex: Math.max(transparentIndex, 0) });
    gif.finish();
    return gif.bytes();
}

function allocate(width, height) {
    if (!(width > 0) || !(height > 0) || width * height > MAX_PIXELS)
        throw new Error(`Unsupported image size ${width} x ${height}.`);
    return new Uint8ClampedArray(width * height * 4);
}

function truncated() {
    return new Error('The image file is truncated.');
}

function decodeTga(s) {
    if (s.length < 18) throw truncated();
    const idLength = s[0], mapType = s[1], type = s[2];
    const mapFirst = s[3] | (s[4] << 8), mapLength = s[5] | (s[6] << 8), mapDepth = s[7];
    const width = s[12] | (s[13] << 8), height = s[14] | (s[15] << 8), depth = s[16], descriptor = s[17];
    const kind = type & 7, rle = (type & 8) !== 0;
    if ((type & ~8) !== kind || kind < 1 || kind > 3) throw new Error(`Unsupported TGA image type ${type}.`);
    const mapped = kind === 1;
    if (mapped ? depth !== 8 && depth !== 16 : ![8, 15, 16, 24, 32].includes(depth))
        throw new Error(`Unsupported TGA pixel depth ${depth}.`);

    const out = allocate(width, height);
    const alpha = (descriptor & 15) > 0;
    let p = 18 + idLength;
    let palette = null;
    if (mapType === 1) {
        const entrySize = Math.ceil(mapDepth / 8);
        if (p + mapLength * entrySize > s.length) throw truncated();
        palette = s.subarray(p, p + mapLength * entrySize);
        p += mapLength * entrySize;
    }
    if (mapped && !palette) throw new Error('The TGA color map is missing.');

    const color = (buffer, q, bits, o) => {
        if (bits === 8) {
            out[o] = out[o + 1] = out[o + 2] = buffer[q];
            out[o + 3] = 255;
        } else if (bits === 15 || bits === 16) {
            const v = buffer[q] | (buffer[q + 1] << 8);
            out[o] = ((v >> 10) & 31) * 255 / 31;
            out[o + 1] = ((v >> 5) & 31) * 255 / 31;
            out[o + 2] = (v & 31) * 255 / 31;
            out[o + 3] = bits === 16 && alpha && !(v & 0x8000) ? 0 : 255;
        } else {
            out[o] = buffer[q + 2];
            out[o + 1] = buffer[q + 1];
            out[o + 2] = buffer[q];
            out[o + 3] = bits === 32 && alpha ? buffer[q + 3] : 255;
        }
    };
    const entrySize = Math.ceil(mapDepth / 8);
    const pixel = (q, o) => {
        if (!mapped) return color(s, q, depth, o);
        const index = (depth === 8 ? s[q] : s[q] | (s[q + 1] << 8)) - mapFirst;
        if (index < 0 || index >= mapLength) throw new Error('The TGA color index is out of range.');
        color(palette, index * entrySize, mapDepth, o);
    };

    const size = Math.ceil(depth / 8), count = width * height;
    const topDown = (descriptor & 0x20) !== 0, rightToLeft = (descriptor & 0x10) !== 0;
    const target = i => {
        const row = Math.floor(i / width), column = i % width;
        return ((topDown ? row : height - 1 - row) * width + (rightToLeft ? width - 1 - column : column)) * 4;
    };
    for (let i = 0; i < count;) {
        let repeat = 1, raw = true;
        if (rle) {
            if (p >= s.length) throw truncated();
            const header = s[p++];
            repeat = Math.min((header & 127) + 1, count - i);
            raw = (header & 128) === 0;
        }
        if (p + (raw ? repeat : 1) * size > s.length) throw truncated();
        for (let n = 0; n < repeat; n++, i++) {
            pixel(p, target(i));
            if (raw) p += size;
        }
        if (!raw) p += size;
    }
    return { width, height, data: out };
}

function decodeQoi(s) {
    if (s.length < 22 || s[0] !== 113 || s[1] !== 111 || s[2] !== 105 || s[3] !== 102)
        throw new Error('The file is not a QOI image.');
    const view = new DataView(s.buffer, s.byteOffset, s.byteLength);
    const width = view.getUint32(4), height = view.getUint32(8);
    const out = allocate(width, height);
    const index = new Uint8Array(256);
    const end = s.length - 8;
    let r = 0, g = 0, b = 0, a = 255, p = 14, run = 0;
    for (let o = 0; o < out.length; o += 4) {
        if (run > 0) {
            run--;
        } else {
            if (p >= end) throw truncated();
            const op = s[p++];
            if (op === 0xfe) {
                r = s[p++]; g = s[p++]; b = s[p++];
            } else if (op === 0xff) {
                r = s[p++]; g = s[p++]; b = s[p++]; a = s[p++];
            } else if (op >> 6 === 0) {
                const h = op * 4;
                r = index[h]; g = index[h + 1]; b = index[h + 2]; a = index[h + 3];
            } else if (op >> 6 === 1) {
                r = (r + ((op >> 4) & 3) - 2) & 255;
                g = (g + ((op >> 2) & 3) - 2) & 255;
                b = (b + (op & 3) - 2) & 255;
            } else if (op >> 6 === 2) {
                const next = s[p++], vg = (op & 63) - 32;
                r = (r + vg - 8 + (next >> 4)) & 255;
                g = (g + vg) & 255;
                b = (b + vg - 8 + (next & 15)) & 255;
            } else {
                run = op & 63;
            }
            const h = ((r * 3 + g * 5 + b * 7 + a * 11) % 64) * 4;
            index[h] = r; index[h + 1] = g; index[h + 2] = b; index[h + 3] = a;
        }
        out[o] = r; out[o + 1] = g; out[o + 2] = b; out[o + 3] = a;
    }
    return { width, height, data: out };
}

function decodePnm(s) {
    if (s[0] !== 80 || s[1] < 49 || s[1] > 54) throw new Error('The file is not a PBM, PGM or PPM image.');
    const kind = s[1] - 48;
    let p = 2;
    const isSpace = c => c === 32 || (c >= 9 && c <= 13);
    const skip = () => {
        for (;;) {
            while (p < s.length && isSpace(s[p])) p++;
            if (s[p] !== 35) return;
            while (p < s.length && s[p] !== 10 && s[p] !== 13) p++;
        }
    };
    const int = () => {
        skip();
        const start = p;
        let value = 0;
        while (p < s.length && s[p] >= 48 && s[p] <= 57) value = value * 10 + s[p++] - 48;
        if (p === start) throw truncated();
        return value;
    };

    const width = int(), height = int();
    const maxValue = kind === 1 || kind === 4 ? 1 : int();
    if (maxValue < 1 || maxValue > 65535) throw new Error(`Unsupported PNM maximum value ${maxValue}.`);
    const out = allocate(width, height);
    const channels = kind === 3 || kind === 6 ? 3 : 1, wide = maxValue > 255;
    p++; // the single whitespace that ends the header
    if (kind >= 4) {
        const needed = kind === 4 ? Math.ceil(width / 8) * height : width * height * channels * (wide ? 2 : 1);
        if (p + needed > s.length) throw truncated();
    }

    const sample = () => {
        const value = kind <= 3 ? int() : wide ? (s[p++] << 8) | s[p++] : s[p++];
        return Math.min(value, maxValue) * 255 / maxValue;
    };
    for (let y = 0, o = 0; y < height; y++) {
        let bits = 0, bit = 8;
        for (let x = 0; x < width; x++, o += 4) {
            if (kind === 1) {
                skip();
                if (p >= s.length) throw truncated();
                out[o] = out[o + 1] = out[o + 2] = s[p++] === 49 ? 0 : 255;
            } else if (kind === 4) {
                if (bit === 8) { bits = s[p++]; bit = 0; }
                out[o] = out[o + 1] = out[o + 2] = (bits >> (7 - bit++)) & 1 ? 0 : 255;
            } else {
                out[o] = out[o + 1] = out[o + 2] = sample();
                if (channels === 3) { out[o + 1] = sample(); out[o + 2] = sample(); }
            }
            out[o + 3] = 255;
        }
    }
    return { width, height, data: out };
}

function encodeBmp({ width, height, data }) {
    // BITMAPV4HEADER with bit fields, so alpha survives
    const offset = 14 + 108, out = new Uint8Array(offset + data.length), view = new DataView(out.buffer);
    out[0] = 66; out[1] = 77;
    view.setUint32(2, out.length, true);
    view.setUint32(10, offset, true);
    view.setUint32(14, 108, true);
    view.setInt32(18, width, true);
    view.setInt32(22, -height, true); // top-down rows
    view.setUint16(26, 1, true);
    view.setUint16(28, 32, true);
    view.setUint32(30, 3, true); // BI_BITFIELDS
    view.setUint32(34, data.length, true);
    view.setUint32(54, 0x00ff0000, true);
    view.setUint32(58, 0x0000ff00, true);
    view.setUint32(62, 0x000000ff, true);
    view.setUint32(66, 0xff000000, true);
    view.setUint32(70, 0x73524742, true); // 'sRGB'
    writeBgra(data, out, offset);
    return out;
}

function encodeTga({ width, height, data }) {
    if (width > 65535 || height > 65535) throw new Error('TGA images are limited to 65535 pixels per side.');
    const out = new Uint8Array(18 + data.length);
    out[2] = 2;
    out[12] = width & 255; out[13] = width >> 8;
    out[14] = height & 255; out[15] = height >> 8;
    out[16] = 32;
    out[17] = 0x28; // top-down, 8 alpha bits
    writeBgra(data, out, 18);
    return out;
}

function writeBgra(data, out, offset) {
    for (let i = 0; i < data.length; i += 4) {
        out[offset + i] = data[i + 2];
        out[offset + i + 1] = data[i + 1];
        out[offset + i + 2] = data[i];
        out[offset + i + 3] = data[i + 3];
    }
}

function encodeQoi({ width, height, data }) {
    const out = new Uint8Array(14 + width * height * 5 + 8), view = new DataView(out.buffer);
    out.set([113, 111, 105, 102]);
    view.setUint32(4, width);
    view.setUint32(8, height);
    out[12] = 4;
    const index = new Uint8Array(256), last = data.length - 4;
    let p = 14, run = 0, pr = 0, pg = 0, pb = 0, pa = 255;
    for (let o = 0; o < data.length; o += 4) {
        const r = data[o], g = data[o + 1], b = data[o + 2], a = data[o + 3];
        if (r === pr && g === pg && b === pb && a === pa) {
            if (++run === 62 || o === last) { out[p++] = 0xc0 | (run - 1); run = 0; }
            continue;
        }
        if (run > 0) { out[p++] = 0xc0 | (run - 1); run = 0; }
        const h = ((r * 3 + g * 5 + b * 7 + a * 11) % 64) * 4;
        if (index[h] === r && index[h + 1] === g && index[h + 2] === b && index[h + 3] === a) {
            out[p++] = h / 4;
        } else {
            index[h] = r; index[h + 1] = g; index[h + 2] = b; index[h + 3] = a;
            const vr = (r - pr) << 24 >> 24, vg = (g - pg) << 24 >> 24, vb = (b - pb) << 24 >> 24;
            const vgr = vr - vg, vgb = vb - vg;
            if (a !== pa) {
                out.set([0xff, r, g, b, a], p); p += 5;
            } else if (vr > -3 && vr < 2 && vg > -3 && vg < 2 && vb > -3 && vb < 2) {
                out[p++] = 0x40 | ((vr + 2) << 4) | ((vg + 2) << 2) | (vb + 2);
            } else if (vgr > -9 && vgr < 8 && vg > -33 && vg < 32 && vgb > -9 && vgb < 8) {
                out[p++] = 0x80 | (vg + 32);
                out[p++] = ((vgr + 8) << 4) | (vgb + 8);
            } else {
                out.set([0xfe, r, g, b], p); p += 4;
            }
        }
        pr = r; pg = g; pb = b; pa = a;
    }
    out.set([0, 0, 0, 0, 0, 0, 0, 1], p);
    return out.subarray(0, p + 8);
}

function encodePpm({ width, height, data }) {
    const header = new TextEncoder().encode(`P6\n${width} ${height}\n255\n`);
    const out = new Uint8Array(header.length + width * height * 3);
    out.set(header);
    for (let i = 0, p = header.length; i < data.length; i += 4) {
        out[p++] = data[i]; out[p++] = data[i + 1]; out[p++] = data[i + 2];
    }
    return out;
}
