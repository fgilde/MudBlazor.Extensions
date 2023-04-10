using BlazorJS;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MudBlazor.Extensions.Components;

//public partial class MudExCardList<T>
//{
//    [Parameter] public ICollection<T> Items { get; set; } = Enumerable.Repeat<T>(default, 10).ToList();

//    [Parameter] public RenderFragment<T>? ItemTemplate { get; set; }

//    protected override Task OnAfterRenderAsync(bool firstRender)
//    {
//        if (firstRender)
//        {
//            JsRuntime.InvokeVoidAsync("eval", @"
//                document.querySelector("".mud-ex-cards"").onmousemove = e => {
//                  for(const card of document.getElementsByClassName(""mud-ex-card"")) {
//                    const rect = card.getBoundingClientRect(),
//                          x = e.clientX - rect.left,
//                          y = e.clientY - rect.top;

//                    card.style.setProperty(""--mouse-x"", `${x}px`);
//                    card.style.setProperty(""--mouse-y"", `${y}px`);
//                  };
//                }
//            ");
//        }
//        return base.OnAfterRenderAsync(firstRender);
//    }
//}