
namespace MudBlazor.Extensions.Helper;

public static partial class MudExCss
{
    public abstract partial class Classes
    {
        /// <summary>
        /// Css classes for dialogs
        /// </summary>
        public static class Dialog
        {
            [Obsolete("Use Initial instead. Will be removed in future versions.")]
            public static Classes _Initial => Initial;

            /// <summary>
            /// The mud ex initial class should always applied to dialogs if you use dialogServiceExt
            /// </summary>
            public static readonly Classes Initial = new CssClasses(true, "dialog-initial");
            
            /// <summary>
            /// Glass dialog
            /// </summary>
            public static readonly Classes Glass = new CssClasses(true, "glass-dialog");
            
            /// <summary>
            /// Class for color full glass dialog
            /// </summary>
            public static readonly Classes ColorfullGlass = new CssClasses(Glass, Backgrounds.LightBulb);
            
            /// <summary>
            /// Class for sticky dialog actions
            /// </summary>
            public static readonly Classes DialogActionsSticky = new CssClasses(true, "dialog-actions-sticky");
            
            /// <summary>
            /// Class for absolute dialog actions
            /// </summary>
            public static readonly Classes DialogActionsAbsolute = new CssClasses(true, "dialog-actions-absolute");

            
            /// <summary>
            /// Class for full height content
            /// </summary>
            public static readonly Classes FullHeightContent = new CssClasses(true, "dialog-content-full-height");

            /// <summary>
            /// Dialog class for object edit dialogs
            /// </summary>
            public static readonly Classes ObjectEdit = new CssClasses(true, "object-edit-dialog");
            
            public static readonly Classes ObjectEditForm = new CssClasses($"{ObjectEdit}-form");

            /// <summary>
            /// Full height with margin
            /// </summary>
            public static readonly Classes FullHeightWithMargin = new CssClasses(true, "dialog-height-full");

            /// <summary>
            /// Full height without margin
            /// </summary>
            public static readonly Classes FullHeightWithoutMargin = new CssClasses(true, "dialog-height-full-no-margin");

            /// <summary>
            /// No margin dialog
            /// </summary>
            internal static readonly Classes PositionFixedNoMargin = new CssClasses(true, "dialog-position-fixed", "dialog-no-margin");
        }
    }
}