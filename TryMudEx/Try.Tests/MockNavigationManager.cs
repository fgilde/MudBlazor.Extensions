using Microsoft.AspNetCore.Components;

namespace Tests
{
    public class MockNavigationManager
        : NavigationManager
    {
        public MockNavigationManager() : base() =>
            this.Initialize("https://localhost:5001/", "https://localhost:5001/snippet/");

        protected override void NavigateToCore(string uri, bool forceLoad) =>
            this.WasNavigateInvoked = true;

        public bool WasNavigateInvoked { get; private set; }
    }
}
