using MudBlazor.Extensions.Components.ObjectEdit.Options;

namespace MudBlazor.Extensions.Components.ObjectEdit;

/// <summary>
/// Interface for configuring the <see cref="MudExObjectEdit{T}"/> component for a meta of TModel.
/// </summary>
/// <typeparam name="TModel">Model type to configure</typeparam>
public interface IObjectMetaConfiguration<TModel>
{
    /// <summary>
    /// Configure meta 
    /// </summary>
    /// <param name="meta"></param>
    /// <returns></returns>
    Task ConfigureAsync(ObjectEditMeta<TModel> meta);
}