namespace TryMudEx.Client.Models
{
    public static class Try
    {
        public static string Initialize = "Try.initialize";
        public static string UpdateSplitView = "Try.updateSplitView";
        public static string ChangeDisplayUrl = "Try.changeDisplayUrl";
        public static string ReloadIframe = "Try.reloadIframe";
        public static string Dispose = "Try.dispose";
        public static class Editor
        {
            public static string Create = "Try.Editor.create";
            public static string GetValue = "Try.Editor.getValue";
            public static string SetValue = "Try.Editor.setValue";
            public static string SetReadOnly = "Try.Editor.setReadOnly";
            public static string SetLangugage = "Try.Editor.setLanguage";
            public static string Focus = "Try.Editor.focus";
            public static string SetTheme = "Try.Editor.setTheme";
            public static string Dispose = "Try.Editor.dispose";
            public static string SetSelection = "Try.Editor.setSelection";
            public static string SetPosition = "Try.Editor.setPosition";
        }

        public static class CodeExecution
        {
            public static string UpdateUserComponentsDLL = "Try.CodeExecution.updateUserComponentsDll";
        }
    }
}
