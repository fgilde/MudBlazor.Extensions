using Microsoft.AspNetCore.Components;

namespace MainSample.WebAssembly.Pages;

public partial class MudExDemoPage : IMudExDemoRegistrar
{
    internal static MudExDemoPage? Instance { get; private set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Instance = this;
    }

    private HashSet<Type> _types = new();
    private readonly List<MudExDemoSection> _sections = new();

    void IMudExDemoRegistrar.Register(MudExDemoSection section)
    {
        if (_sections.Any(s => s.Id == section.Id))
            return;

        _sections.Add(section);
        InvokeAsync(StateHasChanged);
    }

    void IMudExDemoRegistrar.RegisterComponentType(Type type)
    {
        _types.Add(type);
        InvokeAsync(StateHasChanged);
    }

    void IMudExDemoRegistrar.Unregister(string id)
    {
        var s = _sections.FirstOrDefault(x => x.Id == id);
        if (s is null) return;
        _sections.Remove(s);
        InvokeAsync(StateHasChanged);
    }
}



public interface IMudExDemoRegistrar
{
    void RegisterComponentType(Type type);
    void Register(MudExDemoSection section);
    void Unregister(string id);
}

public class MudExDemoSection
{
    public string Id { get; set; } = default!;
    public string Title { get; set; } = default!;
    public int Order { get; set; }
}