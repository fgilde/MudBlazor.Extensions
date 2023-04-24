using MainSample.WebAssembly.Types;
using MudBlazor;

namespace MainSample.WebAssembly;

public static class Navigations
{
    // MudPopoverExtensions
    // MudExCss
    // ;udExSvg
    public static HashSet<NavigationEntry> Default() => new() {
        new NavigationEntry("Home", Icons.Material.Outlined.Home, "/"),
        new NavigationEntry("Dialog Samples")
            {
                Children = new()
                {
                    new NavigationEntry("Dialog Sample", Icons.Material.Outlined.DesktopWindows, "/dialogs"),
                    new NavigationEntry("Component in Dialog", Icons.Material.Outlined.DesktopWindows, "/component-in-dialog"),
                    new NavigationEntry("Simple Dialogs", Icons.Material.Outlined.DesktopWindows, "/simple-dialogs")
                }
            },
            new NavigationEntry("File Handling")
            {
                Children = new()
                {
                    new NavigationEntry("File Display", Icons.Material.Outlined.FolderZip, "/file-display"),
                    new NavigationEntry("Explicit File Display Zip", Icons.Material.Outlined.FolderZip, "/file-display-zip"),
                    new NavigationEntry("Upload Edit", Icons.Material.Outlined.Upload, "/upload-edit"),
                }
            },
            new NavigationEntry("MudExObjectEdit")
            {
                Children = new()
                {
                    new NavigationEntry("With default configuration", Icons.Material.Outlined.Edit, "/object-edit"),
                    new NavigationEntry("With custom configuration", Icons.Material.Outlined.TextSnippet, "/mudex-object-edit-with-configuration"),
                    new NavigationEntry("Rendered Virtualized", Icons.Material.Outlined.DataObject, "/virtualized-object-edit"),
                    new NavigationEntry("Edit with shared MetaConfig", Icons.Material.Outlined.Person, "/shared-config"),
                    new NavigationEntry("Conditional updates", Icons.Material.Outlined.DataArray, "/condition-object-edit"),
                    new NavigationEntry("Object Edit in Dialog", Icons.Material.Outlined.DesktopWindows, "/dialog-object-edit"),
                    new NavigationEntry("Edit Running Component", Icons.Material.Outlined.SettingsInputComponent, "/edit-component"),
                    new NavigationEntry("Edit Current Theme", Icons.Material.Outlined.Palette, "/theme-edit")
                }
            },
            new NavigationEntry("Other components")
            {
                Children = new()
                {
                    new NavigationEntry("MudExEnumSelect", Icons.Material.Outlined.List, "/enum-select"),
                    new NavigationEntry("MudExChipSelect", Icons.Material.Outlined.BubbleChart, "/chip-select"),
                    new NavigationEntry("MudExColorBubble", Icons.Material.Outlined.ColorLens, "/color-bubble"),
                    new NavigationEntry("MudExColorPicker", Icons.Material.Outlined.Colorize, "/mudexcolor-picker"),
                    new NavigationEntry("MudExSplitter", Icons.Material.Outlined.HorizontalSplit, "/splitter"),
                    new NavigationEntry("MudExCardList", Icons.Material.Outlined.HorizontalSplit, "/card"),

                }
            },

            //new NavigationEntry("Demos")
            //{
            //    Children = DemoAttribute.AllEntries()
            //},

            new NavigationEntry("Dynamic component tests")
            {
                Children = new()
                {
                    new NavigationEntry("MudEx Components", null) {Children = ReflectMudExComponents()},
                    new NavigationEntry("MudBlazor Components", null) {Children = ReflectMudBlazorComponents()},
                }
            }
        };

    private static HashSet<NavigationEntry> ReflectMudExComponents()
    {
        return ComponentTypes.AllMudExComponents().Select(type => new NavigationEntry($"{type.Name}", "", $"/c/{type.Name}")).ToHashSet();
    }

    private static HashSet<NavigationEntry> ReflectMudBlazorComponents()
    {
        return ComponentTypes.AllMudBlazorComponents().Select(type => new NavigationEntry($"{type.Name}", "", $"/c/{type.Name}")).ToHashSet();
    }

}