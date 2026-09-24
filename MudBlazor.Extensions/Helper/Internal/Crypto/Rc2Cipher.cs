namespace MudBlazor.Extensions.Helper.Internal.Crypto;

/// <summary>
/// RC2 in CBC mode, decryption only, as described in RFC 2268.
/// </summary>
/// <remarks>
/// .NET dropped RC2 with .NET Core, so a pfx written by an older tool - those use pbeWithSHAAnd40BitRC2-CBC
/// for the certificates - cannot be opened anywhere on a current runtime, browser or not. This reads such a
/// container. RC2 with 40 bits is long broken and has no business protecting anything; it is implemented here
/// to look at old files, not to write new ones.
/// </remarks>
internal static class Rc2Cipher
{
    private static readonly byte[] PiTable =
    {
        0xd9, 0x78, 0xf9, 0xc4, 0x19, 0xdd, 0xb5, 0xed, 0x28, 0xe9, 0xfd, 0x79, 0x4a, 0xa0, 0xd8, 0x9d,
        0xc6, 0x7e, 0x37, 0x83, 0x2b, 0x76, 0x53, 0x8e, 0x62, 0x4c, 0x64, 0x88, 0x44, 0x8b, 0xfb, 0xa2,
        0x17, 0x9a, 0x59, 0xf5, 0x87, 0xb3, 0x4f, 0x13, 0x61, 0x45, 0x6d, 0x8d, 0x09, 0x81, 0x7d, 0x32,
        0xbd, 0x8f, 0x40, 0xeb, 0x86, 0xb7, 0x7b, 0x0b, 0xf0, 0x95, 0x21, 0x22, 0x5c, 0x6b, 0x4e, 0x82,
        0x54, 0xd6, 0x65, 0x93, 0xce, 0x60, 0xb2, 0x1c, 0x73, 0x56, 0xc0, 0x14, 0xa7, 0x8c, 0xf1, 0xdc,
        0x12, 0x75, 0xca, 0x1f, 0x3b, 0xbe, 0xe4, 0xd1, 0x42, 0x3d, 0xd4, 0x30, 0xa3, 0x3c, 0xb6, 0x26,
        0x6f, 0xbf, 0x0e, 0xda, 0x46, 0x69, 0x07, 0x57, 0x27, 0xf2, 0x1d, 0x9b, 0xbc, 0x94, 0x43, 0x03,
        0xf8, 0x11, 0xc7, 0xf6, 0x90, 0xef, 0x3e, 0xe7, 0x06, 0xc3, 0xd5, 0x2f, 0xc8, 0x66, 0x1e, 0xd7,
        0x08, 0xe8, 0xea, 0xde, 0x80, 0x52, 0xee, 0xf7, 0x84, 0xaa, 0x72, 0xac, 0x35, 0x4d, 0x6a, 0x2a,
        0x96, 0x1a, 0xd2, 0x71, 0x5a, 0x15, 0x49, 0x74, 0x4b, 0x9f, 0xd0, 0x5e, 0x04, 0x18, 0xa4, 0xec,
        0xc2, 0xe0, 0x41, 0x6e, 0x0f, 0x51, 0xcb, 0xcc, 0x24, 0x91, 0xaf, 0x50, 0xa1, 0xf4, 0x70, 0x39,
        0x99, 0x7c, 0x3a, 0x85, 0x23, 0xb8, 0xb4, 0x7a, 0xfc, 0x02, 0x36, 0x5b, 0x25, 0x55, 0x97, 0x31,
        0x2d, 0x5d, 0xfa, 0x98, 0xe3, 0x8a, 0x92, 0xae, 0x05, 0xdf, 0x29, 0x10, 0x67, 0x6c, 0xba, 0xc9,
        0xd3, 0x00, 0xe6, 0xcf, 0xe1, 0x9e, 0xa8, 0x2c, 0x63, 0x16, 0x01, 0x3f, 0x58, 0xe2, 0x89, 0xa9,
        0x0d, 0x38, 0x34, 0x1b, 0xab, 0x33, 0xff, 0xb0, 0xbb, 0x48, 0x0c, 0x5f, 0xb9, 0xb1, 0xcd, 0x2e,
        0xc5, 0xf3, 0xdb, 0x47, 0xe5, 0xa5, 0x9c, 0x77, 0x0a, 0xa6, 0x20, 0x68, 0xfe, 0x7f, 0xc1, 0xad
    };

    /// <summary>
    /// Decrypts data encrypted with RC2 in CBC mode and strips the padding.
    /// </summary>
    /// <param name="key">The key, as long as the scheme says - five bytes for the 40 bit variant.</param>
    /// <param name="effectiveBits">Effective key length in bits, 40 or 128 for the pfx schemes.</param>
    public static byte[] DecryptCbc(byte[] key, int effectiveBits, byte[] iv, byte[] data)
    {
        if (key is not { Length: > 0 } || iv is not { Length: 8 } || data == null || data.Length % 8 != 0 || data.Length == 0)
            return null;

        var expanded = ExpandKey(key, effectiveBits);
        var result = new byte[data.Length];
        var previous = (byte[])iv.Clone();
        var block = new byte[8];

        for (var offset = 0; offset < data.Length; offset += 8)
        {
            Buffer.BlockCopy(data, offset, block, 0, 8);
            var plain = DecryptBlock(expanded, block);

            for (var i = 0; i < 8; i++)
                result[offset + i] = (byte)(plain[i] ^ previous[i]);

            Buffer.BlockCopy(block, 0, previous, 0, 8);
        }

        return TripleDesCipher.RemovePadding(result);
    }

    /// <summary>One block through the cipher, for the known answer tests of RFC 2268.</summary>
    internal static byte[] DecryptBlockRaw(byte[] key, int effectiveBits, byte[] block)
        => DecryptBlock(ExpandKey(key, effectiveBits), block);

    /// <summary>The key schedule of RFC 2268, section 2.</summary>
    private static ushort[] ExpandKey(byte[] key, int effectiveBits)
    {
        var l = new byte[128];
        Buffer.BlockCopy(key, 0, l, 0, key.Length);

        for (var i = key.Length; i < 128; i++)
            l[i] = PiTable[(l[i - 1] + l[i - key.Length]) & 0xFF];

        var t8 = (effectiveBits + 7) / 8;
        var tm = (byte)(255 % (1 << (8 + effectiveBits - 8 * t8)));

        l[128 - t8] = PiTable[l[128 - t8] & tm];
        for (var i = 127 - t8; i >= 0; i--)
            l[i] = PiTable[l[i + 1] ^ l[i + t8]];

        var expanded = new ushort[64];
        for (var i = 0; i < 64; i++)
            expanded[i] = (ushort)(l[2 * i] + (l[2 * i + 1] << 8));

        return expanded;
    }

    private static byte[] DecryptBlock(ushort[] key, byte[] block)
    {
        var x0 = (ushort)(block[0] | (block[1] << 8));
        var x1 = (ushort)(block[2] | (block[3] << 8));
        var x2 = (ushort)(block[4] | (block[5] << 8));
        var x3 = (ushort)(block[6] | (block[7] << 8));

        var j = 63;

        // The rounds of the encryption, run backwards: five mixes, one mash, six mixes, one mash, five mixes.
        RMixBack(ref x0, ref x1, ref x2, ref x3, key, ref j, 5);
        RMashBack(ref x0, ref x1, ref x2, ref x3, key);
        RMixBack(ref x0, ref x1, ref x2, ref x3, key, ref j, 6);
        RMashBack(ref x0, ref x1, ref x2, ref x3, key);
        RMixBack(ref x0, ref x1, ref x2, ref x3, key, ref j, 5);

        return new[]
        {
            (byte)x0, (byte)(x0 >> 8),
            (byte)x1, (byte)(x1 >> 8),
            (byte)x2, (byte)(x2 >> 8),
            (byte)x3, (byte)(x3 >> 8)
        };
    }

    private static void RMixBack(ref ushort x0, ref ushort x1, ref ushort x2, ref ushort x3, ushort[] key, ref int j, int rounds)
    {
        for (var round = 0; round < rounds; round++)
        {
            x3 = (ushort)(RotateRight(x3, 5) - key[j--] - (x2 & x1) - (~x2 & x0));
            x2 = (ushort)(RotateRight(x2, 3) - key[j--] - (x1 & x0) - (~x1 & x3));
            x1 = (ushort)(RotateRight(x1, 2) - key[j--] - (x0 & x3) - (~x0 & x2));
            x0 = (ushort)(RotateRight(x0, 1) - key[j--] - (x3 & x2) - (~x3 & x1));
        }
    }

    private static void RMashBack(ref ushort x0, ref ushort x1, ref ushort x2, ref ushort x3, ushort[] key)
    {
        x3 = (ushort)(x3 - key[x2 & 63]);
        x2 = (ushort)(x2 - key[x1 & 63]);
        x1 = (ushort)(x1 - key[x0 & 63]);
        x0 = (ushort)(x0 - key[x3 & 63]);
    }

    private static ushort RotateRight(ushort value, int amount)
        => (ushort)((value >> amount) | (value << (16 - amount)));
}
