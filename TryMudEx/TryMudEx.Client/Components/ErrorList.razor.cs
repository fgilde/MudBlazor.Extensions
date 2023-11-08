namespace TryMudEx.Client.Components
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Try.Core;
    using Microsoft.AspNetCore.Components;

    public partial class ErrorList
    {
        [Parameter]
        public IReadOnlyCollection<CompilationDiagnostic> Diagnostics { get; set; } = Array.Empty<CompilationDiagnostic>();

        [Parameter]
        public EventCallback<CompilationDiagnostic> OnDiagnosticClick { get; set; }

        [Parameter]
        public bool Show { get; set; }

        [Parameter]
        public EventCallback<bool> ShowChanged { get; set; }

        private Task ToggleDiagnosticsAsync()
        {
            this.Show = !this.Show;
            return this.ShowChanged.InvokeAsync(this.Show);
        }

        private Task Goto(CompilationDiagnostic diagnostic)
        {
            return OnDiagnosticClick.InvokeAsync(diagnostic);
        }
    }
}
