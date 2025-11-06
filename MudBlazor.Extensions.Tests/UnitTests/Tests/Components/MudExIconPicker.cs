
using Bunit;
using Bunit.TestDoubles;
using MudBlazor.Extensions.Components;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class MudExIconPickerTest
{
    //[Fact]
    //public void ShouldDisplaysIcons()
    //{
    //    using var ctx = new TestContext();
    //    ctx.Services.AddMudServicesWithExtensions();
    //    ctx.Services.AddSingleton(JsRuntime);

    //    if (string.IsNullOrEmpty(file.Url))
    //    {
    //        await file.EnsureDataLoadedAsync();
    //        file.Url = await DataUrl.GetDataUrlAsync(file.Data, file.ContentType);
    //    }
    //    Console.WriteLine(file.Url);
    //    var component = ctx.RenderComponent<MudExFileDisplay>(parameters => parameters
    //        .Add(p => p.Url, file.Url)
    //        .Add(p => p.FileName, file.FileName)
    //        .Add(p => p.ContentType, file.ContentType)
    //        .Add(p => p.ShowContentError, false)
    //    );
    //    var markup = component.Markup;
    //    _tmpMarkup = markup;
    //    Console.WriteLine(markup);
    //    await InsertHtmlAsync(markup);
    //}

    public void ShouldDisplaysIcons()
    {
        using var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.SetupVoid("mudPopover.initialize", _ => true);
        context.JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true);

        var cut = context.RenderComponent<MudExIconPicker>();

        cut.Find("button.mud-button-root").Click();
        Assert.NotEmpty(cut.Find(".mud-icon-root")?.ToMarkup() ?? string.Empty);
    }
}