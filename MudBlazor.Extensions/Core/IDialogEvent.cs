using Microsoft.AspNetCore.Components;
using MudBlazor.Interop;

namespace MudBlazor.Extensions.Core;

public interface IDialogEvent
{
    string DialogId { get; }
    ComponentBase Dialog { get; }    
}

public class BaseDialogEvent : IDialogEvent {
    public string DialogId { get; set; }
    public ComponentBase Dialog { get; set; }
    public BoundingClientRect Rect { get; set; }
}

public class DialogBeforeOpenEvent : IDialogEvent {
    public string DialogId { get; set; }
    public ComponentBase Dialog { get; set; }
    public IDialogReference DialogReference { get; set; }
}

public class DialogAfterOpenEvent : DialogBeforeOpenEvent
{ }


public class DialogClosedEvent : DialogBeforeOpenEvent
{
    public DialogResult Result { get; set; }
}

public class DialogDragEndEvent : BaseDialogEvent { }
public class DialogDragStartEvent : BaseDialogEvent { }
public class DialogDraggingEvent : BaseDialogEvent { }
public class DialogResizingEvent : BaseDialogEvent { }
public class DialogResizedEvent : BaseDialogEvent { }

