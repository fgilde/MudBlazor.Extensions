using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Options;
using Nextended.Core.Contracts;
using Nextended.Core.Types;

namespace MudBlazor.Extensions;

/// <summary>
/// Contains extensions for component-based dialog actions.
/// </summary>
public static partial class DialogServiceExt
{
    /// <summary>
    /// Displays a dialog with a custom component and retrieves items from that component.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component to be displayed in the dialog.</typeparam>
    /// <typeparam name="T">The type of items to be retrieved from the component.</typeparam>
    /// <param name="dialogService">The dialog service instance used to show the dialog.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="headerText">The header text of the dialog.</param>
    /// <param name="cmpConfigure">An action to configure the component instance.</param>
    /// <param name="retrieveItemsFromComponent">A function to asynchronously retrieve items from the component.</param>
    /// <param name="buttons">An array of dialog result actions to configure dialog buttons and result conditions.</param>
    /// <param name="options">Optional dialog options to customize behavior and appearance.</param>
    /// <param name="dialogParameters">An optional action to configure additional dialog parameters.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an array of selected items of type <typeparamref name="T"/>.</returns>
    internal static async Task<T[]> InnerPickItemsAsync<TComponent, T>(
        this IDialogService dialogService,
        string title,
        string headerText,
        Action<TComponent> cmpConfigure,
        Func<TComponent, Task<IEnumerable<T>>> retrieveItemsFromComponent,
        MudExDialogResultAction[] buttons,
        DialogOptionsEx options = null,
        Action<MudExMessageDialog> dialogParameters = null
    ) where TComponent : ComponentBase, new()
    {
        dialogParameters ??= _ => { };

        var res = await dialogService.ShowComponentInDialogAsync(
            title,
            headerText,
            cmpConfigure,
            // Merge actions: given action can also override default action
            dialogParameters.CombineWith(dialog =>
            {
                dialog.Icon = Icons.Material.Filled.List;
                dialog.Buttons = buttons;
            }),
            options
        );

        if (!res.DialogResult.Canceled)
        {
            var result = await retrieveItemsFromComponent(res.Component);
            return result.ToArray();
        }

        return Array.Empty<T>();
    }

    /// <summary>
    /// Displays a dialog to let the user pick one or multiple items from a list.
    /// </summary>
    /// <typeparam name="T">The type of items to display in the list.</typeparam>
    /// <param name="dialogService">The dialog service instance used to show the dialog.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="headerText">The header text of the dialog.</param>
    /// <param name="itemsLoadFunc">A function that asynchronously loads the items to be displayed.</param>
    /// <param name="forceSelect">
    /// If set to <c>true</c>, the dialog cannot be closed without selecting an item.
    /// </param>
    /// <param name="allowEmpty">
    /// Indicates whether an empty selection is allowed.
    /// </param>
    /// <param name="itemToStringFunc">
    /// A function to convert an item to its string representation. If not provided, the item's <c>ToString</c> method is used.
    /// </param>
    /// <param name="options">Optional dialog options to customize the dialog behavior.</param>
    /// <param name="cmpConfigure">An optional action to further configure the list component.</param>
    /// <param name="dialogParameters">An optional action to configure additional dialog parameters.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an array of the selected items.
    /// </returns>
    public static async Task<T[]> PickItemsAsync<T>(
        this IDialogService dialogService,
        string title,
        string headerText,
        Func<Task<IEnumerable<T>>> itemsLoadFunc,
        bool forceSelect = false,
        bool allowEmpty = false,
        Func<T, string> itemToStringFunc = null,
        DialogOptionsEx options = null,
        Action<MudExList<T>> cmpConfigure = null,
        Action<MudExMessageDialog> dialogParameters = null
    )
    {
        cmpConfigure ??= _ => { };
        Func<MudExList<T>, bool> canOkCondition = c => allowEmpty || c.SelectedItem != null;
        itemToStringFunc ??= i => i.ToString();
        dialogParameters ??= _ => { };
        options ??= DialogOptionsEx.DefaultDialogOptions;

        if (forceSelect)
        {
            options.CloseButton = false;
            options.BackdropClick = false;
            options.CloseOnEscapeKey = false;
        }

        var items = (await itemsLoadFunc()).ToList();
        var buttons = forceSelect
            ? MudExDialogResultAction.OkWithCondition(canOkCondition)
            : MudExDialogResultAction.OkCancelWithOkCondition(canOkCondition);

        return await dialogService.InnerPickItemsAsync(
            title,
            headerText,
            cmpConfigure.CombineWith(lb =>
            {
                lb.ToStringFunc = itemToStringFunc;
                lb.ItemCollection = items;
                lb.SelectAll = true;
                lb.SearchBox = true;
                lb.SearchBoxBackgroundColor = Color.Transparent;
                lb.MultiSelection = true;
                lb.Unselectable = allowEmpty;
                lb.Clickable = true;
                lb.Color = Color.Primary;
            }),
            cmp => Task.FromResult(cmp.SelectedValues),
            buttons,
            options,
            dialogParameters);
    }

    /// <summary>
    /// Displays a dialog to let the user select one or more items using a select component.
    /// </summary>
    /// <typeparam name="T">The type of items available for selection.</typeparam>
    /// <param name="dialogService">The dialog service instance used to show the dialog.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="headerText">The header text of the dialog.</param>
    /// <param name="itemsLoadFunc">A function that asynchronously loads the items for selection.</param>
    /// <param name="forceSelect">
    /// If set to <c>true</c>, the dialog requires an item to be selected before closing.
    /// </param>
    /// <param name="allowEmpty">
    /// Indicates whether an empty selection is allowed.
    /// </param>
    /// <param name="itemToStringFunc">
    /// A function to convert an item to its string representation. Defaults to <c>ToString</c> if not provided.
    /// </param>
    /// <param name="options">Optional dialog options to customize the appearance and behavior.</param>
    /// <param name="cmpConfigure">An optional action to further configure the select component.</param>
    /// <param name="dialogParameters">An optional action to set additional dialog parameters.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an array of the selected items.
    /// </returns>
    public static async Task<T[]> SelectItemsAsync<T>(
        this IDialogService dialogService,
        string title,
        string headerText,
        Func<Task<IEnumerable<T>>> itemsLoadFunc,
        bool forceSelect = false,
        bool allowEmpty = false,
        Func<T, string> itemToStringFunc = null,
        DialogOptionsEx options = null,
        Action<MudExSelect<T>> cmpConfigure = null,
        Action<MudExMessageDialog> dialogParameters = null
    )
    {
        cmpConfigure ??= _ => { };
        Func<MudExSelect<T>, bool> canOkCondition = c => allowEmpty || c.Value != null;
        itemToStringFunc ??= i => i.ToString();
        dialogParameters ??= _ => { };
        options ??= DialogOptionsEx.DefaultDialogOptions;

        if (forceSelect)
        {
            options.CloseButton = false;
            options.BackdropClick = false;
            options.CloseOnEscapeKey = false;
        }

        var items = (await itemsLoadFunc()).ToList();
        var buttons = forceSelect
            ? MudExDialogResultAction.OkWithCondition(canOkCondition)
            : MudExDialogResultAction.OkCancelWithOkCondition(canOkCondition);

        var result = await dialogService.InnerPickItemsAsync(
            title,
            headerText,
            cmpConfigure.CombineWith(lb =>
            {
                lb.ToStringFunc = itemToStringFunc;
                lb.ItemCollection = items;
                lb.SelectAll = true;
                lb.SearchBox = true;
                lb.MultiSelection = true;
                lb.Color = Color.Primary;
            }),
            cmp => Task.FromResult(cmp.SelectedValues),
            buttons,
            options,
            dialogParameters);

        return result;
    }

    /// <summary>
    /// Displays a dialog to pick one or more items from a predefined set of items passed as parameters.
    /// </summary>
    /// <typeparam name="T">The type of items available for selection.</typeparam>
    /// <param name="dialogService">The dialog service instance used to show the dialog.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="headerText">The header text of the dialog.</param>
    /// <param name="items">A parameter array of items from which the user can make a selection.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an array of the selected items.</returns>
    public static Task<T[]> PickItemsAsync<T>(
        this IDialogService dialogService,
        string title,
        string headerText,
        params T[] items
    )
    {
        return dialogService.PickItemsAsync(title, headerText, () => Task.FromResult(items.AsEnumerable()));
    }

    /// <summary>
    /// Displays a dialog to pick one or more items from a predefined collection.
    /// </summary>
    /// <typeparam name="T">The type of items available for selection.</typeparam>
    /// <param name="dialogService">The dialog service instance used to display the dialog.</param>
    /// <param name="title">The dialog title.</param>
    /// <param name="headerText">The header text shown in the dialog.</param>
    /// <param name="items">A collection of items from which the user can choose.</param>
    /// <param name="forceSelect">If <c>true</c> the dialog cannot be closed without a selection.</param>
    /// <param name="allowEmpty">If <c>true</c>, an empty selection is allowed.</param>
    /// <param name="itemToStringFunc">A function to convert items to a string; if not provided, <c>ToString()</c> is used.</param>
    /// <param name="options">Optional dialog options for customization.</param>
    /// <param name="cmpConfigure">An optional action to further configure the list component.</param>
    /// <param name="dialogParameters">An optional action to set additional dialog parameters.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an array of the selected items.</returns>
    public static Task<T[]> PickItemsAsync<T>(
        this IDialogService dialogService,
        string title,
        string headerText,
        IEnumerable<T> items,
        bool forceSelect = false,
        bool allowEmpty = false,
        Func<T, string> itemToStringFunc = null,
        DialogOptionsEx options = null,
        Action<MudExList<T>> cmpConfigure = null,
        Action<MudExMessageDialog> dialogParameters = null
    )
    {
        return dialogService.PickItemsAsync(
            title, headerText,
            () => Task.FromResult(items),
            forceSelect, allowEmpty, itemToStringFunc, options, cmpConfigure,
            dialogParameters);
    }

    /// <summary>
    /// Displays a dialog for picking a single item from a list.
    /// </summary>
    /// <typeparam name="T">The type of the item to select.</typeparam>
    /// <param name="dialogService">The dialog service instance used to show the dialog.</param>
    /// <param name="title">The dialog title.</param>
    /// <param name="headerText">The header text of the dialog.</param>
    /// <param name="itemsLoadFunc">A function that asynchronously loads the items to display.</param>
    /// <param name="forceSelect">
    /// If set to <c>true</c>, the dialog requires a selection and cannot be closed without choosing an item.
    /// </param>
    /// <param name="allowEmpty">Specifies whether a null or empty selection is permitted.</param>
    /// <param name="itemToStringFunc">
    /// A function to convert an item to its string representation. Uses <c>ToString()</c> if not provided.
    /// </param>
    /// <param name="options">Optional dialog options for further customization.</param>
    /// <param name="cmpConfigure">
    /// An optional action to configure the list component with the selection settings; multi-selection is disabled.
    /// </param>
    /// <param name="dialogParameters">An optional action to configure additional dialog parameters.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the selected item, or the default value if none was selected.
    /// </returns>
    public static async Task<T> PickItemAsync<T>(
        this IDialogService dialogService,
        string title,
        string headerText,
        Func<Task<IEnumerable<T>>> itemsLoadFunc,
        bool forceSelect = false,
        bool allowEmpty = false,
        Func<T, string> itemToStringFunc = null,
        DialogOptionsEx options = null,
        Action<MudExList<T>> cmpConfigure = null,
        Action<MudExMessageDialog> dialogParameters = null
    )
    {
        cmpConfigure ??= _ => { };

        var res = await dialogService.PickItemsAsync(
            title, headerText, itemsLoadFunc, forceSelect, allowEmpty,
            itemToStringFunc, options, cmpConfigure.CombineWith(c =>
            {
                c.MultiSelection = false;
            }), dialogParameters);

        return res.FirstOrDefault();
    }

    /// <summary>
    /// Displays a dialog for picking a single item from a predefined set of items.
    /// </summary>
    /// <typeparam name="T">The type of the item to select.</typeparam>
    /// <param name="dialogService">The dialog service instance used to display the dialog.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="headerText">The header text of the dialog.</param>
    /// <param name="items">A parameter array of items to choose from.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the selected item, or the default value if no selection was made.</returns>
    public static Task<T> PickItemAsync<T>(
        this IDialogService dialogService,
        string title,
        string headerText,
        params T[] items
    )
    {
        return dialogService.PickItemAsync(title, headerText, () => Task.FromResult(items.AsEnumerable()));
    }

    /// <summary>
    /// Displays a dialog for picking a single item from a predefined collection.
    /// </summary>
    /// <typeparam name="T">The type of the item to select.</typeparam>
    /// <param name="dialogService">The dialog service instance used to display the dialog.</param>
    /// <param name="title">The dialog title.</param>
    /// <param name="headerText">The header text displayed in the dialog.</param>
    /// <param name="items">A collection of items available for selection.</param>
    /// <param name="forceSelect">
    /// If set to <c>true</c>, forces the user to make a selection before the dialog can be closed.
    /// </param>
    /// <param name="allowEmpty">Indicates whether an empty selection is allowed.</param>
    /// <param name="itemToStringFunc">
    /// A function to determine the string representation of an item. Defaults to <c>ToString()</c> if not specified.
    /// </param>
    /// <param name="options">Optional dialog options to adjust the behavior and appearance.</param>
    /// <param name="cmpConfigure">An optional action to further configure the list component.</param>
    /// <param name="dialogParameters">An optional action to specify additional dialog parameters.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the selected item, or the default value if none was selected.
    /// </returns>
    public static Task<T> PickItemAsync<T>(
        this IDialogService dialogService,
        string title,
        string headerText,
        IEnumerable<T> items,
        bool forceSelect = false,
        bool allowEmpty = false,
        Func<T, string> itemToStringFunc = null,
        DialogOptionsEx options = null,
        Action<MudExList<T>> cmpConfigure = null,
        Action<MudExMessageDialog> dialogParameters = null
    )
    {
        return dialogService.PickItemAsync(
            title, headerText,
            () => Task.FromResult(items),
            forceSelect, allowEmpty,
            itemToStringFunc, options, cmpConfigure,
            dialogParameters);
    }

    /// <summary>
    /// Displays a dialog to select one or multiple items using a select component from a predefined set of items.
    /// </summary>
    /// <typeparam name="T">The type of items available for selection.</typeparam>
    /// <param name="dialogService">The dialog service instance used to display the dialog.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="headerText">The header text displayed in the dialog.</param>
    /// <param name="items">A parameter array of items available for selection.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an array of selected items.</returns>
    public static Task<T[]> SelectItemsAsync<T>(
        this IDialogService dialogService,
        string title,
        string headerText,
        params T[] items
    )
    {
        return dialogService.SelectItemsAsync(title, headerText, () => Task.FromResult(items.AsEnumerable()));
    }

    /// <summary>
    /// Displays a dialog to select one or multiple items from a predefined collection using a select component.
    /// </summary>
    /// <typeparam name="T">The type of items available for selection.</typeparam>
    /// <param name="dialogService">The dialog service instance used to display the dialog.</param>
    /// <param name="title">The dialog title.</param>
    /// <param name="headerText">The header text for the dialog.</param>
    /// <param name="items">A collection of items available for selection.</param>
    /// <param name="forceSelect">
    /// If set to <c>true</c>, the dialog requires a selection before closing.
    /// </param>
    /// <param name="allowEmpty">Specifies whether an empty selection is allowed.</param>
    /// <param name="itemToStringFunc">
    /// A function used to convert each item to a string. Defaults to using <c>ToString()</c> if not provided.
    /// </param>
    /// <param name="options">Optional dialog options to further customize the dialog.</param>
    /// <param name="cmpConfigure">An optional action to configure the select component.</param>
    /// <param name="dialogParameters">An optional action to set additional dialog parameters.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an array of the selected items.
    /// </returns>
    public static Task<T[]> SelectItemsAsync<T>(
        this IDialogService dialogService,
        string title,
        string headerText,
        IEnumerable<T> items,
        bool forceSelect = false,
        bool allowEmpty = false,
        Func<T, string> itemToStringFunc = null,
        DialogOptionsEx options = null,
        Action<MudExSelect<T>> cmpConfigure = null,
        Action<MudExMessageDialog> dialogParameters = null
    )
    {
        return dialogService.SelectItemsAsync(
            title, headerText,
            () => Task.FromResult(items),
            forceSelect, allowEmpty, itemToStringFunc, options, cmpConfigure,
            dialogParameters);
    }

    /// <summary>
    /// Displays a dialog for selecting a single item using a select component.
    /// </summary>
    /// <typeparam name="T">The type of item to select.</typeparam>
    /// <param name="dialogService">The dialog service instance used to display the dialog.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="headerText">The header text of the dialog.</param>
    /// <param name="itemsLoadFunc">A function that asynchronously loads the items to choose from.</param>
    /// <param name="forceSelect">
    /// If set to <c>true</c>, the dialog requires a selection before closing.
    /// </param>
    /// <param name="allowEmpty">Specifies whether an empty selection is allowed.</param>
    /// <param name="itemToStringFunc">
    /// A function used to convert an item to its string representation. Defaults to using <c>ToString()</c> if not provided.
    /// </param>
    /// <param name="options">Optional dialog options to configure the dialog.</param>
    /// <param name="cmpConfigure">
    /// An optional action to further configure the select component; multi-selection is disabled in this case.
    /// </param>
    /// <param name="dialogParameters">An optional action to configure additional dialog parameters.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the selected item, or the default value if none was selected.
    /// </returns>
    public static async Task<T> SelectItemAsync<T>(
        this IDialogService dialogService,
        string title,
        string headerText,
        Func<Task<IEnumerable<T>>> itemsLoadFunc,
        bool forceSelect = false,
        bool allowEmpty = false,
        Func<T, string> itemToStringFunc = null,
        DialogOptionsEx options = null,
        Action<MudExSelect<T>> cmpConfigure = null,
        Action<MudExMessageDialog> dialogParameters = null
    )
    {
        cmpConfigure ??= _ => { };

        var res = await dialogService.SelectItemsAsync(
            title, headerText,
            itemsLoadFunc,
            forceSelect, allowEmpty, itemToStringFunc, options, cmpConfigure.CombineWith(c =>
            {
                c.MultiSelection = false;
            }), dialogParameters);

        return res.FirstOrDefault();
    }

    /// <summary>
    /// Displays a dialog for selecting a single item from a predefined list using a select component.
    /// </summary>
    /// <typeparam name="T">The type of the item to select.</typeparam>
    /// <param name="dialogService">The dialog service instance used to display the dialog.</param>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="headerText">The header text of the dialog.</param>
    /// <param name="items">A parameter array of items available for selection.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the selected item, or the default value if none was selected.
    /// </returns>
    public static Task<T> SelectItemAsync<T>(
        this IDialogService dialogService,
        string title,
        string headerText,
        params T[] items
    )
    {
        return dialogService.SelectItemAsync(title, headerText, () => Task.FromResult(items.AsEnumerable()));
    }

    /// <summary>
    /// Displays a dialog for selecting a single item from a predefined collection using a select component.
    /// </summary>
    /// <typeparam name="T">The type of the item to select.</typeparam>
    /// <param name="dialogService">The dialog service instance used to display the dialog.</param>
    /// <param name="title">The dialog title.</param>
    /// <param name="headerText">The header text displayed in the dialog.</param>
    /// <param name="items">A collection of items available for selection.</param>
    /// <param name="forceSelect">
    /// If set to <c>true</c>, forces the user to select an item before closing the dialog.
    /// </param>
    /// <param name="allowEmpty">Specifies whether an empty selection is permitted.</param>
    /// <param name="itemToStringFunc">
    /// A function to convert an item to its string representation. Defaults to using <c>ToString()</c> if not provided.
    /// </param>
    /// <param name="options">Optional dialog options for further customization.</param>
    /// <param name="cmpConfigure">An optional action to configure the select component.</param>
    /// <param name="dialogParameters">An optional action to set additional dialog parameters.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the selected item, or the default value if no selection was made.
    /// </returns>
    public static Task<T> SelectItemAsync<T>(
        this IDialogService dialogService,
        string title,
        string headerText,
        IEnumerable<T> items,
        bool forceSelect = false,
        bool allowEmpty = false,
        Func<T, string> itemToStringFunc = null,
        DialogOptionsEx options = null,
        Action<MudExSelect<T>> cmpConfigure = null,
        Action<MudExMessageDialog> dialogParameters = null
    )
    {
        return dialogService.SelectItemAsync(
            title, headerText,
            () => Task.FromResult(items),
            forceSelect, allowEmpty,
            itemToStringFunc, options, cmpConfigure,
            dialogParameters);
    }
    
}