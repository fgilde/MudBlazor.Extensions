using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components;

namespace MudBlazor.Extensions;


/// <summary>
/// Represents an action within a MudBlazor dialog that returns a specific dialog result.
/// </summary>
public class MudExDialogResultAction
{
    private Func<IComponent, bool> _canExecuteFunc;

    /// <summary>
    /// Specify a condition for this action to be enabled.
    /// </summary>
    public MudExDialogResultAction WithCondition<TComponent>(Func<TComponent, bool> canExecuteFunc) where TComponent : IComponent
    {
        _canExecuteFunc = c => c is TComponent tComponent && canExecuteFunc(tComponent);
        return this;
    }

    internal bool CanExecuted(ComponentBase component) => _canExecuteFunc?.Invoke(component) ?? true;
    internal MudExDialogActionButton RenderedReference;

    /// <summary>
    /// Attach custom on click
    /// </summary>
    public Action OnClick { get; set; }

    /// <summary>
    /// Gets or sets the DialogResult for this action.
    /// </summary>
    public DialogResult Result { get; set; }

    /// <summary>
    /// Gets or sets the text displayed on the action button.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Gets or sets the visual style variant for the action button.
    /// </summary>
    public Variant Variant { get; set; }

    /// <summary>
    /// Gets or sets the color for the action button.
    /// </summary>
    public Color Color { get; set; }

    /// <summary>
    /// Returns true if this action has a condition.
    /// </summary>
    public bool HasCondition => _canExecuteFunc != null;
    //public bool Disabled { get; set; }

    /// <summary>
    /// Creates an array with a single "Ok" action.
    /// </summary>
    /// <param name="confirmText">The text for the "Ok" button. Defaults to "Ok".</param>
    /// <returns>An array with a single "Ok" action.</returns>
    public static MudExDialogResultAction[] Ok(string confirmText = "Ok") => new[] { OkCancel(confirmText).Last() };

    /// <summary>
    /// Creates an array with a single "Ok" action.
    /// </summary>
    /// <param name="canExecuteFunc">Function to determine if action can be executed</param>
    /// <param name="confirmText">The text for the "Ok" button. Defaults to "Ok".</param>
    /// <returns>An array with a single "Ok" action.</returns>
    public static MudExDialogResultAction[] OkWithCondition<TComponent>(Func<TComponent, bool> canExecuteFunc, string confirmText = "Ok") where TComponent : IComponent
        => new[] { OkCancelWithOkCondition(canExecuteFunc, confirmText).Last() };

    /// <summary>
    /// Creates an array with a single "Cancel" action.
    /// </summary>
    /// <param name="confirmText">The text for the "Cancel" button. Defaults to "Ok".</param>
    /// <returns>An array with a single "Cancel" action.</returns>
    public static MudExDialogResultAction[] Cancel(string confirmText = "Ok") => new[] { OkCancel(confirmText).First() };

    /// <summary>
    /// Creates an array with both "Ok" and "Cancel" actions.
    /// </summary>
    /// <param name="canExecuteFunc">Function to determine if action can be executed</param>
    /// <param name="confirmText">The text for the "Ok" button. Defaults to "Ok".</param>
    /// <param name="cancelText">The text for the "Cancel" button. Defaults to "Cancel".</param>
    /// <returns>An array with both "Ok" and "Cancel" actions.</returns>
    public static MudExDialogResultAction[] OkCancelWithOkCondition<TComponent>(Func<TComponent, bool> canExecuteFunc, string confirmText = "Ok", string cancelText = "Cancel") where TComponent : IComponent
    {
        var actions = OkCancel(confirmText, cancelText);
        actions.Last().WithCondition(canExecuteFunc);
        return actions;
    }

    /// <summary>
    /// Creates an array with both "Ok" and "Cancel" actions.
    /// </summary>
    /// <param name="confirmText">The text for the "Ok" button. Defaults to "Ok".</param>
    /// <param name="cancelText">The text for the "Cancel" button. Defaults to "Cancel".</param>
    /// <returns>An array with both "Ok" and "Cancel" actions.</returns>
    public static MudExDialogResultAction[] OkCancel(string confirmText = "Ok", string cancelText = "Cancel")
    {
        var actions = new[]
        {
            new MudExDialogResultAction
            {
                Label = cancelText,
                Variant = Variant.Text,
                Result = DialogResult.Cancel()
            },
            new MudExDialogResultAction
            {
                Label = confirmText,
                Color = Color.Error,
                Variant = Variant.Filled,
                Result = DialogResult.Ok(true)
            },
        };
        return actions;
    }

}