using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Core;

public interface IReinitializable: IComponent
{
    Task ReinitializeAsync();
}