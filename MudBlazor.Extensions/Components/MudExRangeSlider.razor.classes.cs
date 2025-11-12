using MudBlazor.Extensions.Helper;
using Nextended.Core.Contracts;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Components;

internal enum DragMode { None, StartThumb, EndThumb, WholeRange }

internal enum Thumb { Start, End }

//TODO: check BoundingClientRect
internal record struct DomRect(double Left, double Top, double Width, double Height);
