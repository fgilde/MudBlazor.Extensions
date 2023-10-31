using Microsoft.AspNetCore.Components;
using Nextended.Core.Contracts;

namespace MudBlazor.Extensions.Core;

/// <summary>
/// External file picker
/// </summary>
public interface IMudExExternalFilePicker: IComponent
{
    /// <summary>
    /// Image
    /// </summary>
    public string Image { get; }

    /// <summary>
    /// Forces the picker to open and return files
    /// </summary>
    public Task<IUploadableFile[]> PickAsync(CancellationToken cancellation = default);
}