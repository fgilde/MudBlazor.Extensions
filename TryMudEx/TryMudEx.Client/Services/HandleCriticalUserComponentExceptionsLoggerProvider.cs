namespace TryMudEx.Client.Services
{
    using Microsoft.Extensions.Logging;
    using Microsoft.JSInterop;

    public class HandleCriticalUserComponentExceptionsLoggerProvider : ILoggerProvider
    {
        private readonly IJSInProcessRuntime _jsRuntime;

        public HandleCriticalUserComponentExceptionsLoggerProvider(IJSInProcessRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public ILogger CreateLogger(string categoryName) => new HandleCriticalUserComponentExceptionsLogger(_jsRuntime);

        public void Dispose()
        {
        }
    }
}
