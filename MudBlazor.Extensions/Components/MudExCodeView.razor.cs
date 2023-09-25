using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Simple CodeViewer Component
/// </summary>
public partial class MudExCodeView
{
    private string _markdownCode;
    private string _code;

    /// <summary>
    /// Text for expand code
    /// </summary>
    [Parameter] public string ExpandedText { get; set; } = "Hide code";

    /// <summary>
    /// Text for hide code
    /// </summary>
    [Parameter] public string CollapsedText { get; set; } = "Show code";

    /// <summary>
    /// Text while loading
    /// </summary>
    [Parameter] public string LoadingText { get; set; } = "Loading...";

    /// <summary>
    /// Programming Language of code
    /// </summary>
    [Parameter] public string Language { get; set; } = "c#";

    /// <summary>
    /// Code is expanded
    /// </summary>
    [Parameter] public bool CodeIsExpanded { get; set; }

    /// <summary>
    /// Theme for code
    /// </summary>
    [Parameter] public CodeBlockTheme Theme { get; set; } = CodeBlockTheme.AtomOneDark;

    /// <summary>
    /// ChildContent to show code for
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Set to true to render also the given child content otherwise only code is generated for
    /// </summary>
    [Parameter] public bool RenderChildContent { get; set; }
    
    [Parameter]
    public string Code
    {
        get => _code;
        set
        {
            _code = _markdownCode = TryLocalize(LoadingText);
            StateHasChanged();
            Task.Delay(10).ContinueWith(task =>
            {
                _markdownCode = CodeAsMarkup(value, Language);
                StateHasChanged();
            });
            _code = value;
        }
    }


    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (ChildContent != null)
            Code = FormatHtml(CodeFromFragment(ChildContent)) + Environment.NewLine + Code;
    }


    /// <summary>
    /// Returns the given code to markup code value
    /// </summary>
    public static string CodeAsMarkup(string code, string lang = "c#") => $"```{lang}{Environment.NewLine}{code}{Environment.NewLine}```";

    /// <summary>
    /// Executes the given action and returns the code for the action as string
    /// </summary>
    public static string ExecuteAndReturnFuncAsString(Action func, bool replaceLambda = true, [CallerArgumentExpression("func")] string caller = null)
    {
        func();
        return replaceLambda ? ReplaceLambdaInFuncString(caller) : caller;
    }

    /// <summary>
    /// Returns the given function as string
    /// </summary>
    public static (string CodeStr, Action Func) FuncAsString(Action func, bool replaceLambda = true, [CallerArgumentExpression("func")] string caller = null)
    {
        return (replaceLambda ? ReplaceLambdaInFuncString(caller) : caller, func);
    }

    /// <summary>
    /// Removes lambda signs from code
    /// </summary>
    public static string ReplaceLambdaInFuncString(string caller)
    {
        caller = Regex.Replace(caller, @"^\s*\([^)]*\)\s*=>\s*{?", "", RegexOptions.Singleline);
        caller = Regex.Replace(caller, @"\s*}\s*$", "", RegexOptions.Singleline);
        return caller;
    }

    /// <summary>
    /// Generates Markup from instance
    /// </summary>
    public static string GenerateBlazorMarkupFromInstance<TComponent>(TComponent componentInstance)
    {
        var componentName = componentInstance.GetType().FullName.Replace(componentInstance.GetType().Namespace + ".", string.Empty);
        var properties = componentInstance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var props = properties.ToDictionary(info => info.Name, info => info.GetValue(componentInstance)).Where(pair => Nextended.Blazor.Helper.ComponentRenderHelper.IsValidParameter(typeof(TComponent), pair.Key, pair.Value));

        var parameterString = string.Join("\n", props.Select(p => $"    {p.Key}=\"{p.Value}\""));

        var markup = $"<{componentName}\n{parameterString}\n/>";

        return markup;
    }

    /// <summary>
    /// Formats html code
    /// </summary>
    public static string FormatHtml(string html)
    {
        string pattern = @"(\<[^/][^>]*\>)|(\<\/[^>]*\>)";
        string replacement = "$1\r\n$2";

        Regex rgx = new Regex(pattern);

        string result = rgx.Replace(html, replacement);
        result = Regex.Replace(result, @"[\r\n]+", "\r\n");
        result = Regex.Replace(result, @"([^\r\n])<", "$1\r\n<");
        result = Regex.Replace(result, @">([^\r\n])", ">\r\n$1");

        int indentLevel = 0;
        var lines = result.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("</"))
                indentLevel--;
            lines[i] = new string(' ', indentLevel * 4) + lines[i].Trim();
            if (lines[i].Contains("<") && !lines[i].Contains("</"))
                indentLevel++;
        }

        return string.Join("\n", lines);
    }

    private static string CodeFromFragment(RenderFragment? fragment)
    {
        if (fragment == null)
            return string.Empty;
        var builder = new RenderTreeBuilder();
        fragment(builder);

        var framesRange = builder.GetFrames();

        var frames = framesRange.Array.AsSpan(0, framesRange.Count);
        var stringBuilder = new StringBuilder();
        ProcessFrames(frames, stringBuilder);
        return stringBuilder.ToString();
    }


    private static void ProcessFrames(ReadOnlySpan<RenderTreeFrame> frames, StringBuilder stringBuilder)
    {
        for (var i = 0; i < frames.Length; i++)
        {
            var frame = frames[i];
            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Markup:
                    stringBuilder.Append(frame.MarkupContent);
                    break;
                case RenderTreeFrameType.Text:
                    stringBuilder.Append(frame.TextContent);
                    break;
                case RenderTreeFrameType.Element:
                    stringBuilder.Append(BuildTag(frame.FrameType, frame.ElementName, frames.Slice(i, frame.ElementSubtreeLength)));
                    i += frame.ElementSubtreeLength - 1;
                    break;

                case RenderTreeFrameType.Component:
                    stringBuilder.Append(BuildTag(frame.FrameType, frame.ComponentType, frames.Slice(i, frame.ComponentSubtreeLength)));
                    i += frame.ComponentSubtreeLength - 1;
                    break;
                case RenderTreeFrameType.Attribute:
                    if (frame.AttributeValue is not RenderFragment)
                        stringBuilder.Append(BuildAttribute(frame));
                    break;
                case RenderTreeFrameType.Region:
                case RenderTreeFrameType.ElementReferenceCapture:
                case RenderTreeFrameType.None:
                case RenderTreeFrameType.ComponentReferenceCapture:
                    break;
            }
        }
    }

    private static string GetAttributeValueAsString(object? value)
    {
        if (value == null)
            return string.Empty;

        var type = value.GetType();

        if (type.IsEnum)
            return $"{type.FullName}.{value}";

        if (type == typeof(bool))
            return value.ToString().ToLower();

        return value.ToString();
    }

    private static string BuildAttribute(RenderTreeFrame frame)
    {
        var value = GetAttributeValueAsString(frame.AttributeValue);
        return $" {frame.AttributeName}=\"{value}\"";
    }

    private static string BuildTag(RenderTreeFrameType frameType, string tagName, ReadOnlySpan<RenderTreeFrame> frames)
    {
        var frame = frames[0];
        var stringBuilder = new StringBuilder().Append('<').Append(tagName);

        ProcessFrames(frames.Slice(1, frame.ElementSubtreeLength - 1), stringBuilder);

        // Handle child content
        var childContentFrame = frames.Slice(1, frame.ElementSubtreeLength - 1).ToArray()
            .FirstOrDefault(f => f is { FrameType: RenderTreeFrameType.Attribute, AttributeValue: RenderFragment });

        if (childContentFrame.Sequence != 0)
        {
            stringBuilder.Append('>');
            stringBuilder.Append(CodeFromFragment((RenderFragment)childContentFrame.AttributeValue));
            stringBuilder.Append("</").Append(tagName).Append('>');
        }
        else
        {
            stringBuilder.Append("/>");
        }

        return stringBuilder.ToString();
    }

    private static string BuildTag(RenderTreeFrameType frameType, Type componentType, ReadOnlySpan<RenderTreeFrame> frames)
    {
        var (baseName, attributes) = GetFriendlyComponentNameAndAttributes(componentType);
        var frame = frames[0];
        var stringBuilder = new StringBuilder().Append('<').Append(baseName);

        if (!string.IsNullOrEmpty(attributes))
        {
            stringBuilder.Append(' ').Append(attributes);
        }

        ProcessFrames(frames.Slice(1, frame.ElementSubtreeLength - 1), stringBuilder);

        // Handle child content
        var childContentFrame = frames.Slice(1, frame.ElementSubtreeLength - 1).ToArray()
            .FirstOrDefault(f => f is { FrameType: RenderTreeFrameType.Attribute, AttributeValue: RenderFragment });

        if (childContentFrame.Sequence != 0)
        {
            stringBuilder.Append('>');
            stringBuilder.Append(CodeFromFragment((RenderFragment)childContentFrame.AttributeValue));
            stringBuilder.Append("</").Append(baseName).Append('>');
        }
        else
        {
            stringBuilder.Append("/>");
        }

        return stringBuilder.ToString();
    }


    private static (string BaseName, string Attributes) GetFriendlyComponentNameAndAttributes(Type type)
    {
        if (!type.IsGenericType)
            return (type.Name, "");

        string typeName = type.Name.Split('`')[0];
        var genericArguments = type.GetGenericArguments();
        var attributes = new List<string>();
        var genericParameters = type.GetGenericTypeDefinition().GetGenericArguments();

        for (int i = 0; i < genericParameters.Length; i++)
        {
            attributes.Add($"{genericParameters[i].Name}=\"{genericArguments[i].Name.ToLower()}\"");
        }

        var attributesString = string.Join(" ", attributes);
        return (typeName, attributesString);
    }


}