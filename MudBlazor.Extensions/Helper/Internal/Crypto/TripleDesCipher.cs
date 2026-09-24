namespace MudBlazor.Extensions.Helper.Internal.Crypto;

/// <summary>
/// DES and three key triple DES in CBC mode, decryption only.
/// </summary>
/// <remarks>
/// The browser runtime of .NET has no symmetric ciphers at all, and SubtleCrypto never had DES, so a pfx
/// encrypted with pbeWithSHAAnd3-KeyTripleDES-CBC could not be opened in a Blazor WebAssembly app. This is
/// the plain FIPS 46-3 algorithm, used to read such a container - nothing here encrypts anything, and neither
/// DES nor triple DES should be used for that any more.
/// </remarks>
internal static class TripleDesCipher
{
    private static readonly int[] InitialPermutation =
    {
        58, 50, 42, 34, 26, 18, 10, 2, 60, 52, 44, 36, 28, 20, 12, 4,
        62, 54, 46, 38, 30, 22, 14, 6, 64, 56, 48, 40, 32, 24, 16, 8,
        57, 49, 41, 33, 25, 17, 9, 1, 59, 51, 43, 35, 27, 19, 11, 3,
        61, 53, 45, 37, 29, 21, 13, 5, 63, 55, 47, 39, 31, 23, 15, 7
    };

    private static readonly int[] FinalPermutation =
    {
        40, 8, 48, 16, 56, 24, 64, 32, 39, 7, 47, 15, 55, 23, 63, 31,
        38, 6, 46, 14, 54, 22, 62, 30, 37, 5, 45, 13, 53, 21, 61, 29,
        36, 4, 44, 12, 52, 20, 60, 28, 35, 3, 43, 11, 51, 19, 59, 27,
        34, 2, 42, 10, 50, 18, 58, 26, 33, 1, 41, 9, 49, 17, 57, 25
    };

    private static readonly int[] Expansion =
    {
        32, 1, 2, 3, 4, 5, 4, 5, 6, 7, 8, 9, 8, 9, 10, 11, 12, 13,
        12, 13, 14, 15, 16, 17, 16, 17, 18, 19, 20, 21, 20, 21, 22, 23, 24, 25,
        24, 25, 26, 27, 28, 29, 28, 29, 30, 31, 32, 1
    };

    private static readonly int[] Permutation =
    {
        16, 7, 20, 21, 29, 12, 28, 17, 1, 15, 23, 26, 5, 18, 31, 10,
        2, 8, 24, 14, 32, 27, 3, 9, 19, 13, 30, 6, 22, 11, 4, 25
    };

    private static readonly int[] PermutedChoice1 =
    {
        57, 49, 41, 33, 25, 17, 9, 1, 58, 50, 42, 34, 26, 18,
        10, 2, 59, 51, 43, 35, 27, 19, 11, 3, 60, 52, 44, 36,
        63, 55, 47, 39, 31, 23, 15, 7, 62, 54, 46, 38, 30, 22,
        14, 6, 61, 53, 45, 37, 29, 21, 13, 5, 28, 20, 12, 4
    };

    private static readonly int[] PermutedChoice2 =
    {
        14, 17, 11, 24, 1, 5, 3, 28, 15, 6, 21, 10,
        23, 19, 12, 4, 26, 8, 16, 7, 27, 20, 13, 2,
        41, 52, 31, 37, 47, 55, 30, 40, 51, 45, 33, 48,
        44, 49, 39, 56, 34, 53, 46, 42, 50, 36, 29, 32
    };

    private static readonly int[] Shifts = { 1, 1, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2, 2, 2, 2, 1 };

    private static readonly byte[,,] SBoxes = BuildSBoxes();

    /// <summary>
    /// Decrypts data that was encrypted with three key triple DES in CBC mode and strips the padding.
    /// </summary>
    /// <param name="key">24 bytes: the three DES keys.</param>
    /// <param name="iv">8 bytes.</param>
    public static byte[] DecryptCbc(byte[] key, byte[] iv, byte[] data)
    {
        if (key is not { Length: 24 } || iv is not { Length: 8 } || data == null || data.Length % 8 != 0 || data.Length == 0)
            return null;

        var first = Schedule(key.AsSpan(0, 8).ToArray());
        var second = Schedule(key.AsSpan(8, 8).ToArray());
        var third = Schedule(key.AsSpan(16, 8).ToArray());

        var result = new byte[data.Length];
        var previous = (byte[])iv.Clone();
        var block = new byte[8];

        for (var offset = 0; offset < data.Length; offset += 8)
        {
            Buffer.BlockCopy(data, offset, block, 0, 8);

            // EDE in reverse: decrypt with the third key, encrypt with the second, decrypt with the first.
            var value = ToBlock(block);
            value = Process(value, third, decrypt: true);
            value = Process(value, second, decrypt: false);
            value = Process(value, first, decrypt: true);

            var plain = FromBlock(value);
            for (var i = 0; i < 8; i++)
                result[offset + i] = (byte)(plain[i] ^ previous[i]);

            Buffer.BlockCopy(block, 0, previous, 0, 8);
        }

        return RemovePadding(result);
    }

    /// <summary>
    /// Decrypts without touching the padding. Only the known answer tests use this; a container always ends
    /// on padding and goes through <see cref="DecryptCbc"/>.
    /// </summary>
    internal static byte[] DecryptCbcRaw(byte[] key, byte[] iv, byte[] data)
    {
        var first = Schedule(key.AsSpan(0, 8).ToArray());
        var second = Schedule(key.AsSpan(8, 8).ToArray());
        var third = Schedule(key.AsSpan(16, 8).ToArray());

        var result = new byte[data.Length];
        var previous = (byte[])iv.Clone();
        var block = new byte[8];

        for (var offset = 0; offset < data.Length; offset += 8)
        {
            Buffer.BlockCopy(data, offset, block, 0, 8);
            var value = ToBlock(block);
            value = Process(value, third, decrypt: true);
            value = Process(value, second, decrypt: false);
            value = Process(value, first, decrypt: true);

            var plain = FromBlock(value);
            for (var i = 0; i < 8; i++)
                result[offset + i] = (byte)(plain[i] ^ previous[i]);

            Buffer.BlockCopy(block, 0, previous, 0, 8);
        }

        return result;
    }

    /// <summary>Strips PKCS#7 padding, or returns null when it is not valid - which is how a wrong key shows.</summary>
    internal static byte[] RemovePadding(byte[] data)
    {
        if (data.Length == 0)
            return null;

        var padding = data[^1];
        if (padding == 0 || padding > 8 || padding > data.Length)
            return null;

        for (var i = data.Length - padding; i < data.Length; i++)
        {
            if (data[i] != padding)
                return null;
        }

        var result = new byte[data.Length - padding];
        Buffer.BlockCopy(data, 0, result, 0, result.Length);
        return result;
    }

    private static ulong Process(ulong block, ulong[] subKeys, bool decrypt)
    {
        var permuted = Permute(block, InitialPermutation, 64);
        var left = (uint)(permuted >> 32);
        var right = (uint)permuted;

        for (var round = 0; round < 16; round++)
        {
            var key = subKeys[decrypt ? 15 - round : round];
            var temp = right;
            right = left ^ Feistel(right, key);
            left = temp;
        }

        var combined = ((ulong)right << 32) | left;
        return Permute(combined, FinalPermutation, 64);
    }

    private static uint Feistel(uint right, ulong subKey)
    {
        var expanded = Permute(right, Expansion, 32, 48) ^ subKey;

        uint output = 0;
        for (var box = 0; box < 8; box++)
        {
            var six = (int)((expanded >> (42 - box * 6)) & 0x3F);
            var row = ((six & 0x20) >> 4) | (six & 0x01);
            var column = (six >> 1) & 0x0F;
            output = (output << 4) | SBoxes[box, row, column];
        }

        return (uint)Permute(output, Permutation, 32);
    }

    private static ulong[] Schedule(byte[] key)
    {
        var permuted = Permute(ToBlock(key), PermutedChoice1, 64, 56);
        var left = (uint)((permuted >> 28) & 0x0FFFFFFF);
        var right = (uint)(permuted & 0x0FFFFFFF);

        var subKeys = new ulong[16];
        for (var round = 0; round < 16; round++)
        {
            left = Rotate(left, Shifts[round]);
            right = Rotate(right, Shifts[round]);
            subKeys[round] = Permute(((ulong)left << 28) | right, PermutedChoice2, 56, 48);
        }

        return subKeys;
    }

    private static uint Rotate(uint value, int amount)
        => ((value << amount) | (value >> (28 - amount))) & 0x0FFFFFFF;

    /// <summary>Applies a permutation table, counting bits from the most significant one as the standard does.</summary>
    private static ulong Permute(ulong value, int[] table, int inputBits, int outputBits = 0)
    {
        outputBits = outputBits == 0 ? table.Length : outputBits;
        ulong result = 0;

        for (var i = 0; i < table.Length; i++)
        {
            var bit = (value >> (inputBits - table[i])) & 1;
            result |= bit << (outputBits - 1 - i);
        }

        return result;
    }

    private static ulong ToBlock(byte[] bytes)
    {
        ulong value = 0;
        for (var i = 0; i < 8; i++)
            value = (value << 8) | bytes[i];

        return value;
    }

    private static byte[] FromBlock(ulong value)
    {
        var bytes = new byte[8];
        for (var i = 7; i >= 0; i--)
        {
            bytes[i] = (byte)value;
            value >>= 8;
        }

        return bytes;
    }

    private static byte[,,] BuildSBoxes()
    {
        int[][] flat =
        {
            new[] { 14, 4, 13, 1, 2, 15, 11, 8, 3, 10, 6, 12, 5, 9, 0, 7, 0, 15, 7, 4, 14, 2, 13, 1, 10, 6, 12, 11, 9, 5, 3, 8, 4, 1, 14, 8, 13, 6, 2, 11, 15, 12, 9, 7, 3, 10, 5, 0, 15, 12, 8, 2, 4, 9, 1, 7, 5, 11, 3, 14, 10, 0, 6, 13 },
            new[] { 15, 1, 8, 14, 6, 11, 3, 4, 9, 7, 2, 13, 12, 0, 5, 10, 3, 13, 4, 7, 15, 2, 8, 14, 12, 0, 1, 10, 6, 9, 11, 5, 0, 14, 7, 11, 10, 4, 13, 1, 5, 8, 12, 6, 9, 3, 2, 15, 13, 8, 10, 1, 3, 15, 4, 2, 11, 6, 7, 12, 0, 5, 14, 9 },
            new[] { 10, 0, 9, 14, 6, 3, 15, 5, 1, 13, 12, 7, 11, 4, 2, 8, 13, 7, 0, 9, 3, 4, 6, 10, 2, 8, 5, 14, 12, 11, 15, 1, 13, 6, 4, 9, 8, 15, 3, 0, 11, 1, 2, 12, 5, 10, 14, 7, 1, 10, 13, 0, 6, 9, 8, 7, 4, 15, 14, 3, 11, 5, 2, 12 },
            new[] { 7, 13, 14, 3, 0, 6, 9, 10, 1, 2, 8, 5, 11, 12, 4, 15, 13, 8, 11, 5, 6, 15, 0, 3, 4, 7, 2, 12, 1, 10, 14, 9, 10, 6, 9, 0, 12, 11, 7, 13, 15, 1, 3, 14, 5, 2, 8, 4, 3, 15, 0, 6, 10, 1, 13, 8, 9, 4, 5, 11, 12, 7, 2, 14 },
            new[] { 2, 12, 4, 1, 7, 10, 11, 6, 8, 5, 3, 15, 13, 0, 14, 9, 14, 11, 2, 12, 4, 7, 13, 1, 5, 0, 15, 10, 3, 9, 8, 6, 4, 2, 1, 11, 10, 13, 7, 8, 15, 9, 12, 5, 6, 3, 0, 14, 11, 8, 12, 7, 1, 14, 2, 13, 6, 15, 0, 9, 10, 4, 5, 3 },
            new[] { 12, 1, 10, 15, 9, 2, 6, 8, 0, 13, 3, 4, 14, 7, 5, 11, 10, 15, 4, 2, 7, 12, 9, 5, 6, 1, 13, 14, 0, 11, 3, 8, 9, 14, 15, 5, 2, 8, 12, 3, 7, 0, 4, 10, 1, 13, 11, 6, 4, 3, 2, 12, 9, 5, 15, 10, 11, 14, 1, 7, 6, 0, 8, 13 },
            new[] { 4, 11, 2, 14, 15, 0, 8, 13, 3, 12, 9, 7, 5, 10, 6, 1, 13, 0, 11, 7, 4, 9, 1, 10, 14, 3, 5, 12, 2, 15, 8, 6, 1, 4, 11, 13, 12, 3, 7, 14, 10, 15, 6, 8, 0, 5, 9, 2, 6, 11, 13, 8, 1, 4, 10, 7, 9, 5, 0, 15, 14, 2, 3, 12 },
            new[] { 13, 2, 8, 4, 6, 15, 11, 1, 10, 9, 3, 14, 5, 0, 12, 7, 1, 15, 13, 8, 10, 3, 7, 4, 12, 5, 6, 11, 0, 14, 9, 2, 7, 11, 4, 1, 9, 12, 14, 2, 0, 6, 10, 13, 15, 3, 5, 8, 2, 1, 14, 7, 4, 10, 8, 13, 15, 12, 9, 0, 3, 5, 6, 11 }
        };

        var boxes = new byte[8, 4, 16];
        for (var box = 0; box < 8; box++)
        {
            for (var row = 0; row < 4; row++)
            {
                for (var column = 0; column < 16; column++)
                    boxes[box, row, column] = (byte)flat[box][row * 16 + column];
            }
        }

        return boxes;
    }
}
