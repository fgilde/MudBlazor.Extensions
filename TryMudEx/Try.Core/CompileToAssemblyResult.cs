namespace Try.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;

    public class CompileToAssemblyResult
    {
        public Compilation Compilation { get; set; }

        public IEnumerable<CompilationDiagnostic> Diagnostics { get; set; } = Enumerable.Empty<CompilationDiagnostic>();

        public byte[] AssemblyBytes { get; set; }
    }
}
