namespace Playzor.Blazor.Editor.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Playzor.Core;
    using Microsoft.AspNetCore.Components;
    using Microsoft.CodeAnalysis;
    using Microsoft.JSInterop;
    using Playzor.Blazor.Editor.Services;

    public partial class ErrorList
    {
        [Inject] private IJSRuntime JsRuntime { get; set; }

        [Inject] private PlayzorLocalizer L { get; set; }

        [Parameter]
        public IReadOnlyCollection<CompilationDiagnostic> Diagnostics { get; set; } = Array.Empty<CompilationDiagnostic>();

        [Parameter]
        public EventCallback<CompilationDiagnostic> OnDiagnosticClick { get; set; }

        private HashSet<string> _levels = new() { "error", "warning" };

        private int ErrorCount => Diagnostics?.Count(d => d.Severity == DiagnosticSeverity.Error) ?? 0;

        private int WarningCount => Diagnostics?.Count(d => d.Severity == DiagnosticSeverity.Warning) ?? 0;

        private IEnumerable<CompilationDiagnostic> Filtered => (Diagnostics ?? Array.Empty<CompilationDiagnostic>())
            .Where(d => _levels.Contains(d.Severity == DiagnosticSeverity.Error ? "error" : "warning"));

        private static string Location(CompilationDiagnostic diagnostic)
        {
            if (string.IsNullOrEmpty(diagnostic.File)) return string.Empty;
            return diagnostic.Line is > 0 ? $"{diagnostic.File}:{diagnostic.Line}" : diagnostic.File;
        }

        private Task Goto(CompilationDiagnostic diagnostic)
        {
            return OnDiagnosticClick.InvokeAsync(diagnostic);
        }

        private async Task CopyAsync()
        {
            var text = string.Join(Environment.NewLine, Filtered.Select(d =>
                $"{d.Severity} {d.Code}: {d.Description} ({Location(d)})"));
            await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
        }
    }
}
