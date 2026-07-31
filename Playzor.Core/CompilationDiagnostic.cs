using System;

namespace Playzor.Core
{
    using System.IO;
    using Microsoft.AspNetCore.Razor.Language;
    using Microsoft.CodeAnalysis;

    public class CompilationDiagnostic: IEquatable<CompilationDiagnostic>
    {
        public bool Equals(CompilationDiagnostic other)
        {
            return Code == other.Code && Severity == other.Severity && Description == other.Description && Line == other.Line && File == other.File && Kind == other.Kind;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CompilationDiagnostic)obj);
        }

        public override int GetHashCode() => HashCode.Combine(Code, (int)Severity, Description, Line, File, (int)Kind);

        public string Code { get; set; }

        public DiagnosticSeverity Severity { get; set; }

        public string Description { get; set; }

        public int? Line { get; set; }

        public string File { get; set; }

        public CompilationDiagnosticKind Kind { get; set; }

        /// <summary>Lines the compiler sees before the user's first line of __Main.razor.</summary>
        internal const int MainComponentInjectedLineCount = 3;

        internal static CompilationDiagnostic FromCSharpDiagnostic(Diagnostic diagnostic)
        {
            if (diagnostic == null)
            {
                return null;
            }

            var mappedLineSpan = diagnostic.Location.GetMappedLineSpan();
            var file = Path.GetFileName(mappedLineSpan.Path);
            var line = mappedLineSpan.StartLinePosition.Line + 1; // roslyn is 0-based

            if (file == CoreConstants.MainComponentFilePath)
            {
                // the main component is compiled with injected lines in front of the user code:
                // the two MudBlazor provider tags and the @page directive (the leading newline of
                // the provider block is eaten by TrimStart in CreateRazorProjectItem)
                line -= MainComponentInjectedLineCount;
            }

            return new CompilationDiagnostic
            {
                Kind = CompilationDiagnosticKind.CSharp,
                Code = diagnostic.Descriptor.Id,
                Severity = diagnostic.Severity,
                Description = diagnostic.GetMessage(),
                File = file,
                Line = line,
            };
        }

        internal static CompilationDiagnostic FromRazorDiagnostic(RazorDiagnostic diagnostic)
        {
            if (diagnostic == null)
            {
                return null;
            }

            return new CompilationDiagnostic
            {
                Kind = CompilationDiagnosticKind.Razor,
                Code = diagnostic.Id,
                Severity = (DiagnosticSeverity)diagnostic.Severity,
                Description = diagnostic.GetMessage(),
                File = Path.GetFileName(diagnostic.Span.FilePath),

                // Line = diagnostic.Span.LineIndex, // TODO: Find a way to calculate this
            };
        }
    }
}
