using System.ComponentModel;

namespace MudBlazor.Extensions.Options;

/// <summary>
/// Language of the code
/// </summary>
public enum MudExCodeLanguage
{
    /// <summary>
    /// Unknown language to format
    /// </summary>
    [Description("")]
    Unknown,
    
    /// <summary>
    /// C# Razor language
    /// </summary>
    [Description("csharp")]
    Razor,
    
    /// <summary>
    /// C# language
    /// </summary>
    [Description("csharp")]
    CSharp,
    
    /// <summary>
    /// JavaScript language
    /// </summary>
    [Description("javascript")]
    JavaScript,
    
    /// <summary>
    /// JSX language
    /// </summary>
    [Description("jsx")]
    JSX,
    
    /// <summary>
    /// TypeScript language
    /// </summary>
    [Description("typescript")]
    TypeScript,
    
    /// <summary>
    /// Python language
    /// </summary>
    [Description("python")]
    Python,
    
    /// <summary>
    /// Go language
    /// </summary>
    [Description("go")]
    Go,
    
    /// <summary>
    /// Java language
    /// </summary>
    [Description("java")]
    Java,
    
    /// <summary>
    /// Html language
    /// </summary>
    [Description("html")]
    Html,
    
    /// <summary>
    /// Css language
    /// </summary>
    [Description("css")]
    Css,
    
    /// <summary>
    /// Scss language
    /// </summary>
    [Description("scss")]
    Scss,
    
    /// <summary>
    /// Sass language
    /// </summary>
    [Description("sass")]
    Sass,
    
    /// <summary>
    /// Less language
    /// </summary>
    [Description("less")]
    Less,
    
    /// <summary>
    /// Php language
    /// </summary>
    [Description("php")]
    Php,
    
    /// <summary>
    /// Ruby language
    /// </summary>
    [Description("ruby")]
    Ruby,
    
    /// <summary>
    /// C++ language
    /// </summary>
    [Description("cpp")]
    Cpp,
    
    /// <summary>
    /// C language
    /// </summary>
    [Description("c")]
    C,
    
    /// <summary>
    /// Rust language
    /// </summary>
    [Description("rust")]
    Rust,
    
    /// <summary>
    /// Lua language
    /// </summary>
    [Description("lua")]
    Lua,
    
    /// <summary>
    /// R language
    /// </summary>
    [Description("r")]
    R,
    
    /// <summary>
    /// Bash language
    /// </summary>
    [Description("bash")]
    Bash,
    
    /// <summary>
    /// SQL language
    /// </summary>
    [Description("sql")]
    Sql,
    
    /// <summary>
    /// Perl language
    /// </summary>
    [Description("perl")]
    Perl,
    
    /// <summary>
    /// Swift language
    /// </summary>
    [Description("swift")]
    Swift,
    
    /// <summary>
    /// Kotlin Java language
    /// </summary>
    [Description("kotlin")]
    Kotlin,
    
    /// <summary>
    /// Groovy language
    /// </summary>
    [Description("groovy")]
    Groovy,
    
    /// <summary>
    /// Objective-C language
    /// </summary>
    [Description("objective-c")]
    ObjectiveC,
    
    /// <summary>
    /// Objective-C++ language
    /// </summary>
    [Description("objective-c++")]
    ObjectiveCpp,
    
    /// <summary>
    /// Fortran language
    /// </summary>
    [Description("fortran")]
    Fortran,
    
    /// <summary>
    /// Prolog language
    /// </summary>
    [Description("prolog")]
    Prolog,
    
    /// <summary>
    /// Ocaml language
    /// </summary>
    [Description("ocaml")]
    Ocaml,
    
    /// <summary>
    /// Scala language
    /// </summary>
    [Description("scala")]
    Scala,
    
    /// <summary>
    /// Haskell language
    /// </summary>
    [Description("haskell")]
    Haskell,
    
    /// <summary>
    /// Clojure language
    /// </summary>
    [Description("clojure")]
    Clojure,
    
    /// <summary>
    /// ClojureScript language
    /// </summary>
    [Description("clojurescript")]
    ClojureScript,
    
    /// <summary>
    /// Elm language
    /// </summary>
    [Description("elm")]
    Elm,
    
    /// <summary>
    /// Erlang language
    /// </summary>
    [Description("erlang")]
    Erlang,
    
    /// <summary>
    /// Visual Basic .NET language
    /// </summary>
    [Description("vbnet")]
    VbNet,
    
    /// <summary>
    /// VBScript language
    /// </summary>
    [Description("vbscript")]
    VbScript,
    
    /// <summary>
    /// JSON language
    /// </summary>
    [Description("json")]
    Json,
    
    /// <summary>
    /// YAML language
    /// </summary>
    [Description("yaml")]
    Yaml,
    
    /// <summary>
    /// Markdown language
    /// </summary>
    [Description("markdown")]
    Markdown,
    
    /// <summary>
    /// TOML language
    /// </summary>
    [Description("toml")]
    Toml,
    
    /// <summary>
    /// Ini language
    /// </summary>
    [Description("ini")]
    Ini,
    
    /// <summary>
    /// XML language
    /// </summary>
    [Description("xml")]
    Xml
}

/// <summary>
/// Extensions for MudExCodeLanguage
/// </summary>
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

    /// <summary>
    /// Returns the code language for a file
    /// </summary>
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