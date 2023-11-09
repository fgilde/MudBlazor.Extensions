using System.Collections.Generic;

namespace Try.Core;

public static class CodeFileTemplates
{
    public static List<CodeFile> All()
    {
        return new List<CodeFile>
        {
            new() { Path = "_Imports.razor" },
            new() { Path = "Startup.cs", Content = CoreConstants.StartupContent }
        };
    }
}