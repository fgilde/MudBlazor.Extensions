namespace MudBlazor.Extensions
{
    public class DialogOptionsEx : DialogOptions
    {
        public bool? MaximizeButton { get; set; }
        //public bool? MinimizeButton { get; set; }
        public bool Resizeable { get; set; }
        public MudDialogButton[] Buttons { get; set; }
        public MudDialogDragMode DragMode { get; set; }
        public bool? FullHeight { get; set; }
        public bool? DisableMargin { get; set; }
    }
}