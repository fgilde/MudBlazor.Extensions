using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Core;

public class JsRecordingCallbackWrapper<T>
{
    private readonly Action<T> _callback;
    private readonly Action<string> _stoppedCallback;

    public JsRecordingCallbackWrapper(Action<T> callback, Action<string> stoppedCallback = null)
    {
        _callback = callback;
        _stoppedCallback = stoppedCallback;
    }

    [JSInvokable]
    public void OnStopped(string id)
    {
        _stoppedCallback?.Invoke(id);
    }

    [JSInvokable]
    public void Invoke(T result)
    {
        _callback.Invoke(result);
    }
}