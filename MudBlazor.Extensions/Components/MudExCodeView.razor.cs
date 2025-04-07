using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text;
using BlazorJS;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Core;
using MudBlazor.Utilities;
using MudBlazor.Extensions.Api;
using MudBlazor.Extensions.Attribute;
using Newtonsoft.Json;
using Nextended.Blazor.Helper;
using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Simple CodeViewer Component
/// </summary>
public partial class MudExCodeView
{
    private ElementReference _element;
    private string _markdownCode;
    private string _code;
    private bool _isExpanded;
    private bool _jsReady;
    private bool _copyCallbackIsAttached;
    private string _elementId = $"mudex-codeview-{Guid.NewGuid().ToString("N")[..8]}";
    /// <summary>
    /// Classname for the component
    /// </summary>
    protected string Classname =>
        new MudExCssBuilder("mud-ex-code-view")
            .AddClass(Class)
            .Build();

    /// <summary>
    /// Style for the component
    /// </summary>
    protected string StyleStr =>
        new MudExStyleBuilder()
            .AddRaw(Style)
            .WithOverflow("auto")
            .Build();

    /// <summary>
    /// Classname for the code view
    /// </summary>
    protected string CodeViewClassname =>
        new MudExCssBuilder("mud-ex-code-view")
            .AddClass("full-width", FullWidth)
            .AddClass("full-height", FullHeight)
            .AddClass(CodeClass)
            .Build();

    /// <summary>
    /// Style for the code view
    /// </summary>
    protected string CodeViewStyleStr =>
        new MudExStyleBuilder()
            .AddRaw(CodeStyle)
            .WithOverflow("auto")
            .Build();

    /// <summary>
    /// Classname for the code view container
    /// </summary>
    protected string RenderFragmentClassname =>
        new MudExCssBuilder("mud-ex-code-view-container")
            .AddClass(RenderFragmentClass)
            .Build();

    /// <summary>
    /// Style for the code view container
    /// </summary>
    protected string RenderFragmentStyleStr =>
        new MudExStyleBuilder()
            .AddRaw(RenderFragmentStyle)
            .WithOverflow("hidden")
            .Build();

    [Inject] private ISnackbar Snackbar { get; set; }

    /// <summary>
    /// If this is true, a toast is shown when the code is copied
    /// </summary>
    public bool ShowCopyToast { get; set; } = true;

    /// <summary>
    /// Is raised when the code is copied
    /// </summary>
    [Parameter] public EventCallback OnCodeCopied { get; set; }

    /// <summary>
    /// MinWidth for the left docked panel when split view is used
    /// </summary>
    [Parameter] public string DockedMinWidthLeft { get; set; } = "350px";
    
    /// <summary>
    /// MinWidth for the right docked panel when split view is used
    /// </summary>
    [Parameter] public string DockedMinWidthRight { get; set; } = "350px;";
    
    /// <summary>
    /// MinHeight for the left docked panel when split view is used
    /// </summary>
    [Parameter] public string DockedMinHeightLeft { get; set; } = "150px";
    
    /// <summary>
    /// MinHeight for the right docked panel when split view is used
    /// </summary>
    [Parameter] public string DockedMinHeightRight { get; set; } = "150px;";

    /// <summary>
    /// User class names, separated by space.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.ComponentBase.Common)]
    public string CodeClass { get; set; }

    /// <summary>
    /// User styles, applied on top of the component's own classes and styles.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.ComponentBase.Common)]
    public string CodeStyle { get; set; }

    /// <summary>
    /// User class names, separated by space.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.ComponentBase.Common)]
    public string RenderFragmentClass { get; set; }

    /// <summary>
    /// User styles, applied on top of the component's own classes and styles.
    /// </summary>
    [Parameter]
    [SafeCategory(CategoryTypes.ComponentBase.Common)]
    public string RenderFragmentStyle { get; set; }

    /// <summary>
    /// Specify layout if render content and code ist displayed
    /// </summary>
    [Parameter] public CodeViewModeWithRenderFragment CodeViewModeWithRenderFragment { get; set; } = CodeViewModeWithRenderFragment.ExpansionPanel;

    /// <summary>
    /// Text for code copied toast
    /// </summary>
    [Parameter] public string CodeCopiedText { get; set; } = "Code copied";

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
    [Parameter] public MudExCodeLanguage Language { get; set; } = MudExCodeLanguage.CSharp;

    /// <summary>
    /// Code is expanded
    /// </summary>
    [Parameter] public bool CodeIsExpanded { get; set; }

    /// <summary>
    /// FullWidth
    /// </summary>
    [Parameter] public bool FullWidth { get; set; } = true;

    /// <summary>
    /// FullHeight
    /// </summary>
    [Parameter]
    public bool FullHeight { get; set; } = true;

    /// <summary>
    /// Theme for code
    /// </summary>
    [Parameter] public CodeBlockTheme Theme { get; set; } = CodeBlockTheme.AtomOneDark;

    /// <summary>
    /// ChildContent to show code for
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Set to true to render also the given child content otherwise only code is generated for
    /// </summary>
    [Parameter] public bool RenderChildContent { get; set; }

    /// <summary>
    /// Code to show
    /// </summary>
    [Parameter]
    public string Code
    {
        get => _code;
        set
        {
            _copyCallbackIsAttached = false;
            _code = _markdownCode = TryLocalize(LoadingText);
            if (IsRendered)
            {
                StateHasChanged();
            }

            Task.Delay(10).ContinueWith(_ =>
            {
                _markdownCode = CodeAsMarkup(value, Language.GetDescription());
                if (IsRendered)
                {
                    InvokeAsync(StateHasChanged);
                }
            });
            _code = value;
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _isExpanded = CodeIsExpanded;
            await JsRuntime.LoadMudMarkdownAsync();
            _jsReady = true;
            if (ChildContent != null && string.IsNullOrWhiteSpace(Code))
                Code = FormatHtml(CodeFromFragment(ChildContent));
            await InvokeAsync(StateHasChanged);
        }
        
        await AttachCopyClickAsync();
        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task AttachCopyClickAsync()
    {
        if(_copyCallbackIsAttached)
            return;
        _copyCallbackIsAttached = true;
        var cbref = DotNetObjectReference.Create(this);
        var callbackMethodName = nameof(OnCopyClick);
        _copyCallbackIsAttached = await JsRuntime.DInvokeAsync<bool>((w, el, dotnet, methodName) =>
        {
            if(!el || !el.querySelector)
                return false;
            var btn = el.querySelector("button");
            if(!btn)
                return false;
            w.MudExEventHelper.addEventListenerCallback(btn, "click", methodName, dotnet);
            return true;
        }, _element, cbref, callbackMethodName).AsTask();
    }

    /// <summary>
    ///  Callback when the copy button is clicked
    /// </summary>
    [JSInvokable]
    public Task OnCopyClick()
    {
        if (Snackbar != null && ShowCopyToast)
            Snackbar.Add(LocalizerToUse.TryLocalize(CodeCopiedText), Severity.Success);
        return OnCodeCopied.InvokeAsync();
    }

    private Task OnExpandedChanged(bool arg)
    {
        _isExpanded = arg;
        return Task.CompletedTask;
    }

    #region Static methods for code generation

    /// <summary>
    /// Returns the given code to markup code value
    /// </summary>
    public static string CodeAsMarkup(string code, string lang = "c#") => $"```{lang}{Environment.NewLine}{code}{Environment.NewLine}```";

    /// <summary>
    /// Executes the given action and returns the code for the action as string
    /// </summary>
    public static string ExecuteAndReturnFuncAsString(Action func, bool replaceLambda = true, [CallerArgumentExpression(nameof(func))] string caller = null)
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
    public static string GenerateBlazorMarkupFromInstance<TComponent>(TComponent componentInstance, string comment = "")
    {
        // TODO: Move to central place with ApiMemberInfo
        var componentName = componentInstance?.GetType().FullName?.Replace(componentInstance.GetType().Namespace + ".", string.Empty);
        var properties = componentInstance?.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => ObjectEditMeta.IsAllowedAsPropertyToEdit(p) && ObjectEditMeta.IsAllowedAsPropertyToEditOnAComponent<TComponent>(p));

        var props = properties?.ToDictionary(info => info.Name, info => info.GetValue(componentInstance))
            .Where(pair => ComponentRenderHelper.IsValidParameter(typeof(TComponent), pair.Key, pair.Value)
                           && pair.Value != null
                           && pair.Value?.GetType() != typeof(object)) ?? Enumerable.Empty<KeyValuePair<string, object>>();

        var parameterString = string.Join("\n",
            props.Select(p => new KeyValuePair<string, string>(p.Key, MarkupValue(p.Value, p.Key)))
                .Where(p => !string.IsNullOrWhiteSpace(p.Value))
                .Select(p => $"{p.Key}=\"{p.Value}\""));

        var tags = GetComponentTagNames(componentName);
        var markup = $"<{tags.StartTag}\n{parameterString}\n></{tags.EndTag}>";

        if (!string.IsNullOrWhiteSpace(comment))
            markup = $"<!-- {comment} -->\n{markup}";

        return markup;
    }

    /// <summary>
    /// Returns the start and end tag for the given component name
    /// </summary>
    public static (string StartTag, string EndTag) GetComponentTagNames(string input)
    {
        if (!input.Contains("`"))
        {
            return (input, input);
        }
        input = ApiMemberInfo.GetGenericFriendlyTypeName(input);
        var match = Regex.Match(input, @"^(?<class>\w+)<(?<type>.+)>$");


        var className = match.Groups["class"].Value;
        var typeName = match.Groups["type"].Value;

        var formatted1 = $"{className} T=\"{typeName}\"";
        var formatted2 = className;

        return (formatted1, formatted2);
    }


    private static string MarkupValue(object value, string propName, bool ignoreComplexTypes = true)
    {
        if (value == null || string.IsNullOrEmpty(value.ToString()))
            return null;

        if (value is EventCallback || (value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(EventCallback<>)))
        {
            return $"@Your{propName}EventHandler";
        }

        if (value is bool)
            return value.ToString()?.ToLower();

        if (value.GetType().IsEnum)
            return $"{value.GetType().FullName}.{value}";

        if (value is DateTime dt)
            return $"@(System.DateTime.Parse(\"{dt}\"))";
        if (value is TimeSpan ts)
            return $"@(System.TimeSpan.Parse(\"{ts}\"))";

        if (value is MudColor mudColor)
            value = new MudExColor(mudColor);

        if (value is System.Drawing.Color dc)
            value = new MudExColor(dc);

        if (value is MudExColor color)
            return $"@(\"{color.ToCssStringValue()}\")";

        if (value.ToString()?.StartsWith("<") == true)
        {
            var name = MudExSvg.SvgPropertyNameForValue(value.ToString());
            return name != null ? $"@{name}" : value.ToString()?.Replace("\"", "\\\"");
        }

        if (value is string || MudExObjectEditHelper.HandleAsPrimitive(value.GetType()))
        {
            return value.ToString();
        }

        if (ignoreComplexTypes)
            return null;

        //return $"{p.Value?.GetType().FullName}.{p.Value}";

        // Create a json string from the object and deserialize it in the markup
        var fullName = value.GetType().FullName;
        var friendlyTypeName = ApiMemberInfo.GetGenericFriendlyTypeName(fullName);
        var json = JsonConvert.SerializeObject(value);
        return $"@(Newtonsoft.Json.JsonConvert.DeserializeObject<{friendlyTypeName}>(\"{json.Replace("\"", "\\\"")}\"))";
    }

    /// <summary>
    /// Shortens the given markup
    /// </summary>
    public static string ShortenMarkup(string markup)
    {
        // Regex pattern to find empty tags
        string pattern = @"<(\w+)([^>]*)>\s*</\1>";

        // Replacement pattern for self-closing tags
        string replacement = @"<$1$2 />";

        // Replace empty tags with self-closing tags
        return RemoveEmptyLines(Regex.Replace(markup, pattern, replacement));
    }

    /// <summary>
    /// Removes empty lines from the given input
    /// </summary>
    public static string RemoveEmptyLines(string input)
    {
        var filteredLines = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
            .Where(line => !string.IsNullOrWhiteSpace(line));

        return string.Join(Environment.NewLine, filteredLines);
    }

    /// <summary>
    /// Formats html code
    /// </summary>
    public static string FormatHtml(string html)
    {
        html = html.Replace(Environment.NewLine, " ");
        while (html.Contains(">>"))
        {
            html = html.Replace(">>", ">");
        }

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

        return ShortenMarkup(string.Join("\n", lines));
    }

#pragma warning disable BL0006

    /// <summary>
    /// Returns the code from the given fragment
    /// </summary>
    public static string CodeFromFragment(RenderFragment fragment)
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

    private static string BuildAttribute(RenderTreeFrame frame)
    {
        var value = MarkupValue(frame.AttributeValue, frame.AttributeName);
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

    //private static string BuildTag(RenderTreeFrameType frameType, Type componentType, ReadOnlySpan<RenderTreeFrame> frames)
    //{
    //    var (baseName, attributes) = GetFriendlyComponentNameAndAttributes(componentType);
    //    var stringBuilder = new StringBuilder().Append('<').Append(baseName);

    //    if (!string.IsNullOrEmpty(attributes))
    //    {
    //        stringBuilder.Append(' ').Append(attributes);
    //    }

    //    // Determine if ChildContent is the only RenderFragment property
    //    var frameArray = frames.ToArray();
    //    bool isChildContentOnly = frameArray.Count(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeValue is RenderFragment) == 1
    //                              && frameArray.Any(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "ChildContent");

    //    // Process each attribute
    //    for (int i = 1; i < frames.Length; i++)
    //    {
    //        var frame = frames[i];
    //        if (frame.FrameType == RenderTreeFrameType.Attribute)
    //        {
    //            if (frame.AttributeValue is RenderFragment fragment && fragment != null)
    //            {
    //                // If not only ChildContent or attribute name is not ChildContent, wrap in tags
    //                if (!isChildContentOnly || frame.AttributeName != "ChildContent")
    //                {
    //                    stringBuilder.Append($"<{frame.AttributeName}>");
    //                }

    //                stringBuilder.Append(CodeFromFragment(fragment));

    //                if (!isChildContentOnly || frame.AttributeName != "ChildContent")
    //                {
    //                    stringBuilder.Append($"</{frame.AttributeName}>");
    //                }
    //            }
    //            else
    //            {
    //                // Process normal attribute
    //                stringBuilder.Append(BuildAttribute(frame));
    //            }
    //        }
    //    }

    //    stringBuilder.Append("</").Append(baseName).Append('>');

    //    return stringBuilder.ToString();
    //}

    private static string BuildTag(RenderTreeFrameType frameType, Type componentType, ReadOnlySpan<RenderTreeFrame> frames)
    {
        var (baseName, attributes) = GetFriendlyComponentNameAndAttributes(componentType);
        var stringBuilder = new StringBuilder().Append('<').Append(baseName);

        if (!string.IsNullOrEmpty(attributes))
        {
            stringBuilder.Append(' ').Append(attributes);
        }

        var framesArray = frames.ToArray();
        bool isChildContentOnly = framesArray.Count(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeValue is RenderFragment) == 1
                                  && framesArray.Any(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "ChildContent");

        bool hasRenderFragments = false;

        // Process each attribute
        for (int i = 1; i < frames.Length; i++)
        {
            var frame = frames[i];
            if (frame.FrameType == RenderTreeFrameType.Attribute)
            {
                if (frame.AttributeValue is RenderFragment fragment)
                {
                    hasRenderFragments = true;

                    // Open tag for RenderFragment if not only ChildContent or attribute name is not ChildContent
                    if (!isChildContentOnly || frame.AttributeName != "ChildContent")
                    {
                        stringBuilder.Append('>').Append($"<{frame.AttributeName}>");
                    }
                    else
                    {
                        stringBuilder.Append('>');
                    }

                    stringBuilder.Append(CodeFromFragment(fragment));

                    // Close tag for RenderFragment
                    if (!isChildContentOnly || frame.AttributeName != "ChildContent")
                    {
                        stringBuilder.Append($"</{frame.AttributeName}>");
                    }
                }
                else
                {
                    // Process normal attribute
                    stringBuilder.Append(BuildAttribute(frame));
                }
            }
        }

        // Close the main tag
        if (!hasRenderFragments)
        {
            stringBuilder.Append('>');
        }

        stringBuilder.Append("</").Append(baseName).Append('>');

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
#pragma warning restore BL0006

    #endregion

}