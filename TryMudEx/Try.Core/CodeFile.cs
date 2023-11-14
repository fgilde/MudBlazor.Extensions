namespace Try.Core
{
    using System;
    using System.Text.Json.Serialization;
    using System.Xml.Linq;

    public class CodeFile
    {
        public const string RazorFileExtension = ".razor";
        public const string CsharpFileExtension = ".cs";
        public const string RefFileExtension = ".ref";

        private CodeFileType? type;

        public string Path { get; init; }

        public string Content { get; set; }

        [JsonIgnore]
        public CodeFileType Type
        {
            get
            {
                if (!this.type.HasValue)
                {
                    var extension = System.IO.Path.GetExtension(this.Path);

                    this.type = extension switch
                    {
                        RazorFileExtension => CodeFileType.Razor,
                        CsharpFileExtension => CodeFileType.CSharp,
                        _ => CodeFileType.Hidden
                    };
                }

                return this.type.Value;
            }
        }

        public static CodeFile Create(string name)
        {
            var nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(name);

            var newCodeFile = new CodeFile { Path = name };

            newCodeFile.Content = DefaultContent(newCodeFile, nameWithoutExtension);

            return newCodeFile;
        }

        private static string DefaultContent(CodeFile newCodeFile, string nameWithoutExtension)
        {
            if (newCodeFile.Type == CodeFileType.CSharp)
                return string.Format(CoreConstants.DefaultCSharpFileContentFormat, nameWithoutExtension);
            if (newCodeFile.Type == CodeFileType.Razor)
            {
                if (newCodeFile.Path == CoreConstants.ImportsFileName)
                    return CoreConstants.DefaultImports;
                return string.Format(CoreConstants.DefaultRazorFileContentFormat, nameWithoutExtension);
            }
            return string.Empty;
        }
    }
}
