using Microsoft.AspNetCore.Components.Forms;

namespace MudBlazor.Extensions.Core.FileManager;

/// <summary>
/// Optional companion to <see cref="IMudExFileStructureManager"/> for providers that can change the structure.
/// A read-only provider does not implement it, so it never carries methods it cannot fulfil.
/// </summary>
/// <remarks>
/// Implementing this interface is not permission to use it: the caller checks
/// <see cref="IMudExFileStructureManager.Capabilities"/> before offering an action.
/// The collection based operations exist so multi select and multi drag cause one call, not one per node.
/// <para>
/// Two exceptions carry two different meanings here, and callers should tell them apart: a provider that
/// cannot support an operation at all - because its storage has no such concept, or because it was never
/// granted the access - throws <see cref="NotSupportedException"/> for "this can never work here"; a provider
/// that does support the operation in general throws <see cref="InvalidOperationException"/> for "this
/// particular request was refused", for example a name collision. Do not gate UI on catching either - check
/// <see cref="IMudExFileStructureManager.Capabilities"/> up front instead.
/// </para>
/// <para>
/// Every operation here invalidates the nodes it was given. An operation that returns a node returns a
/// <em>new</em> node describing the result; the node passed in - and every descendant already loaded below it
/// - is stale afterwards, keeps its old name and path, and must not be reused. The collection based
/// operations return nothing at all for the same reason. A caller refreshes by re-listing the parent through
/// <see cref="IMudExFileStructureManager.GetChildrenAsync"/>, or
/// <see cref="IMudExFileStructureManager.GetRootAsync"/> at the top level.
/// </para>
/// </remarks>
public interface IMudExFileStructureWriter
{
    /// <summary>Creates a directory below <paramref name="parent"/> and returns the new node.</summary>
    /// <returns>A new node for the created directory. See the remarks on invalidation.</returns>
    /// <exception cref="NotSupportedException">Thrown when this provider cannot support the operation at all - see the remarks above.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the operation cannot be carried out as requested, for example because another entry already
    /// occupies the resulting location. A provider may throw it for other reasons of its own; callers should
    /// treat it as "this request was refused", not as a promise about which case applies.
    /// </exception>
    Task<MudExFileStructureNode> CreateDirectoryAsync(MudExFileStructureNode parent, string name, CancellationToken ct = default);

    /// <summary>Renames a node and returns a new node carrying the new name and path.</summary>
    /// <returns>
    /// A new node for the renamed entry. <paramref name="node"/> itself is left untouched and is stale from
    /// here on - it still reports the old name and path. See the remarks on invalidation.
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when this provider cannot support the operation at all - see the remarks above.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the operation cannot be carried out as requested, for example because <paramref name="newName"/>
    /// does not describe a plain name (a rename changes what a node is called, not where it lives), or because
    /// another entry already occupies the resulting location. A provider may throw it for other reasons of its
    /// own; callers should treat it as "this request was refused", not as a promise about which case applies.
    /// </exception>
    Task<MudExFileStructureNode> RenameAsync(MudExFileStructureNode node, string newName, CancellationToken ct = default);

    /// <summary>Moves all given nodes into the target directory. Every given node is stale afterwards.</summary>
    /// <exception cref="NotSupportedException">Thrown when this provider cannot support the operation at all - see the remarks above.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any of the given nodes cannot be moved as requested, for example because the resulting
    /// location is already occupied, or because a node would end up inside itself. When thrown, none of the
    /// given nodes have been moved. A provider may throw it for other reasons of its own; callers should treat
    /// it as "this request was refused", not as a promise about which case applies.
    /// </exception>
    Task MoveAsync(IReadOnlyCollection<MudExFileStructureNode> nodes, MudExFileStructureNode target, CancellationToken ct = default);

    /// <summary>Deletes all given nodes, directories including their content.</summary>
    /// <exception cref="NotSupportedException">Thrown when this provider cannot support the operation at all - see the remarks above.</exception>
    Task DeleteAsync(IReadOnlyCollection<MudExFileStructureNode> nodes, CancellationToken ct = default);

    /// <summary>Adds a file to the target directory and returns the new node.</summary>
    /// <returns>A new node for the added file. See the remarks on invalidation.</returns>
    /// <exception cref="NotSupportedException">Thrown when this provider cannot support the operation at all - see the remarks above.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the operation cannot be carried out as requested, for example because the target location is
    /// already occupied by an entry of a different kind. Replacing an existing file of the same kind is not
    /// guaranteed to be refused - a provider may treat that as a normal re-upload instead. A provider may throw
    /// it for other reasons of its own; callers should treat it as "this request was refused", not as a promise
    /// about which case applies.
    /// </exception>
    Task<MudExFileStructureNode> UploadAsync(MudExFileStructureNode target, IBrowserFile file, CancellationToken ct = default);
}
