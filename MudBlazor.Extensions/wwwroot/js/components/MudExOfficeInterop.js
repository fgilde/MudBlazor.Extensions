const INLINE_CHARS = 8000;

export const encode = value => new TextEncoder().encode(JSON.stringify(value ?? null));

export function payload(value) {
    const json = JSON.stringify(value ?? null);
    return json.length <= INLINE_CHARS ? [value ?? null, null] : [null, DotNet.createJSStreamReference(new TextEncoder().encode(json))];
}
