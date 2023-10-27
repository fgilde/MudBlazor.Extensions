namespace MudBlazor.Extensions.Core;

public interface IMudExExternalFilePicker
{
    public string Image { get; }

    public Task<string> PickAsync(CancellationToken cancellation = default);
}