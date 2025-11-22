using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using MudBlazor.Extensions.Components.Base;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Component that captures component references from ChildContent
/// </summary>
public class MudExCaptureChild : MudExBaseComponent<MudExCaptureChild>
{
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Event fired for each captured component reference
    /// </summary>
    [Parameter] public EventCallback<object> OnComponentCaptured { get; set; }

    /// <summary>
    /// List of all captured component references
    /// </summary>
    public IReadOnlyList<object> ComponentReferences => _capturedComponents.AsReadOnly();

    /// <summary>
    /// First captured component reference (for backwards compatibility)
    /// </summary>
    public object? ComponentReference => _capturedComponents.Count > 0 ? _capturedComponents[0] : null;

    private readonly List<object> _capturedComponents = new();
    private RenderFragment? _wrappedChild;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        _capturedComponents.Clear();

        if (ChildContent != null)
        {
            _wrappedChild = WrapChildWithCapture(ChildContent, component =>
            {
                _capturedComponents.Add(component);
                if (OnComponentCaptured.HasDelegate)
                    _ = OnComponentCaptured.InvokeAsync(component);
            });
        }
        else
        {
            _wrappedChild = null;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_wrappedChild != null)
            _wrappedChild(builder);
        else
            ChildContent?.Invoke(builder);
    }

    /// <summary>
    /// Wraps a RenderFragment and captures all component references within it
    /// </summary>
    private static RenderFragment WrapChildWithCapture(
        RenderFragment original,
        Action<object> onCaptured)
    {
        // 1. Render ChildContent offline to analyze frames
        var tmpBuilder = new RenderTreeBuilder();
        original(tmpBuilder);

        var framesRange = tmpBuilder.GetFrames();
        var frames = framesRange.Array;
        var count = framesRange.Count;

        return builder =>
        {
            var seq = 0;
            RenderRange(builder, frames, 0, count, ref seq, onCaptured);
        };
    }

    /// <summary>
    /// Single-pass renderer über den Frame-Baum, der Components mit Capture ersetzt
    /// </summary>
    private static void RenderRange(
        RenderTreeBuilder builder,
        RenderTreeFrame[] frames,
        int start,
        int end,
        ref int seq,
        Action<object> onCaptured)
    {
        var i = start;

        while (i < end)
        {
            var frame = frames[i];

            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Component:
                    {
                        var subtreeEnd = i + frame.ComponentSubtreeLength;

                        builder.OpenComponent(seq++, frame.ComponentType);

                        // Attribute & evtl. vorhandene ReferenceCaptures übernehmen
                        for (var j = i + 1; j < subtreeEnd; j++)
                        {
                            var f = frames[j];

                            if (f.FrameType == RenderTreeFrameType.Attribute)
                            {
                                builder.AddAttribute(seq++, f.AttributeName, f.AttributeValue);
                            }
                            else if (f.FrameType == RenderTreeFrameType.ComponentReferenceCapture)
                            {
                                builder.AddComponentReferenceCapture(seq++, f.ComponentReferenceCaptureAction);
                            }
                        }

                        // Unser Capture
                        builder.AddComponentReferenceCapture(seq++, inst =>
                        {
                            if (inst != null)
                                onCaptured(inst);
                        });

                        builder.CloseComponent();

                        i = subtreeEnd;
                        break;
                    }

                case RenderTreeFrameType.Element:
                    {
                        var subtreeEnd = i + frame.ElementSubtreeLength;
                        builder.OpenElement(seq++, frame.ElementName);
                        RenderRange(builder, frames, i + 1, subtreeEnd, ref seq, onCaptured);
                        builder.CloseElement();
                        i = subtreeEnd;
                        break;
                    }

                case RenderTreeFrameType.Region:
                    {
                        var subtreeEnd = i + frame.RegionSubtreeLength;
                        builder.OpenRegion(seq++);
                        RenderRange(builder, frames, i + 1, subtreeEnd, ref seq, onCaptured);
                        builder.CloseRegion();
                        i = subtreeEnd;
                        break;
                    }

                case RenderTreeFrameType.Text:
                    builder.AddContent(seq++, frame.TextContent);
                    i++;
                    break;

                case RenderTreeFrameType.Markup:
                    builder.AddMarkupContent(seq++, frame.MarkupContent);
                    i++;
                    break;

                case RenderTreeFrameType.ElementReferenceCapture:
                    builder.AddElementReferenceCapture(seq++, frame.ElementReferenceCaptureAction);
                    i++;
                    break;

                // Attribute tauchen hier nur als Kinder von Element/Component auf und werden dort behandelt
                case RenderTreeFrameType.Attribute:
                case RenderTreeFrameType.ComponentReferenceCapture:
                    i++;
                    break;

                default:
                    i++;
                    break;
            }
        }
    }
}
