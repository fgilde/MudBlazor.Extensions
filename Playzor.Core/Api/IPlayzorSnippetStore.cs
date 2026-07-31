using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Playzor.Core.Api;

/// <summary>
/// Optional backend for saving, loading and listing snippets. Register one and the editor's save
/// and samples buttons work on their own; leave it out and the host wires those buttons through
/// the editor's events instead.
/// </summary>
public interface IPlayzorSnippetStore
{
    /// <summary>Stores the files and returns the id they can be loaded with.</summary>
    Task<string> SaveAsync(IEnumerable<CodeFile> files, CancellationToken cancellationToken = default);

    /// <summary>Loads a stored snippet.</summary>
    Task<IEnumerable<CodeFile>> LoadAsync(string snippetId, CancellationToken cancellationToken = default);

    /// <summary>Names of the ready made samples, empty when the store has none.</summary>
    Task<IReadOnlyList<string>> GetSampleNamesAsync(CancellationToken cancellationToken = default);

    /// <summary>Loads one of the samples returned by <see cref="GetSampleNamesAsync"/>.</summary>
    Task<IEnumerable<CodeFile>> LoadSampleAsync(string sampleName, CancellationToken cancellationToken = default);
}
