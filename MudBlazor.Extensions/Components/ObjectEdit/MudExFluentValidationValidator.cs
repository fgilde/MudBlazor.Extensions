using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace MudBlazor.Extensions.Components.ObjectEdit;

/// <summary>
/// A minimal FluentValidation integration component for Blazor EditContext.
/// Resolves IValidator&lt;T&gt; from DI and feeds validation errors into the EditContext.
/// </summary>
public class MudExFluentValidationValidator : ComponentBase, IDisposable
{
    private ValidationMessageStore _messageStore;

    [CascadingParameter]
    private EditContext CurrentEditContext { get; set; }

    [Inject]
    private IServiceProvider ServiceProvider { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (CurrentEditContext is null)
            return;

        _messageStore = new ValidationMessageStore(CurrentEditContext);
        CurrentEditContext.OnValidationRequested += OnValidationRequested;
        CurrentEditContext.OnFieldChanged += OnFieldChanged;
    }

    private void OnValidationRequested(object sender, ValidationRequestedEventArgs e) => ValidateModel();

    private void OnFieldChanged(object sender, FieldChangedEventArgs e) => ValidateModel();

    private void ValidateModel()
    {
        if (CurrentEditContext is null || _messageStore is null)
            return;

        _messageStore.Clear();

        var model = CurrentEditContext.Model;
        if (model is null)
            return;

        var validatorType = typeof(IValidator<>).MakeGenericType(model.GetType());
        if (ServiceProvider.GetService(validatorType) is not IValidator validator)
            return;

        var result = validator.Validate(new ValidationContext<object>(model));

        foreach (var error in result.Errors)
        {
            if (string.IsNullOrEmpty(error.PropertyName))
                continue;

            _messageStore.Add(
                new FieldIdentifier(model, error.PropertyName),
                error.ErrorMessage);
        }

        CurrentEditContext.NotifyValidationStateChanged();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (CurrentEditContext is null)
            return;

        CurrentEditContext.OnValidationRequested -= OnValidationRequested;
        CurrentEditContext.OnFieldChanged -= OnFieldChanged;
    }
}
