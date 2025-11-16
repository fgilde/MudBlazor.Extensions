using MainSample.WebAssembly.Examples;

namespace MainSample.WebAssembly.Shared;

public interface IExampleHolder
{
    public Task RegisterAsync(IExample example);
}