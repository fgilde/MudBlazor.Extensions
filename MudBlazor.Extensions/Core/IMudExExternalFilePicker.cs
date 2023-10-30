using Microsoft.AspNetCore.Components;
using Nextended.Core.Contracts;

namespace MudBlazor.Extensions.Core;

public interface IMudExExternalFilePicker: IComponent
{
    public string Image { get; }

    public Task<IUploadableFile[]> PickAsync(CancellationToken cancellation = default);
}