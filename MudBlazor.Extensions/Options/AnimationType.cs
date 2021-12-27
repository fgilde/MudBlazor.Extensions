using System.ComponentModel;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Options
{
    public enum AnimationType
    {
        Default,
        [Description("slide")] SlideIn
    }
}