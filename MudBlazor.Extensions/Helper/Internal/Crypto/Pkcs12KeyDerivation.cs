using System.Security.Cryptography;
using System.Text;

namespace MudBlazor.Extensions.Helper.Internal.Crypto;

/// <summary>
/// The key derivation of PKCS#12, RFC 7292 appendix B.2.
/// </summary>
/// <remarks>
/// The older pfx encryption schemes - the ones named pbeWithSHAAnd... - derive their key and their
/// initialisation vector with this, not with PBKDF2. It is only ever used to read such a container; nothing
/// here writes one.
/// </remarks>
internal static class Pkcs12KeyDerivation
{
    /// <summary>What the derived bytes are for. The id goes into the diversifier of the derivation.</summary>
    public enum Purpose
    {
        /// <summary>The encryption key.</summary>
        Key = 1,

        /// <summary>The initialisation vector.</summary>
        InitialisationVector = 2,

        /// <summary>The mac key.</summary>
        Mac = 3
    }

    /// <summary>
    /// Derives <paramref name="length"/> bytes from the password.
    /// </summary>
    /// <param name="password">The password as typed; it is used as a BMPString, which is what the standard says.</param>
    public static byte[] Derive(string password, byte[] salt, int iterations, Purpose purpose, int length)
    {
        // v is the block size of the hash in bytes, u its output size. SHA-1 is what every one of these
        // schemes uses.
        const int v = 64;
        const int u = 20;

        var diversifier = new byte[v];
        Array.Fill(diversifier, (byte)purpose);

        var expandedSalt = Expand(salt, v);
        var expandedPassword = Expand(ToBmpString(password), v);

        var i = new byte[expandedSalt.Length + expandedPassword.Length];
        Buffer.BlockCopy(expandedSalt, 0, i, 0, expandedSalt.Length);
        Buffer.BlockCopy(expandedPassword, 0, i, expandedSalt.Length, expandedPassword.Length);

        var result = new byte[length];
        var written = 0;

        while (written < length)
        {
            using var sha = SHA1.Create();
            var a = sha.ComputeHash(Concat(diversifier, i));
            for (var round = 1; round < iterations; round++)
                a = sha.ComputeHash(a);

            var take = Math.Min(u, length - written);
            Buffer.BlockCopy(a, 0, result, written, take);
            written += take;

            if (written >= length)
                break;

            // B is A repeated to one block, and every block of I is incremented by B + 1.
            var b = Expand(a, v);
            for (var offset = 0; offset < i.Length; offset += v)
                AddOne(i, offset, b, v);
        }

        return result;
    }

    /// <summary>The password as a BMPString: utf-16 big endian with a terminating null character.</summary>
    private static byte[] ToBmpString(string password)
    {
        if (string.IsNullOrEmpty(password))
            return new byte[2];

        var bytes = new byte[(password.Length + 1) * 2];
        Encoding.BigEndianUnicode.GetBytes(password, 0, password.Length, bytes, 0);
        return bytes;
    }

    /// <summary>Repeats the data until it fills a whole number of blocks.</summary>
    private static byte[] Expand(byte[] data, int blockSize)
    {
        if (data.Length == 0)
            return Array.Empty<byte>();

        var length = blockSize * ((data.Length + blockSize - 1) / blockSize);
        var result = new byte[length];
        for (var i = 0; i < length; i++)
            result[i] = data[i % data.Length];

        return result;
    }

    /// <summary>block = block + b + 1, as one big endian number of that size.</summary>
    private static void AddOne(byte[] block, int offset, byte[] b, int size)
    {
        var carry = 1;
        for (var i = size - 1; i >= 0; i--)
        {
            var sum = block[offset + i] + b[i] + carry;
            block[offset + i] = (byte)sum;
            carry = sum >> 8;
        }
    }

    private static byte[] Concat(byte[] first, byte[] second)
    {
        var result = new byte[first.Length + second.Length];
        Buffer.BlockCopy(first, 0, result, 0, first.Length);
        Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
        return result;
    }
}
