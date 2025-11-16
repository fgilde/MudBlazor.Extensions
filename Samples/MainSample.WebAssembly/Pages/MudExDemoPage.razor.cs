namespace MainSample.WebAssembly.Pages;

public partial class MudExDemoPage : IMudExDemoRegistrar
{
    
}


public interface IMudExDemoRegistrar
{
    void Register(MudExDemoSection section);
    void Unregister(string id);
}

public class MudExDemoSection
{
    public string Id { get; set; } = default!;
    public string Title { get; set; } = default!;
    public int Order { get; set; }          // Reihenfolge in der Nav
}