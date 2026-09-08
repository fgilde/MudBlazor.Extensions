using System.Formats.Asn1;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace MudBlazor.Extensions.Helper.Internal;

/// <summary>
/// Reads X.509 certificates with <see cref="AsnReader"/> instead of <c>X509Certificate2</c>.
/// </summary>
/// <remarks>
/// The whole <c>System.Security.Cryptography.X509Certificates</c> namespace throws
/// <see cref="PlatformNotSupportedException"/> on browser-wasm, so a Blazor WebAssembly app cannot open a
/// certificate through it at all. <c>System.Formats.Asn1</c> is pure managed code and works everywhere, so the
/// viewer decodes the DER structure itself. Only PKCS#12 still needs the platform, because that container is
/// encrypted rather than merely encoded.
/// </remarks>
internal static class X509CertificateReader
{
    private const string Pkcs7SignedData = "1.2.840.113549.1.7.2";
    private const string SubjectAlternativeNameOid = "2.5.29.17";

    private static readonly Dictionary<string, string> RdnNames = new()
    {
        ["2.5.4.3"] = "CN",
        ["2.5.4.4"] = "SN",
        ["2.5.4.5"] = "SERIALNUMBER",
        ["2.5.4.6"] = "C",
        ["2.5.4.7"] = "L",
        ["2.5.4.8"] = "S",
        ["2.5.4.9"] = "STREET",
        ["2.5.4.10"] = "O",
        ["2.5.4.11"] = "OU",
        ["2.5.4.12"] = "T",
        ["2.5.4.42"] = "G",
        ["0.9.2342.19200300.100.1.1"] = "UID",
        ["0.9.2342.19200300.100.1.25"] = "DC",
        ["1.2.840.113549.1.9.1"] = "E"
    };

    private static readonly Dictionary<string, string> AlgorithmNames = new()
    {
        ["1.2.840.113549.1.1.1"] = "RSA",
        ["1.2.840.113549.1.1.5"] = "sha1RSA",
        ["1.2.840.113549.1.1.10"] = "RSASSA-PSS",
        ["1.2.840.113549.1.1.11"] = "sha256RSA",
        ["1.2.840.113549.1.1.12"] = "sha384RSA",
        ["1.2.840.113549.1.1.13"] = "sha512RSA",
        ["1.2.840.10045.2.1"] = "ECC",
        ["1.2.840.10045.4.3.2"] = "sha256ECDSA",
        ["1.2.840.10045.4.3.3"] = "sha384ECDSA",
        ["1.2.840.10045.4.3.4"] = "sha512ECDSA",
        ["1.3.101.112"] = "Ed25519",
        ["1.2.840.10045.3.1.7"] = "nistP256",
        ["1.3.132.0.34"] = "nistP384",
        ["1.3.132.0.35"] = "nistP521"
    };

    /// <summary>
    /// Reads every certificate found in the data: one or more PEM blocks, a bare DER certificate or a PKCS#7
    /// certificate chain (.p7b), in DER or PEM form.
    /// </summary>
    public static List<CertificateDetails> Read(byte[] data)
    {
        var certificates = new List<CertificateDetails>();

        foreach (var der in ExtractDerBlocks(data))
        {
            // A PKCS#7 container holds the certificates instead of being one
            if (TryReadPkcs7(der, out var contained))
                certificates.AddRange(contained);
            else
                certificates.Add(ReadCertificate(der));
        }

        if (certificates.Count == 0)
            throw new FormatException("No X.509 certificate could be found in the file.");

        return certificates;
    }

    /// <summary>
    /// Yields the DER payloads: the base64 body of every PEM block, or the input itself when it is not text.
    /// </summary>
    private static IEnumerable<byte[]> ExtractDerBlocks(byte[] data)
    {
        var text = TryGetText(data);
        if (text == null || !text.Contains("-----BEGIN", StringComparison.Ordinal))
        {
            yield return data;
            yield break;
        }

        var searchFrom = 0;
        var found = false;
        while (true)
        {
            var begin = text.IndexOf("-----BEGIN", searchFrom, StringComparison.Ordinal);
            if (begin < 0)
                break;

            var headerEnd = text.IndexOf("-----", begin + 10, StringComparison.Ordinal);
            if (headerEnd < 0)
                break;

            var label = text[(begin + 10)..headerEnd].Trim();
            var bodyStart = headerEnd + 5;
            var end = text.IndexOf("-----END", bodyStart, StringComparison.Ordinal);
            if (end < 0)
                break;

            searchFrom = end + 8;

            // Private keys and parameters share the PEM envelope with certificates - only take the certificates
            if (!label.Contains("CERTIFICATE", StringComparison.OrdinalIgnoreCase)
                && !label.Contains("PKCS7", StringComparison.OrdinalIgnoreCase))
                continue;
            if (label.Contains("REQUEST", StringComparison.OrdinalIgnoreCase))
                continue;

            byte[] der;
            try
            {
                der = Convert.FromBase64String(text[bodyStart..end]);
            }
            catch (FormatException)
            {
                continue;
            }

            found = true;
            yield return der;
        }

        if (!found)
            yield return data;
    }

    private static string TryGetText(byte[] data)
    {
        // A PEM file is ASCII; a DER file almost always contains bytes that are not
        foreach (var b in data)
        {
            if (b > 126 || (b < 32 && b != (byte)'\r' && b != (byte)'\n' && b != (byte)'\t'))
                return null;
        }
        return Encoding.ASCII.GetString(data);
    }

    private static bool TryReadPkcs7(byte[] der, out List<CertificateDetails> certificates)
    {
        certificates = null;
        try
        {
            var contentInfo = new AsnReader(der, AsnEncodingRules.BER).ReadSequence();
            if (contentInfo.ReadObjectIdentifier() != Pkcs7SignedData)
                return false;

            var content = contentInfo.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            var signedData = content.ReadSequence();

            signedData.ReadInteger();                                    // version
            signedData.ReadSetOf();                                      // digestAlgorithms
            signedData.ReadSequence();                                   // encapContentInfo

            var certificatesTag = new Asn1Tag(TagClass.ContextSpecific, 0);
            if (!signedData.HasData || !signedData.PeekTag().HasSameClassAndValue(certificatesTag))
                return false;

            var bag = signedData.ReadSetOf(certificatesTag);
            certificates = new List<CertificateDetails>();
            while (bag.HasData)
                certificates.Add(ReadCertificate(bag.ReadEncodedValue().ToArray()));

            return certificates.Count > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static CertificateDetails ReadCertificate(byte[] der)
    {
        var certificate = new AsnReader(der, AsnEncodingRules.DER).ReadSequence();
        var tbs = certificate.ReadSequence();

        var version = 1;
        var versionTag = new Asn1Tag(TagClass.ContextSpecific, 0);
        if (tbs.PeekTag().HasSameClassAndValue(versionTag))
            version = (int)tbs.ReadSequence(versionTag).ReadInteger() + 1;

        var serial = tbs.ReadIntegerBytes().ToArray();
        var signatureAlgorithm = ReadAlgorithm(tbs);
        var issuer = ReadName(tbs);

        var validity = tbs.ReadSequence();
        var notBefore = ReadTime(validity);
        var notAfter = ReadTime(validity);

        var subject = ReadName(tbs);

        var subjectPublicKeyInfo = tbs.ReadSequence();
        var keyAlgorithm = ReadAlgorithm(subjectPublicKeyInfo);
        var publicKey = subjectPublicKeyInfo.ReadBitString(out _);

        return new CertificateDetails
        {
            RawData = der,
            Version = version,
            SerialNumber = FormatSerial(serial),
            SignatureAlgorithm = FriendlyName(signatureAlgorithm.Oid),
            PublicKeyAlgorithm = FriendlyName(keyAlgorithm.Oid),
            PublicKeyDescription = DescribeKey(keyAlgorithm, publicKey),
            Issuer = issuer,
            Subject = subject,
            NotBefore = notBefore,
            NotAfter = notAfter,
            Thumbprint = Hash(der, "SHA1"),
            Sha256Fingerprint = Hash(der, "SHA256"),
            SubjectAlternativeNames = ReadSubjectAlternativeNames(tbs)
        };
    }

    private static (string Oid, ReadOnlyMemory<byte>? Parameters) ReadAlgorithm(AsnReader parent)
    {
        var algorithm = parent.ReadSequence();
        var oid = algorithm.ReadObjectIdentifier();
        ReadOnlyMemory<byte>? parameters = algorithm.HasData ? algorithm.ReadEncodedValue() : null;
        return (oid, parameters);
    }

    /// <summary>
    /// Formats an RDNSequence the way certificate tools display it: most specific component first.
    /// </summary>
    private static string ReadName(AsnReader parent)
    {
        var name = parent.ReadSequence();
        var parts = new List<string>();

        while (name.HasData)
        {
            var rdn = name.ReadSetOf();
            while (rdn.HasData)
            {
                var attribute = rdn.ReadSequence();
                var oid = attribute.ReadObjectIdentifier();
                var value = ReadAnyString(attribute);
                parts.Add($"{(RdnNames.TryGetValue(oid, out var label) ? label : "OID." + oid)}={value}");
            }
        }

        parts.Reverse();
        return string.Join(", ", parts);
    }

    private static string ReadAnyString(AsnReader reader)
    {
        if (!reader.HasData)
            return string.Empty;

        var tag = reader.PeekTag();
        try
        {
            return tag.TagValue switch
            {
                (int)UniversalTagNumber.UTF8String => reader.ReadCharacterString(UniversalTagNumber.UTF8String),
                (int)UniversalTagNumber.PrintableString => reader.ReadCharacterString(UniversalTagNumber.PrintableString),
                (int)UniversalTagNumber.IA5String => reader.ReadCharacterString(UniversalTagNumber.IA5String),
                (int)UniversalTagNumber.BMPString => reader.ReadCharacterString(UniversalTagNumber.BMPString),
                (int)UniversalTagNumber.NumericString => reader.ReadCharacterString(UniversalTagNumber.NumericString),
                (int)UniversalTagNumber.VisibleString => reader.ReadCharacterString(UniversalTagNumber.VisibleString),
                // T61String and friends have no managed decoder; latin-1 is the pragmatic reading
                _ => Encoding.Latin1.GetString(reader.ReadEncodedValue().ToArray())
            };
        }
        catch (AsnContentException)
        {
            return string.Empty;
        }
    }

    private static DateTimeOffset ReadTime(AsnReader validity)
    {
        var tag = validity.PeekTag();
        return tag.TagValue == (int)UniversalTagNumber.UtcTime
            ? validity.ReadUtcTime()
            : validity.ReadGeneralizedTime();
    }

    /// <summary>
    /// Walks the optional trailing fields of the TBSCertificate up to the "[3] extensions" block and reads the
    /// subject alternative names out of it.
    /// </summary>
    private static List<string> ReadSubjectAlternativeNames(AsnReader tbs)
    {
        var result = new List<string>();
        var extensionsTag = new Asn1Tag(TagClass.ContextSpecific, 3);

        try
        {
            while (tbs.HasData)
            {
                if (!tbs.PeekTag().HasSameClassAndValue(extensionsTag))
                {
                    tbs.ReadEncodedValue(); // issuerUniqueID / subjectUniqueID
                    continue;
                }

                var extensions = tbs.ReadSequence(extensionsTag).ReadSequence();
                while (extensions.HasData)
                {
                    var extension = extensions.ReadSequence();
                    var oid = extension.ReadObjectIdentifier();
                    if (extension.PeekTag().TagValue == (int)UniversalTagNumber.Boolean)
                        extension.ReadBoolean();

                    var value = extension.ReadOctetString();
                    if (oid == SubjectAlternativeNameOid)
                        result.AddRange(ReadGeneralNames(value));
                }
                break;
            }
        }
        catch (Exception)
        {
            // A certificate with unreadable extensions is still worth showing without them
        }

        return result;
    }

    private static IEnumerable<string> ReadGeneralNames(byte[] value)
    {
        var names = new List<string>();
        var reader = new AsnReader(value, AsnEncodingRules.DER).ReadSequence();

        while (reader.HasData)
        {
            var tag = reader.PeekTag();
            switch (tag.TagValue)
            {
                case 1:
                    names.Add("Email=" + reader.ReadCharacterString(UniversalTagNumber.IA5String, tag));
                    break;
                case 2:
                    names.Add("DNS-Name=" + reader.ReadCharacterString(UniversalTagNumber.IA5String, tag));
                    break;
                case 6:
                    names.Add("URL=" + reader.ReadCharacterString(UniversalTagNumber.IA5String, tag));
                    break;
                case 7:
                    names.Add("IP-Address=" + new IPAddress(reader.ReadOctetString(tag)));
                    break;
                default:
                    reader.ReadEncodedValue();
                    break;
            }
        }

        return names;
    }

    private static string DescribeKey((string Oid, ReadOnlyMemory<byte>? Parameters) algorithm, byte[] publicKey)
    {
        try
        {
            // RSAPublicKey ::= SEQUENCE { modulus INTEGER, publicExponent INTEGER }
            if (algorithm.Oid == "1.2.840.113549.1.1.1")
            {
                var modulus = new AsnReader(publicKey, AsnEncodingRules.DER).ReadSequence().ReadIntegerBytes().Span;
                var length = modulus.Length > 1 && modulus[0] == 0 ? modulus.Length - 1 : modulus.Length;
                return $"RSA {length * 8} bit";
            }

            // For EC the named curve sits in the algorithm parameters
            if (algorithm.Oid == "1.2.840.10045.2.1" && algorithm.Parameters != null)
            {
                var curve = new AsnReader(algorithm.Parameters.Value, AsnEncodingRules.DER).ReadObjectIdentifier();
                return $"ECC {FriendlyName(curve)}";
            }
        }
        catch (Exception)
        {
            // fall through to the plain algorithm name
        }

        return FriendlyName(algorithm.Oid);
    }

    private static string Hash(byte[] data, string algorithm)
    {
        try
        {
            // Managed SHA implementations exist on browser-wasm, but guard anyway: a missing fingerprint is
            // a blank row, not a reason to fail the whole certificate.
            var hash = algorithm == "SHA1" ? SHA1.HashData(data) : SHA256.HashData(data);
            return Convert.ToHexString(hash);
        }
        catch (Exception)
        {
            return null;
        }
    }

    // The leading zero that DER adds to keep the serial positive stays in: X509Certificate2.SerialNumber and
    // the certificate dialogs of Windows and OpenSSL all show it, so stripping it would print a different
    // serial than every other tool.
    private static string FormatSerial(byte[] serial) => Convert.ToHexString(serial);

    private static string FriendlyName(string oid)
        => AlgorithmNames.TryGetValue(oid, out var name) ? name : oid;

    /// <summary>
    /// The decoded, display ready contents of one certificate.
    /// </summary>
    internal sealed class CertificateDetails
    {
        /// <summary>The DER bytes the values were decoded from.</summary>
        public byte[] RawData { get; init; }
        /// <summary>X.509 version (1, 2 or 3).</summary>
        public int Version { get; init; }
        /// <summary>Serial number as upper case hex.</summary>
        public string SerialNumber { get; init; }
        /// <summary>Signature algorithm, friendly name where known.</summary>
        public string SignatureAlgorithm { get; init; }
        /// <summary>Public key algorithm, friendly name where known.</summary>
        public string PublicKeyAlgorithm { get; init; }
        /// <summary>Public key algorithm including key size or curve.</summary>
        public string PublicKeyDescription { get; init; }
        /// <summary>Issuer distinguished name.</summary>
        public string Issuer { get; init; }
        /// <summary>Subject distinguished name.</summary>
        public string Subject { get; init; }
        /// <summary>Start of the validity period.</summary>
        public DateTimeOffset NotBefore { get; init; }
        /// <summary>End of the validity period.</summary>
        public DateTimeOffset NotAfter { get; init; }
        /// <summary>SHA-1 fingerprint, the value certificate tools call "thumbprint".</summary>
        public string Thumbprint { get; init; }
        /// <summary>SHA-256 fingerprint.</summary>
        public string Sha256Fingerprint { get; init; }
        /// <summary>Subject alternative names, prefixed with their kind.</summary>
        public List<string> SubjectAlternativeNames { get; init; } = new();

        /// <summary>Common name of the subject, used as a short display label.</summary>
        public string DisplayName
        {
            get
            {
                var commonName = Subject?.Split(',')
                    .Select(part => part.Trim())
                    .FirstOrDefault(part => part.StartsWith("CN=", StringComparison.OrdinalIgnoreCase));
                return commonName?[3..] is { Length: > 0 } name ? name : Subject is { Length: > 0 } ? Subject : "Certificate";
            }
        }

        /// <summary>Whether the certificate is expired or not yet valid.</summary>
        public bool IsExpired => DateTimeOffset.Now < NotBefore || DateTimeOffset.Now > NotAfter;
    }
}
