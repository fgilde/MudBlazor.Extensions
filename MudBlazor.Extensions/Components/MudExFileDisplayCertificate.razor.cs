using MudBlazor.Extensions.Helper;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Viewer for X.509 certificate files (.pem, .crt, .cer, .der, .pfx, .p12, .p7b, .p7c). Shows subject, issuer,
/// validity, fingerprints, key details and subject alternative names of every certificate in the file - a PEM
/// bundle or a PKCS#7 container may hold a whole chain.
/// </summary>
/// <remarks>
/// Decoding is done by <see cref="X509CertificateReader"/> rather than <c>X509Certificate2</c>, because the
/// platform certificate stack is unavailable in Blazor WebAssembly. Only password protected PKCS#12 containers
/// still need it, since those are encrypted rather than merely encoded.
/// </remarks>
public partial class MudExFileDisplayCertificate : IMudExFileDisplay
{
    private object _loadedSource;
    private static readonly string[] Pkcs12Extensions = { ".pfx", ".p12" };

    [Inject] private MudExFileService FileService { get; set; }

    /// <inheritdoc />
    public string Name => nameof(MudExFileDisplayCertificate);

    /// <inheritdoc />
    [Parameter]
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Reference to the parent MudExFileDisplay if the component is used inside a MudExFileDisplay
    /// </summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    private List<X509CertificateReader.CertificateDetails> _certificates;
    private X509CertificateReader.CertificateDetails _selected;
    private string _errorMessage;
    private string _password = string.Empty;
    private bool _requiresPassword;
    private byte[] _pendingBytes;

    /// <inheritdoc />
    public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
    {
        var fileName = fileDisplayInfos?.FileName ?? string.Empty;
        var isMatch = new[] { ".pem", ".crt", ".cer", ".der", ".pfx", ".p12", ".p7b", ".p7c" }
            .Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(isMatch);
    }

    /// <inheritdoc />
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>
        {
            { "Certificates", _certificates?.Count ?? 0 }
        });
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        // The same viewer instance serves every file of its kind, and FileDisplayInfos is always
        // the same object - so only the values tell one file from the next.
        parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var infos);
        var sourceChanged = infos.SourceChanged(ref _loadedSource);

        await base.SetParametersAsync(parameters);

        if (sourceChanged)
        {
            _certificates = null;
            _errorMessage = null;
            _requiresPassword = false;
            await LoadCertificatesAsync();
            // A viewer knows its own values only now, and its renders do not reach the host.
            await FileDisplayInfos.NotifyMetaChangedAsync();
        }
    }

    private async Task LoadCertificatesAsync()
    {
        try
        {
            var stream = await FileService.ReadStreamAsync(FileDisplayInfos);
            if (stream == null)
                return;

            _pendingBytes = await ReadAllBytesAsync(stream);

            if (IsPkcs12(FileDisplayInfos?.FileName))
            {
                // A PKCS#12 container is encrypted, so it needs the platform stack and usually a password
                _requiresPassword = true;
            }
            else
            {
                _certificates = X509CertificateReader.Read(_pendingBytes);
                _selected = _certificates.FirstOrDefault();
            }
        }
        catch (Exception e)
        {
            SetError(e);
        }
        StateHasChanged();
    }

    private async Task TryWithPasswordAsync()
    {
        _errorMessage = null;
        try
        {
            _certificates = await ReadPkcs12Async(_pendingBytes, _password);
            _selected = _certificates.FirstOrDefault();
            _requiresPassword = false;
        }
        catch (Exception e)
        {
            SetError(e);
        }
        StateHasChanged();
    }

    /// <summary>
    /// PKCS#12 is the one format that needs the password before anything can be read. The certificates that
    /// come out of the container are re-read from their DER, so the display stays on one code path.
    /// </summary>
    private async Task<List<X509CertificateReader.CertificateDetails>> ReadPkcs12Async(byte[] bytes, string password)
        => (await Pkcs12CertificateReader.ReadCertificatesAsync(bytes, password, JsRuntime))
            .Select(der => X509CertificateReader.Read(der).First())
            .ToList();

    /// <summary>The oid the runtime named in its refusal, turned into something readable.</summary>
    private static string AlgorithmOf(Exception exception)
    {
        var oid = System.Text.RegularExpressions.Regex.Match(exception.Message ?? string.Empty, @"[\d]+(\.[\d]+){3,}").Value;
        return string.IsNullOrEmpty(oid) ? exception.Message : Pkcs12CertificateReader.SchemeName(oid);
    }

    /// <summary>An algorithm nobody here implements, rather than a wrong password.</summary>
    private static bool IsUnsupportedAlgorithm(Exception exception)
        => exception is PlatformNotSupportedException
           || exception.Message.Contains("algorithm", StringComparison.OrdinalIgnoreCase)
           || exception.Message.Contains("AlgorithmIdentifier", StringComparison.OrdinalIgnoreCase);

    private static bool IsPkcs12(string fileName) =>
        fileName != null && Pkcs12Extensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

    private void SetError(Exception exception)
    {
        _errorMessage = exception switch
        {
            Pkcs12CertificateReader.UnsupportedCipherException cipher
                => TryLocalize("This container is encrypted with {0}, which is not available in the browser. Export it again with AES - openssl pkcs12 -export -keypbe AES-256-CBC -certpbe AES-256-CBC -macalg sha256.", cipher.Scheme),
            PlatformNotSupportedException or CryptographicException when IsUnsupportedAlgorithm(exception)
                => TryLocalize("This container is encrypted with an algorithm that is not available here: {0}. A pfx in the legacy format uses RC2 or 3DES, which no current .NET carries - export it again with AES.", AlgorithmOf(exception)),
            CryptographicException => TryLocalize("The container could not be decrypted. Check the password."),
            _ => $"{exception.GetType().Name}: {exception.Message}"
        };
        MudExFileDisplay?.ShowError(_errorMessage);
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }
}
