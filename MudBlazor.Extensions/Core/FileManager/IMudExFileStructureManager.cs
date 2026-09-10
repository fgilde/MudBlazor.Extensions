namespace MudBlazor.Extensions.Core.FileManager;

/// <summary>
/// Provides a file structure to browse. This is the only contract a provider must implement.
/// </summary>
/// <remarks>
/// A provider that loads on demand sets <c>LoadChildrenFunc</c> on every directory node it returns, pointing
/// at <see cref="GetChildrenAsync"/>. A provider that already knows the whole tree returns it from
/// <see cref="GetRootAsync"/> and sets no func. Both are then the same code path for every consumer.
/// </remarks>
public interface IMudExFileStructureManager
{
    /// <summary>What this provider is allowed to do right now.</summary>
    MudExFileManagerCapabilities Capabilities { get; }

    /// <summary>The top level entries.</summary>
    Task<HashSet<MudExFileStructureNode>> GetRootAsync(CancellationToken ct = default);

    /// <summary>The direct children of a directory node.</summary>
    Task<HashSet<MudExFileStructureNode>> GetChildrenAsync(MudExFileStructureNode node, CancellationToken ct = default);

    /// <summary>Opens the content of a file node. The caller owns and disposes the stream.</summary>
    /// <remarks>
    /// An empty stream means the file is genuinely empty. A node that cannot be resolved to content is never
    /// answered with an empty stream - it throws - so "this is gone" can never be mistaken for "this is an
    /// empty file". A node is unresolvable when it is null, when it is a directory, when the provider does not
    /// know it, or when it belongs to a state the provider has since replaced, for example a folder the user
    /// picked before picking another one.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the given node cannot be resolved to readable content. A provider that reads through a
    /// transport of its own may surface that transport's own failure instead - the http provider lets the
    /// server's 404 come out as an <c>HttpRequestException</c> - so callers handle "could not be read" rather
    /// than one exact exception type.
    /// </exception>
    Task<Stream> OpenReadAsync(MudExFileStructureNode node, CancellationToken ct = default);
}
