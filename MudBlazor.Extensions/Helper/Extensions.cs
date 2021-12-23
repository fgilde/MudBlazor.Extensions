using System;
using System.Linq;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;

namespace MudBlazor.Extensions.Helper
{
    internal static class Extensions
    {
        public static string[] GetPositionNames(this DialogPosition? position)
        {
            return position.HasValue ? Enum<DialogPosition>.GetName(position.Value).SplitByUpperCase().Select(n => n.ToLower()).ToArray() : Array.Empty<string>();
        }
    }
}