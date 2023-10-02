using System.ComponentModel;

namespace MudBlazor.Extensions.Options;

public enum MudExCodeLanguage
{
    [Description("")]
    Unknown,
    [Description("csharp")]
    Razor,
    [Description("csharp")]
    CSharp,
    [Description("javascript")]
    JavaScript,
    [Description("jsx")]
    JSX,
    [Description("typescript")]
    TypeScript,
    [Description("python")]
    Python,
    [Description("go")]
    Go,
    [Description("java")]
    Java,
    [Description("html")]
    Html,
    [Description("css")]
    Css,
    [Description("scss")]
    Scss,
    [Description("sass")]
    Sass,
    [Description("less")]
    Less,
    [Description("php")]
    Php,
    [Description("ruby")]
    Ruby,
    [Description("cpp")]
    Cpp,
    [Description("c")]
    C,
    [Description("rust")]
    Rust,
    [Description("lua")]
    Lua,
    [Description("r")]
    R,
    [Description("bash")]
    Bash,
    [Description("sql")]
    Sql,
    [Description("perl")]
    Perl,
    [Description("swift")]
    Swift,
    [Description("kotlin")]
    Kotlin,
    [Description("groovy")]
    Groovy,
    [Description("objective-c")]
    ObjectiveC,
    [Description("objective-c++")]
    ObjectiveCpp,
    [Description("fortran")]
    Fortran,
    [Description("prolog")]
    Prolog,
    [Description("ocaml")]
    Ocaml,
    [Description("scala")]
    Scala,
    [Description("haskell")]
    Haskell,
    [Description("clojure")]
    Clojure,
    [Description("clojurescript")]
    ClojureScript,
    [Description("elm")]
    Elm,
    [Description("erlang")]
    Erlang,
    [Description("vbnet")]
    VbNet,
    [Description("vbscript")]
    VbScript,
    [Description("json")]
    Json,
    [Description("yaml")]
    Yaml,
    [Description("markdown")]
    Markdown,
    [Description("toml")]
    Toml,
    [Description("ini")]
    Ini,
    [Description("xml")]
    Xml
}

public class MudExCodeLanguageExtensionsMapping
{
    private static Dictionary<string, MudExCodeLanguage> fileExtensionToMudExCodeLanguage = new()
    {
        { "razor", MudExCodeLanguage.Razor },
        { "cs", MudExCodeLanguage.CSharp },
        { "js", MudExCodeLanguage.JavaScript },
        { "jsx", MudExCodeLanguage.JSX },
        { "ts", MudExCodeLanguage.TypeScript },
        { "py", MudExCodeLanguage.Python },
        { "go", MudExCodeLanguage.Go },
        { "java", MudExCodeLanguage.Java },
        { "html", MudExCodeLanguage.Html },
        { "css", MudExCodeLanguage.Css },
        { "scss", MudExCodeLanguage.Scss },
        { "sass", MudExCodeLanguage.Sass },
        { "less", MudExCodeLanguage.Less },
        { "php", MudExCodeLanguage.Php },
        { "rb", MudExCodeLanguage.Ruby },
        { "cpp", MudExCodeLanguage.Cpp },
        { "c", MudExCodeLanguage.C },
        { "rs", MudExCodeLanguage.Rust },
        { "lua", MudExCodeLanguage.Lua },
        { "r", MudExCodeLanguage.R },
        { "sh", MudExCodeLanguage.Bash },
        { "bash", MudExCodeLanguage.Bash },
        { "sql", MudExCodeLanguage.Sql },
        { "pl", MudExCodeLanguage.Perl },
        { "swift", MudExCodeLanguage.Swift },
        { "kt", MudExCodeLanguage.Kotlin },
        { "groovy", MudExCodeLanguage.Groovy },
        { "m", MudExCodeLanguage.ObjectiveC },
        { "mm", MudExCodeLanguage.ObjectiveCpp },
        { "f", MudExCodeLanguage.Fortran },
        { "f90", MudExCodeLanguage.Fortran },
        { "prolog", MudExCodeLanguage.Prolog },
        { "ml", MudExCodeLanguage.Ocaml },
        { "scala", MudExCodeLanguage.Scala },
        { "hs", MudExCodeLanguage.Haskell },
        { "clj", MudExCodeLanguage.Clojure },
        { "cljs", MudExCodeLanguage.ClojureScript },
        { "elm", MudExCodeLanguage.Elm },
        { "erl", MudExCodeLanguage.Erlang },
        { "hrl", MudExCodeLanguage.Erlang },
        { "vb", MudExCodeLanguage.VbNet },
        { "vbs", MudExCodeLanguage.VbScript },
        { "json", MudExCodeLanguage.Json },
        { "yaml", MudExCodeLanguage.Yaml },
        { "yml", MudExCodeLanguage.Yaml },
        { "md", MudExCodeLanguage.Markdown },
        { "toml", MudExCodeLanguage.Toml },
        { "ini", MudExCodeLanguage.Ini },
        { "xml", MudExCodeLanguage.Xml },
        { "cfg", MudExCodeLanguage.Ini }
    };

    public static MudExCodeLanguage GetCodeLanguageForFile(string input)
    {
        if(string.IsNullOrEmpty(input))
            return MudExCodeLanguage.Unknown;
        var extension = input.Contains(".") ? Path.GetExtension(input).TrimStart('.') : input;
        
        return fileExtensionToMudExCodeLanguage.TryGetValue(extension, out var codeLanguage) ?
            codeLanguage :
            MudExCodeLanguage.Unknown;
    }

}