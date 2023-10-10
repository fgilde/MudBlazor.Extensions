using System.ComponentModel;
using Blazored.FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit;

/// <summary>
/// Powerful component to edit an object and its properties.
/// </summary>
/// <typeparam name="T">Type of object being edited</typeparam>
public partial class MudExObjectEditForm<T>
{
    /// <summary>
    /// Returns true if there are any errors in the edit form fields
    /// </summary>
    public bool HasErrors => GetErrors().Any(s => !string.IsNullOrWhiteSpace(s));

    /// <summary>
    /// Returns true if both FluentValidation and DataAnnotation validation have been completed successfully
    /// </summary>
    public bool Validated => GetErrors()?.Any() != true;

    /// <summary>
    /// The variant of the cancel button
    /// </summary>
    [Parameter]
    public Variant CancelButtonVariant { get; set; } = Variant.Filled;

    /// <summary>
    /// The variant of the save button
    /// </summary>
    [Parameter]
    public Variant SaveButtonVariant { get; set; } = Variant.Filled;

    /// <summary>
    /// The color of the cancel button
    /// </summary>
    [Parameter]
    public Color CancelButtonColor { get; set; }

    /// <summary>
    /// The color of the save button
    /// </summary>
    [Parameter]
    public Color SaveButtonColor { get; set; } = Color.Success;

    /// <summary>
    /// The icon of the cancel button
    /// </summary>
    [Parameter]
    public string CancelButtonIcon { get; set; } = Icons.Material.Filled.Cancel;

    /// <summary>
    /// The icon of the save button
    /// </summary>
    [Parameter]
    public string SaveButtonIcon { get; set; } = Icons.Material.Filled.Save;

    /// <summary>
    /// The alignment of the icon for the save button
    /// </summary>
    [Parameter]
    public ActionAlignment SaveButtonIconAlignment { get; set; } = ActionAlignment.Left;

    /// <summary>
    /// The alignment of the icon for the cancel button
    /// </summary>
    [Parameter]
    public ActionAlignment CancelButtonIconAlignment { get; set; } = ActionAlignment.Left;

    /// <summary>
    /// If true, display the cancel button as an icon button
    /// </summary>
    [Parameter]
    public bool CancelButtonAsIconButton { get; set; }

    /// <summary>
    /// If true, display the save button as an icon button
    /// </summary>
    [Parameter]
    public bool SaveButtonAsIconButton { get; set; }

    /// <summary>
    /// An event callback for when the form is successfully submitted
    /// </summary>
    [Parameter]
    public EventCallback<EditContext> OnValidSubmit { get; set; }

    /// <summary>
    /// An event callback for when the cancel button is pressed
    /// </summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }

    /// <summary>
    /// The elevation of the card containing the form
    /// </summary>
    [Parameter]
    public int Elevation { get; set; } = 0;

    /// <summary>
    /// The color of the action bar
    /// </summary>
    [Parameter]
    public MudExColor ActionBarColor { get; set; } = Color.Default;

    /// <summary>
    /// If true, the action bar is sticky
    /// </summary>
    [Parameter]
    public bool StickyActionBar { get; set; }

    /// <summary>
    /// The distance from the bottom of the viewport to the bottom of the sticky action bar
    /// </summary>
    [Parameter]
    public string StickyStickyActionBarBottom { get; set; } = "0";

    /// <summary>
    /// The style of the action bar
    /// </summary>
    [Parameter]
    public string ActionBarStyle { get; set; }

    /// <summary>
    /// The class of the action bar
    /// </summary>
    [Parameter]
    public string ActionBarClass { get; set; }

    /// <summary>
    /// The class of the form itself
    /// </summary>
    [Parameter]
    public string FormClass { get; set; }

    /// <summary>
    /// The class of the card containing the form
    /// </summary>
    [Parameter]
    public string CardClass { get; set; }

    /// <summary>
    /// The text displayed on the save button
    /// </summary>
    [Parameter]
    public string SaveButtonText { get; set; } = "Save";

    /// <summary>
    /// The text displayed on the cancel button
    /// </summary>
    [Parameter]
    public string CancelButtonText { get; set; } = "Cancel";

    /// <summary>
    /// If true, display the save button
    /// </summary>
    [Parameter]
    public bool ShowSaveButton { get; set; } = true;

    /// <summary>
    /// If true, display the cancel button
    /// </summary>
    [Parameter]
    public bool ShowCancelButton { get; set; } = true;

    /// <summary>
    /// The alignment of the action bar buttons
    /// </summary>
    [Parameter]
    public ActionAlignment ActionBarActionAlignment { get; set; } = ActionAlignment.Right;

    /// <summary>
    /// RenderFragment of action content
    /// </summary>
    [Parameter]
    public RenderFragment ActionContent { get; set; }

    /// <summary>
    /// If true, render action content before the form
    /// </summary>
    [Parameter]
    public bool RenderActionContentFirst { get; set; }

    /// <summary>
    /// If true, renders form actions (submit, cancel) in a tool bar
    /// </summary>
    [Parameter]
    public bool RenderFormActionsInToolBar { get; set; }

    /// <summary>
    /// Reference to EditForm
    /// </summary>
    public EditForm Form { get; private set; }

    /// <summary>
    /// Returns true if OverwriteActionBar is false, otherwise returns false
    /// </summary>
    protected virtual bool OverwriteActionBar => false;

    /// <summary>
    /// If true, submit the form on button click. Otherwise, do not submit until validation is complete
    /// </summary>
    protected virtual bool UseFormSubmit => true;

    /// <summary>
    /// Returns the button type of the submit button
    /// </summary>
    private ButtonType SubmitButtonType => UseFormSubmit ? ButtonType.Submit : ButtonType.Button;

    private FluentValidationValidator _fluentValidationValidator;
    private DataAnnotationsValidator _dataAnnotationValidator;

    /// <summary>
    /// Returns a list of validation results produced by DataAnnotation validation
    /// </summary>
    private IEnumerable<string> _dataValidations => (Form.EditContext?.GetValidationMessages() ?? Enumerable.Empty<string>());

    /// <summary>
    /// Handles form submission when UseFormSubmit is false
    /// </summary>
    /// <returns></returns>
    private Task OnSubmitButtonClick() => UseFormSubmit ? Task.CompletedTask : OnSubmit(new EditContext(Value));

    /// <summary>
    /// Called when the form is submitted
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Resets the form
    /// </summary>
    /// <returns></returns>
    protected virtual async Task Cancel()
    {
        await Reset();
        await OnCancel.InvokeAsync();
    }

    /// <summary>
    /// Returns a list of all errors in the form fields
    /// </summary>
    /// <returns></returns>
    private IEnumerable<string> GetErrors()
        => GetFluentValidationErrors().Union(GetDataAnnotationErrors());

    /// <summary>
    /// Returns a list of validation errors produced by FluentValidation validation
    /// </summary>
    /// <returns></returns>
    private IEnumerable<string> GetFluentValidationErrors()
    {
        if (_fluentValidationValidator == null)
            return Enumerable.Empty<string>();
        var editContext = _fluentValidationValidator.ExposeField<EditContext>("CurrentEditContext");
        return editContext == null ? Enumerable.Empty<string>() : editContext.GetValidationMessages().Select(s => LocalizerToUse.TryLocalize(s).ToString());
    }

    /// <summary>
    /// Returns a list of validation errors produced by DataAnnotation validation
    /// </summary>
    /// <returns></returns>
    private IEnumerable<string> GetDataAnnotationErrors()
        => _dataValidations.Where(v => !string.IsNullOrEmpty(v)).Select(v => LocalizerToUse.TryLocalize(v).ToString());

    /// <summary>
    /// Applies styles to the action bar based on properties
    /// </summary>
    /// <returns></returns>
    protected string GetActionBarStyle()
    {
        return MudExStyleBuilder.Default
            .WithBottom(StickyStickyActionBarBottom, StickyActionBar && !string.IsNullOrWhiteSpace(StickyStickyActionBarBottom))
            .WithBackgroundColor(ActionBarColor, !ActionBarColor.Is(Color.Inherit))
            .AddRaw(ActionBarStyle)
            .Build();
    }

    /// <summary>
    /// Returns the class of the action bar based on properties
    /// </summary>
    /// <returns></returns>
    protected string GetActionBarClass()
    {
        return MudExCssBuilder.Default
            .AddClass("mud-ex-actionbar-sticky", StickyActionBar)
            .AddClass(ActionBarClass)
            .AddClass("pr-5")
            .Build();
    }
}