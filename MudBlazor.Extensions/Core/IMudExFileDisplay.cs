namespace MudBlazor.Extensions.Core;

public interface IMudExFileDisplay
{
    public bool WrapInMudExFileDisplayDiv => true;
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }
    bool CanHandleFile(IMudExFileDisplayInfos fileDisplayInfos);
}