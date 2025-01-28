using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Options;
using MudBlazor.Interop;

namespace MudBlazor.Extensions.Core;

public interface IDialogEvent
{
    string DialogId { get; }
    Guid DialogGuid => Guid.TryParse(DialogId.StartsWith("_") ? DialogId.Remove(0, 1) : DialogId, out var r) ? r : Guid.Empty;
    ComponentBase Dialog { get; }    
    DialogOptionsEx DialogOptions { get; set; }    
}

public class BaseDialogEvent : IDialogEvent {
    public string DialogId { get; set; }
    public ComponentBase Dialog { get; set; }
    public BoundingClientRect Rect { get; set; }
    public DialogOptionsEx DialogOptions { get; set; }

}

public class DialogBeforeOpenEvent : IDialogEvent {
    public string DialogId { get; set; }
    public ComponentBase Dialog { get; set; }
    public IDialogReference DialogReference { get; set; }
    public DialogOptionsEx DialogOptions { get; set; }
}

public class DialogAfterOpenEvent : DialogBeforeOpenEvent
{ }

public class DialogAfterAnimationEvent : BaseDialogEvent
{ }


public class DialogClosedEvent : DialogBeforeOpenEvent
{
    public DialogResult Result { get; set; }
}

public class DialogClosingEvent : BaseDialogEvent
{
    public bool? Cancel { get; set; }
}

public class DialogDragEndEvent : BaseDialogEvent { }
public class DialogDragStartEvent : BaseDialogEvent { }
public class DialogDraggingEvent : BaseDialogEvent { }
public class DialogResizingEvent : BaseDialogEvent { }
public class DialogResizedEvent : BaseDialogEvent { }

