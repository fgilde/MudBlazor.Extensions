using Microsoft.JSInterop;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Core;

public interface IMudExDialogService : IDialogService
{
    public IJSRuntime JSRuntime { get; }
    public IServiceProvider ServiceProvider { get; }
    public MudExAppearanceService AppearanceService { get; }

}