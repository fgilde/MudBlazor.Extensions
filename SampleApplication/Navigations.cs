using MudBlazor;
using SampleApplication.Client.Types;

namespace SampleApplication.Client;

public static class Navigations
{
    public static HashSet<NavigationEntry> Default() => new() {
        new NavigationEntry("Home", Icons.Material.Outlined.Home, "/"),
        new NavigationEntry("Dialog Samples")
            {
                Children = new()
                {
                    new NavigationEntry("Dialog Sample", Icons.Material.Outlined.Window, "/dialogs")
                }
            },
            new NavigationEntry("File Display")
            {
                Children = new()
                {
                    new NavigationEntry("File Display zip", Icons.Material.Outlined.FolderZip, "/file-display"),
                }
            },
            new NavigationEntry("MudExObjectEdit")
            {
                Children = new()
                {
                    new NavigationEntry("With default configuration", Icons.Material.Outlined.DataObject, "/object-edit"),
                    new NavigationEntry("With custom configuration", Icons.Material.Outlined.DataObject, "/mudex-object-edit-with-configuration"),
                    new NavigationEntry("Edit Running Component", Icons.Material.Outlined.SettingsInputComponent, "/edit-component"),
                    new NavigationEntry("Edit Current Theme", Icons.Material.Outlined.Palette, "/theme-edit")
                    //new NavigationEntry("More")
                    //{
                    //    Children = new()
                    //    {
                    //        new NavigationEntry("Edit Files", Icons.Material.Outlined.Language, "/"),
                    //    }
                    //}
                }
            }
        };
}