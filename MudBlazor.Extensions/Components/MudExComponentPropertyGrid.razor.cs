using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;
using System.Globalization;

namespace MudBlazor.Extensions.Components;

public partial class MudExComponentPropertyGrid<T>
{
    [Parameter] public bool ShowInherited { get; set; }
    [Parameter] public bool ShowInheritedToggle { get; set; } = true;
    [Parameter] public bool GroupByTypes { get; set; }
    [Parameter] public EventCallback<bool> GroupByTypesChanged { get; set; }
    [Parameter] public string GeneratedCodeComment { get; set; } = "This is generated and maybe not correct";
    [Parameter] public EventCallback<string> OnShowCode { get; set; }
    
    [Inject] private IJsApiService JsApiService { get; set; }
    [Inject] private MudExFileService FileService { get; set; }

    private bool rendered;
    private RenderFragment Inherited() => builder => base.BuildRenderTree(builder);
    bool _showInherited;
    bool _groupByTypes;

    protected override void RenderActions(RenderTreeBuilder __builder)
    {}

    protected override Task OnInitializedAsync()
    {
        if(!IsOverwritten(nameof(ToolBarContent)))
            ToolBarContent = RenderToolbarExtraContent();
        if (!IsOverwritten(nameof(StickyToolbar)))
            StickyToolbar = true;
        if (!IsOverwritten(nameof(ToolbarColor)))
            ToolbarColor = Color.Surface;
        if (!IsOverwritten(nameof(StickyToolbarTop)))
            StickyToolbarTop = "-8px";
        if (!IsOverwritten(nameof(StoreAndReadValueFromUrl)))
            StoreAndReadValueFromUrl = true;
        if (!IsOverwritten(nameof(MultiSearch)))
            MultiSearch = true;
        return base.OnInitializedAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _showInherited = ShowInherited;
            _groupByTypes = GroupByTypes;
        }
        base.OnAfterRender(firstRender);
        if (!rendered)
        {
            rendered = true;
            Task.Delay(500).ContinueWith(_ => InvokeAsync(StateHasChanged));
        }
    }

    protected override ObjectEditMeta<T> ConfigureMetaBase(ObjectEditMeta<T> meta)
    {
        meta.IgnoreAllObsoleteFields();
        if (ShowInheritedToggle)
        {
            meta.IgnoreAllInheritedFieldsIf(_ => !_showInherited, new[] { nameof(MudExBaseInput<T>.Color) });
        }

        if (_groupByTypes)
            meta.GroupByTypes();
        else
            meta.GroupByCategoryAttribute();
        meta.Properties<string>()
        .Where(p => p?.Value?.ToString()?.StartsWith("<g") == true || p?.Value?.ToString()?.StartsWith("<path") == true)
        .RenderWith<MudExIconPicker, string, string>(edit => edit.Value);

        meta.Properties<string>()
            .Where(p => IsValidCultureString(p.Value?.ToString()))
        .RenderWith<MudExCultureSelect, string, CultureInfo>(edit => edit.Value, edit => edit.CultureHandling = NeutralCultureHandling.IgnoreNeutralCultures,
                s => !string.IsNullOrWhiteSpace(s) ? CultureInfo.GetCultureInfo(s) : null,
                info => info?.Name ?? string.Empty);

        meta.AllProperties.WrapIn<MudExPropertyInfoContainer<T>>((p, cmp) => cmp.Property = p.PropertyInfo);

        return base.ConfigureMetaBase(meta);
    }

    protected virtual bool IsValidCultureString(string s)
    {
        if (string.IsNullOrEmpty(s))
            return false;

        try
        {
            CultureInfo culture = new CultureInfo(s);
            return s.Contains("-");
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }
    private Task OnChangeInherit(bool b)
    {
        _showInherited = !_showInherited;
        return Update();
    }

    private async Task OnChangeGrouping(bool b)
    {
        _groupByTypes = !_groupByTypes;
        GroupByTypes = _groupByTypes;
        await GroupByTypesChanged.InvokeAsync(_groupByTypes);
        await CreateMetaIfNotExists(true);
        await Update();
    }

    private Task Update()
    {
        UpdateAllConditions();
        Refresh();
        return Task.CompletedTask;
    }

    protected virtual CodeBlockTheme GetCodeTheme() => CodeBlockTheme.AtomOneDark;

    protected virtual async Task ShowCode()
    {
        var theme = GetCodeTheme();
        var generateBlazorMarkupFromInstance = MudExCodeView.GenerateBlazorMarkupFromInstance(Value, L[GeneratedCodeComment]);
        if (OnShowCode.HasDelegate)
        {
            await OnShowCode.InvokeAsync(generateBlazorMarkupFromInstance);
            return;
        }

        var cls = await MudExStyleBuilder.Default.WithOverflow("hidden").AsImportant().BuildAsClassRuleAsync("hidden-overflow");

        await DialogService.ShowComponentInDialogAsync<MudExCodeView>("Markup", null,
            md =>
            {
                md.Code = generateBlazorMarkupFromInstance;
                md.Theme = theme;
            },
            dialog =>
            {
                dialog.Icon = Icons.Material.Filled.Code;
                dialog.Buttons = MudExDialogResultAction.Ok();
                dialog.ContentStyle = "height: 85%; margin-bottom: 12px;";

            }, new DialogOptionsEx()
            {
                DialogAppearance = MudExAppearance.FromCss(MudExCss.Classes.Dialog.Glass, MudExCss.Classes.Dialog.FullHeightContent, cls),
                DragMode = MudDialogDragMode.Simple,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseButton = true,
                Resizeable = true,
            });
    }

    private async Task EditCode()
    {
        var code = MudExCodeView.GenerateBlazorMarkupFromInstance(Value, L[GeneratedCodeComment]);
        await TryMudExHelper.EditCodeInTryMudexAsync(code, JsRuntime);
    }
}