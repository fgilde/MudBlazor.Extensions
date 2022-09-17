using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Blazored.FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor.Extensions.Extensions;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit;

public partial class MudExObjectEditForm<T>
{
    public bool HasErrors => GetErrors().Any(s => !string.IsNullOrWhiteSpace(s));
    public bool Validated => _fluentValidated && _dataAnnotationsValidated;

    [Parameter] public Variant CancelButtonVariant { get; set; } = Variant.Filled;
    [Parameter] public Variant SaveButtonVariant { get; set; } = Variant.Filled;
    [Parameter] public Color CancelButtonColor { get; set; }
    [Parameter] public Color SaveButtonColor { get; set; } = Color.Success;
    [Parameter] public string CancelButtonIcon { get; set; } = Icons.Material.Filled.Cancel;
    [Parameter] public string SaveButtonIcon { get; set; } = Icons.Material.Filled.Save;
    [Parameter] public ActionAlignment SaveButtonIconAlignment { get; set; } = ActionAlignment.Left;
    [Parameter] public ActionAlignment CancelButtonIconAlignment { get; set; } = ActionAlignment.Left;
    [Parameter] public bool CancelButtonAsIconButton { get; set; }
    [Parameter] public bool SaveButtonAsIconButton { get; set; }
    [Parameter] public EventCallback<EditContext> OnValidSubmit { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public int Elevation { get; set; } = 0;
    [Parameter] public Color ActionBarColor { get; set; } = Color.Default;
    [Parameter] public bool StickyActionBar { get; set; }
    [Parameter] public string StickyStickyActionBarBottom { get; set; } = "0";
    [Parameter] public string ActionBarStyle { get; set; }
    [Parameter] public string ActionBarClass { get; set; }
    [Parameter] public string FormClass { get; set; }
    [Parameter] public string CardClass { get; set; }
    [Parameter] public string SaveButtonText { get; set; } = "Save";
    [Parameter] public string CancelButtonText { get; set; } = "Cancel";
    [Parameter] public bool ShowSaveButton { get; set; } = true;
    [Parameter] public bool ShowCancelButton { get; set; } = true;
    [Parameter] public ActionAlignment ActionBarActionAlignment { get; set; } = ActionAlignment.Right;
    [Parameter] public RenderFragment ActionContent { get; set; }
    [Parameter] public bool RenderActionContentFirst { get; set; }
    [Parameter] public bool RenderFormActionsInToolBar { get; set; }
    protected virtual bool OverwriteActionBar => false;
    protected virtual bool UseFormSubmit => true;
    private ButtonType SubmitButtonType => UseFormSubmit ? ButtonType.Submit : ButtonType.Button;
    private EditForm form;
    private FluentValidationValidator _fluentValidationValidator;
    private DataAnnotationsValidator _dataAnnotationValidator;
    private bool _fluentValidated => _fluentValidationValidator == null || _fluentValidationValidator.Validate(options => { options.IncludeAllRuleSets(); });
    private bool _dataAnnotationsValidated => _dataAnnotationValidator == null || !_dataValidations.Any();
    private IEnumerable<ValidationResult> _dataValidations => _dataAnnotationValidator?.Validate() ?? Enumerable.Empty<ValidationResult>();
    private Task OnSubmitButtonClick() => UseFormSubmit ? Task.CompletedTask : OnSubmit(new EditContext(Value));


    protected virtual async Task OnSubmit(EditContext arg)
    {
        IsInternalLoading = true;
        try
        {
            Value = GetUpdatedValue(); // To ensure data is set also fir disabled value bindings
            if (Value is IEditableObject editable)
                editable.EndEdit();
            await OnValidSubmit.InvokeAsync(arg);
        }
        finally
        {
            IsInternalLoading = false;
        }
    }

    protected virtual async Task Cancel()
    {
        await Reset();
        await OnCancel.InvokeAsync();
    }

    private IEnumerable<string> GetFluentValidationErrors()
    {
        if (_fluentValidationValidator == null)
            return Enumerable.Empty<string>();
        var editContext = _fluentValidationValidator.ExposeField<EditContext>("CurrentEditContext");
        return editContext == null ? Enumerable.Empty<string>() : editContext.GetValidationMessages().Select(s => LocalizerToUse.TryLocalize(s).ToString());
    }

    private IEnumerable<string> GetDataAnnotationErrors() 
        => _dataValidations.Where(v => !string.IsNullOrEmpty(v.ErrorMessage)).Select(v => LocalizerToUse.TryLocalize(v.ErrorMessage).ToString());

    private IEnumerable<string> GetErrors() 
        => GetFluentValidationErrors().Union(GetDataAnnotationErrors());

    protected string GetActionBarStyle()
    {
        var res = string.Empty;
        if (StickyActionBar && !string.IsNullOrWhiteSpace(StickyStickyActionBarBottom))
            res += $"bottom: {StickyStickyActionBarBottom};";
        if (ActionBarColor != Color.Inherit)
            res += $"background-color: {ActionBarColor.CssVarDeclaration()};";
        return $"{res} {ActionBarStyle}";
    }

    protected string GetActionBarClass()
    {
        var res = string.Empty;
        if (StickyActionBar)
            res += $"mud-ex-actionbar-sticky";

        return $"{res} {ActionBarClass} pr-5";
    }
}