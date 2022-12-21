namespace MudBlazor.Extensions.Options;

public class MudExConfiguration
{
    internal MudExConfiguration()
    {}

    public MudExConfiguration WithDefaultDialogOptions(Action<DialogOptionsEx> options)
    {
        options?.Invoke(DialogOptionsEx.DefaultDialogOptions);
        return this;
    }

    public MudExConfiguration WithDefaultDialogOptions(DialogOptionsEx options)
    {
        options?.SetAsDefaultDialogOptions();
        return this;
    }
}