using System.Formats.Asn1;
using MudBlazor.Extensions.Helper.Internal.Crypto;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Helper.Internal;

/// <summary>
/// Takes the certificates out of a password protected PKCS#12 container (.pfx, .p12) without the platform
/// certificate stack.
/// </summary>
/// <remarks>
/// Two ways, in that order. On a normal runtime <see cref="Pkcs12Info"/> does all of it in managed code.
/// The browser runtime has no symmetric ciphers, so that path stops at "unknown algorithm identifier
/// 1.2.840.113549.1.5.13" - PBES2 - and the container is then walked here with <see cref="AsnReader"/> while
/// the key derivation and the AES step are handed to the browser, which has both in SubtleCrypto.
/// <para>
/// Private keys are deliberately left alone. A viewer shows what a certificate says; importing a key needs the
/// platform stack and has no business in a file display.
/// </para>
/// </remarks>
internal static class Pkcs12CertificateReader
{
    private const string Data = "1.2.840.113549.1.7.1";
    private const string EncryptedData = "1.2.840.113549.1.7.6";
    private const string Pbes2 = "1.2.840.113549.1.5.13";
    private const string Pbkdf2 = "1.2.840.113549.1.5.12";
    private const string CertBag = "1.2.840.113549.1.12.10.1.3";
    private const string X509Certificate = "1.2.840.113549.1.9.22.1";

    /// <summary>
    /// The older schemes, with the key size and the effective key bits each one uses. They all derive key and
    /// iv the pkcs12 way and differ only in the cipher.
    /// </summary>
    private static readonly Dictionary<string, (int KeySize, int EffectiveBits, bool TripleDes)> LegacySchemes = new()
    {
        ["1.2.840.113549.1.12.1.3"] = (24, 0, true),   // pbeWithSHAAnd3-KeyTripleDES-CBC
        ["1.2.840.113549.1.12.1.4"] = (16, 0, true),   // pbeWithSHAAnd2-KeyTripleDES-CBC
        ["1.2.840.113549.1.12.1.5"] = (16, 128, false), // pbeWithSHAAnd128BitRC2-CBC
        ["1.2.840.113549.1.12.1.6"] = (5, 40, false)   // pbeWithSHAAnd40BitRC2-CBC
    };

    private static readonly Dictionary<string, string> Digests = new()
    {
        ["1.2.840.113549.2.7"] = "SHA-1",
        ["1.2.840.113549.2.9"] = "SHA-256",
        ["1.2.840.113549.2.10"] = "SHA-384",
        ["1.2.840.113549.2.11"] = "SHA-512"
    };

    /// <summary>Names for the encryption schemes a pfx can use, so a refusal can say which one it was.</summary>
    private static readonly Dictionary<string, string> Schemes = new()
    {
        ["1.2.840.113549.1.5.13"] = "PBES2",
        ["1.2.840.113549.1.12.1.1"] = "pbeWithSHAAnd128BitRC4",
        ["1.2.840.113549.1.12.1.2"] = "pbeWithSHAAnd40BitRC4",
        ["1.2.840.113549.1.12.1.3"] = "pbeWithSHAAnd3-KeyTripleDES-CBC",
        ["1.2.840.113549.1.12.1.4"] = "pbeWithSHAAnd2-KeyTripleDES-CBC",
        ["1.2.840.113549.1.12.1.5"] = "pbeWithSHAAnd128BitRC2-CBC",
        ["1.2.840.113549.1.12.1.6"] = "pbeWithSHAAnd40BitRC2-CBC",
        ["1.2.840.113549.1.5.3"] = "pbeWithMD5AndDES-CBC",
        ["1.2.840.113549.1.5.10"] = "pbeWithSHA1AndDES-CBC",
        ["1.2.840.113549.3.7"] = "3DES-CBC",
        ["1.3.14.3.2.7"] = "DES-CBC"
    };

    /// <summary>Names the scheme behind an oid, for a message a human can act on.</summary>
    public static string SchemeName(string oid)
        => oid != null && Schemes.TryGetValue(oid, out var name) ? $"{name} ({oid})" : oid;

    private static readonly Dictionary<string, int> AesKeySizes = new()
    {
        ["2.16.840.1.101.3.4.1.2"] = 128,
        ["2.16.840.1.101.3.4.1.22"] = 192,
        ["2.16.840.1.101.3.4.1.42"] = 256
    };

    /// <summary>
    /// Returns the encoded certificates of the container.
    /// </summary>
    /// <exception cref="CryptographicException">The password is wrong, or neither way can read the container -
    /// a pfx in the legacy format encrypts with RC2, which no current .NET carries.</exception>
    public static async Task<List<byte[]>> ReadCertificatesAsync(byte[] data, string password, IJSRuntime js)
    {
        try
        {
            return ReadWithRuntime(data, password);
        }
        catch (Exception e) when (e is CryptographicException or PlatformNotSupportedException)
        {
            // The runtime has no cipher for this container: walk it here. The older schemes are decrypted in
            // managed code, PBES2 needs the browser - which is exactly the one case where a runtime is around.
            var result = await ReadManuallyAsync(data, password, js);
            if (result.Certificates.Count > 0)
                return result.Certificates;

            // Telling these apart matters: one is fixed by typing again, the others never work here.
            if (result.WrongPassword)
                throw new CryptographicException("The container could not be decrypted.", e);

            // Only when the runtime itself refused the algorithm. A padding failure means the cipher was
            // understood and the password was not right, and that answer must not be overwritten.
            if (result.UnsupportedScheme != null && NamesAnUnknownAlgorithm(e))
                throw new UnsupportedCipherException(result.UnsupportedScheme, e);

            throw;
        }
    }

    private static bool NamesAnUnknownAlgorithm(Exception exception)
        => exception is PlatformNotSupportedException
           || exception.Message.Contains("algorithm", StringComparison.OrdinalIgnoreCase)
           || exception.Message.Contains("AlgorithmIdentifier", StringComparison.OrdinalIgnoreCase);

    /// <summary>The container is fine, but nothing here can decrypt it.</summary>
    internal sealed class UnsupportedCipherException : CryptographicException
    {
        public UnsupportedCipherException(string scheme, Exception inner)
            : base($"The container is encrypted with {scheme}.", inner) => Scheme = scheme;

        /// <summary>Name and oid of the scheme the container uses.</summary>
        public string Scheme { get; }
    }

    /// <summary>The managed path, which is all a normal runtime needs.</summary>
    private static List<byte[]> ReadWithRuntime(byte[] data, string password)
    {
        var info = Pkcs12Info.Decode(data, out _, skipCopy: true);
        var certificates = new List<byte[]>();

        foreach (var contents in info.AuthenticatedSafe)
        {
            if (contents.ConfidentialityMode == Pkcs12ConfidentialityMode.Password)
                contents.Decrypt(password);

            foreach (var bag in contents.GetBags())
            {
                if (bag is Pkcs12CertBag { IsX509Certificate: true } certificate)
                    certificates.Add(Unwrap(certificate.EncodedCertificate));
            }
        }

        if (certificates.Count == 0)
            throw new CryptographicException("The container holds no X.509 certificate.");

        return certificates;
    }

    /// <summary>
    /// Walks the container by hand: the legacy schemes are decrypted here, PBES2 in the browser.
    /// </summary>
    private static async Task<(List<byte[]> Certificates, bool WrongPassword, string UnsupportedScheme)>
        ReadManuallyAsync(byte[] data, string password, IJSRuntime js)
    {
        var certificates = new List<byte[]>();
        var wrongPassword = false;
        string unsupported = null;

        // PFX ::= SEQUENCE { version INTEGER, authSafe ContentInfo, macData MacData OPTIONAL }
        var pfx = new AsnReader(data, AsnEncodingRules.BER).ReadSequence();
        pfx.ReadInteger();

        var authenticatedSafe = ReadContentInfo(pfx, out var authenticatedSafeType);
        if (authenticatedSafeType != Data)
            return (certificates, false, null);

        // AuthenticatedSafe ::= SEQUENCE OF ContentInfo
        var safes = new AsnReader(authenticatedSafe, AsnEncodingRules.BER).ReadSequence();
        while (safes.HasData)
        {
            var contentInfo = safes.ReadSequence();
            var type = contentInfo.ReadObjectIdentifier();
            var content = contentInfo.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0, true));

            byte[] safeContents;
            if (type == Data)
            {
                safeContents = content.ReadOctetString();
            }
            else if (type == EncryptedData)
            {
                var (plain, handled, scheme) = await DecryptAsync(content, password, js);
                safeContents = plain;

                // The cipher was one we know and it still produced nothing: that is the password.
                wrongPassword |= handled && plain == null;
                unsupported ??= handled ? null : scheme;
            }
            else
            {
                safeContents = null;
            }

            if (safeContents != null)
                certificates.AddRange(ReadSafeContents(safeContents));
        }

        return (certificates, wrongPassword && certificates.Count == 0,
            certificates.Count == 0 ? unsupported : null);
    }

    /// <summary>
    /// EncryptedData ::= SEQUENCE { version, encryptedContentInfo }. Only PBES2 is handled: the legacy pfx
    /// ciphers are RC2 and 3DES with the pkcs12 key derivation, and the browser has neither.
    /// </summary>
    private static async Task<(byte[] Plain, bool Handled, string Scheme)> DecryptAsync(
        AsnReader content, string password, IJSRuntime js)
    {
        var encryptedData = content.ReadSequence();
        encryptedData.ReadInteger();

        var encryptedContentInfo = encryptedData.ReadSequence();
        encryptedContentInfo.ReadObjectIdentifier();

        var algorithm = encryptedContentInfo.ReadSequence();
        var scheme = algorithm.ReadObjectIdentifier();

        if (LegacySchemes.TryGetValue(scheme, out var legacy))
        {
            // pkcs-12PbeParams ::= SEQUENCE { salt OCTET STRING, iterations INTEGER }
            var pbeParameters = algorithm.ReadSequence();
            var pbeSalt = pbeParameters.ReadOctetString();
            var pbeIterations = (int)pbeParameters.ReadInteger();
            var pbeContent = encryptedContentInfo.ReadOctetString(new Asn1Tag(TagClass.ContextSpecific, 0));

            var derivedKey = Pkcs12KeyDerivation.Derive(password, pbeSalt, pbeIterations,
                Pkcs12KeyDerivation.Purpose.Key, legacy.KeySize);
            var derivedIv = Pkcs12KeyDerivation.Derive(password, pbeSalt, pbeIterations,
                Pkcs12KeyDerivation.Purpose.InitialisationVector, 8);

            if (legacy.TripleDes && legacy.KeySize == 16)
            {
                // Two key triple DES is three key triple DES with the first key used again as the third.
                var expanded = new byte[24];
                Buffer.BlockCopy(derivedKey, 0, expanded, 0, 16);
                Buffer.BlockCopy(derivedKey, 0, expanded, 16, 8);
                derivedKey = expanded;
            }

            var decrypted = legacy.TripleDes
                ? TripleDesCipher.DecryptCbc(derivedKey, derivedIv, pbeContent)
                : Rc2Cipher.DecryptCbc(derivedKey, legacy.EffectiveBits, derivedIv, pbeContent);

            // Handled either way: a null result here is the password, not the algorithm.
            return (decrypted, true, null);
        }

        // Everything below is PBES2, and its cipher is the one step that needs the browser.
        if (scheme != Pbes2 || js == null)
            return (null, false, SchemeName(scheme));

        var parameters = algorithm.ReadSequence();
        var derivation = parameters.ReadSequence();
        if (derivation.ReadObjectIdentifier() != Pbkdf2)
            return (null, false, SchemeName(scheme));

        var pbkdf2 = derivation.ReadSequence();
        var salt = pbkdf2.ReadOctetString();
        var iterations = (int)pbkdf2.ReadInteger();

        // keyLength and the prf are both optional, and the defaults are the ones from rfc 8018.
        int? keyLength = null;
        var digest = "SHA-1";
        while (pbkdf2.HasData)
        {
            if (pbkdf2.PeekTag().TagValue == (int)UniversalTagNumber.Integer)
            {
                keyLength = (int)pbkdf2.ReadInteger();
                continue;
            }

            var prf = pbkdf2.ReadSequence();
            digest = Digests.TryGetValue(prf.ReadObjectIdentifier(), out var name) ? name : digest;
        }

        var cipher = parameters.ReadSequence();
        var cipherOid = cipher.ReadObjectIdentifier();
        if (!AesKeySizes.TryGetValue(cipherOid, out var keyBits))
            return (null, false, SchemeName(cipherOid));

        var iv = cipher.ReadOctetString();

        // The encrypted content is [0] IMPLICIT, so it reads as an octet string with that tag.
        var encrypted = encryptedContentInfo.ReadOctetString(new Asn1Tag(TagClass.ContextSpecific, 0));

        var plain = await js.InvokeAsync<string>("MudExPkcs12.decryptPbes2",
            password,
            Convert.ToBase64String(salt),
            iterations,
            digest,
            keyLength * 8 ?? keyBits,
            Convert.ToBase64String(iv),
            Convert.ToBase64String(encrypted));

        return (plain == null ? null : Convert.FromBase64String(plain), true, null);
    }

    /// <summary>SafeContents ::= SEQUENCE OF SafeBag - the certificates of one safe.</summary>
    private static IEnumerable<byte[]> ReadSafeContents(byte[] safeContents)
    {
        AsnReader bags;
        try
        {
            bags = new AsnReader(safeContents, AsnEncodingRules.BER).ReadSequence();
        }
        catch (AsnContentException)
        {
            // Decrypted with the wrong password: the padding held but the content is noise.
            yield break;
        }

        while (bags.HasData)
        {
            byte[] certificate = null;
            try
            {
                var bag = bags.ReadSequence();
                if (bag.ReadObjectIdentifier() == CertBag)
                {
                    var value = bag.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0, true)).ReadSequence();
                    if (value.ReadObjectIdentifier() == X509Certificate)
                        certificate = value.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0, true)).ReadOctetString();
                }
            }
            catch (AsnContentException)
            {
                yield break;
            }

            if (certificate != null)
                yield return certificate;
        }
    }

    /// <summary>ContentInfo ::= SEQUENCE { contentType OID, content [0] EXPLICIT ANY }.</summary>
    private static byte[] ReadContentInfo(AsnReader parent, out string contentType)
    {
        var contentInfo = parent.ReadSequence();
        contentType = contentInfo.ReadObjectIdentifier();
        return contentInfo.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0, true)).ReadOctetString();
    }

    /// <summary>
    /// A cert bag carries the certificate inside an octet string, so the der of the certificate itself is one
    /// layer further in.
    /// </summary>
    private static byte[] Unwrap(ReadOnlyMemory<byte> encodedCertificate)
    {
        try
        {
            return new AsnReader(encodedCertificate, AsnEncodingRules.BER).ReadOctetString();
        }
        catch (AsnContentException)
        {
            return encodedCertificate.ToArray();
        }
    }

    /// <summary>Whether the bytes look like a PKCS#12 container rather than a certificate.</summary>
    public static bool IsPkcs12(byte[] data)
    {
        if (data is not { Length: > 4 } || data[0] != 0x30)
            return false;

        try
        {
            Pkcs12Info.Decode(data, out _, skipCopy: true);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
