using Nextended.Core.Types;

namespace MudBlazor.Extensions.Core.Capture;

/// <summary>
/// Id for a capture.
/// </summary>
public class CaptureId : BaseId<CaptureId, string>
{
    /// <summary>
    /// Creates a new instance of <see cref="CaptureId"/>.
    /// </summary>
    public CaptureId(string id) : base(id)
    {}
}