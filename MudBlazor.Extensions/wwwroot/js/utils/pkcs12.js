/**
 * PBES2 decryption for MudExFileDisplayCertificate.
 *
 * The browser runtime of .NET has no symmetric ciphers, so a password protected pfx cannot be opened from
 * managed code alone - it fails with "unknown algorithm identifier 1.2.840.113549.1.5.13", which is PBES2.
 * The browser itself has PBKDF2 and AES-CBC in SubtleCrypto, so C# parses the container and asks here for the
 * one step it cannot do. Everything crosses the boundary as base64.
 */
window.MudExPkcs12 = {
    /**
     * Derives the key from the password and decrypts the content.
     *
     * @param password the password as typed
     * @param saltBase64 salt of the key derivation
     * @param iterations iteration count of the key derivation
     * @param hash digest of the pseudo random function: SHA-1, SHA-256, SHA-384 or SHA-512
     * @param keyBits length of the derived key in bits: 128, 192 or 256
     * @param ivBase64 initialisation vector of the cipher
     * @param dataBase64 the encrypted content
     * @returns the plaintext as base64, or null when the password does not fit
     */
    decryptPbes2: async function (password, saltBase64, iterations, hash, keyBits, ivBase64, dataBase64) {
        try {
            const material = await crypto.subtle.importKey(
                'raw', new TextEncoder().encode(password), 'PBKDF2', false, ['deriveKey']);

            const key = await crypto.subtle.deriveKey(
                { name: 'PBKDF2', salt: toBytes(saltBase64), iterations: iterations, hash: hash },
                material,
                { name: 'AES-CBC', length: keyBits },
                false,
                ['decrypt']);

            const plain = await crypto.subtle.decrypt(
                { name: 'AES-CBC', iv: toBytes(ivBase64) }, key, toBytes(dataBase64));

            return toBase64(new Uint8Array(plain));
        } catch (e) {
            // A wrong password fails the padding check, which is how the caller learns it was wrong.
            return null;
        }
    }
};

function toBytes(base64) {
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
        bytes[i] = binary.charCodeAt(i);
    }
    return bytes;
}

function toBase64(bytes) {
    let binary = '';
    // Chunked, because spreading a large array into fromCharCode blows the argument limit.
    for (let i = 0; i < bytes.length; i += 0x8000) {
        binary += String.fromCharCode.apply(null, bytes.subarray(i, i + 0x8000));
    }
    return btoa(binary);
}
