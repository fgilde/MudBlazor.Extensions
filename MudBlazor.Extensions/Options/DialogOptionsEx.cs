using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Options
{
    public class DialogOptionsEx : DialogOptions
    {
        public bool? MaximizeButton { get; set; }
        //public bool? MinimizeButton { get; set; }
        public bool Resizeable { get; set; }
        public MudDialogButton[] Buttons { get; set; }
        public MudDialogDragMode DragMode { get; set; }
        public bool? FullHeight { get; set; }
        public bool? DisablePositionMargin { get; set; }
        public bool? DisableSizeMargin { get; set; }
        public AnimationOptions Animation { get; set; }
        public string[] DialogPositionNames => Position.GetPositionNames();

    }
}