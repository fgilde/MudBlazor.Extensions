using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using MudBlazor;
using Nextended.Core.Helper;

namespace TryMudEx.Client.Enums;

public enum CodeViewMode
{
    [Icon(Icons.Material.Filled.BorderRight)]
    [Description("Dock preview panel to right side")]
    DockedRight,
    [Icon(Icons.Material.Filled.BorderBottom)]
    [Description("Dock preview panel to bottom")]
    DockedBottom,
    [Icon(Icons.Material.Filled.BorderLeft)]
    [Description("Dock preview panel to left side")]
    DockedLeft,
    [Icon(Icons.Material.Filled.BorderTop)]
    [Description("Dock preview panel to top")]
    DockedTop,
    [Icon(Icons.Material.Filled.DesktopWindows)]
    [Description("Do not dock preview panel and show a dialog after compile")]
    Window,
}

internal class IconAttribute : Attribute
{
    public string IconName { get; }

    public IconAttribute(string iconName)
    {
        IconName = iconName;
    }
}

internal static class EnumExtensions
{
    public static TAttribute GetAttribute<TAttribute>(this Enum e) where TAttribute : Attribute
    {
        TAttribute[] customAttributes = (TAttribute[])e.GetType().GetField(e.ToString()).GetCustomAttributes(typeof(TAttribute), false);
        return customAttributes.FirstOrDefault();
    }
}