using System;

namespace Try.Core
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

        internal static CompilationDiagnostic FromCSharpDiagnostic(Diagnostic diagnostic)
        {
            if (diagnostic == null)
            {
                return null;
            }

            var mappedLineSpan = diagnostic.Location.GetMappedLineSpan();
            var file = Path.GetFileName(mappedLineSpan.Path);
            var line = mappedLineSpan.StartLinePosition.Line;

            if (file != CoreConstants.MainComponentFilePath)
            {
                // Make it 1-based. Skip the main component where we add @page directive line
                line++;
            }
            else
            {
                // Offset for MudProviders
                line -= 4;
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
