namespace Try.Core
{
    public static class CoreConstants
    {
        public const string MainComponentFilePath = "__Main.razor";

        public const string MainComponentDefaultFileContent = """
                                                              @using MudBlazor.Extensions;
                                                              @using MudBlazor.Extensions.Options;
                                                              @using MudBlazor.Extensions.Core;
                                                              @using MudBlazor.Utilities;
                                                              
                                                              @inject IJSRuntime Js
                                                              @inject IDialogService dialogService;
                                                              
                                                              <MudExGradientText Typo="Typo.h6">Edit object in Dialog</MudExGradientText>
                                                              <MudButton OnClick="@ShowSampleDialog" Variant="Variant.Filled" Color="Color.Primary">Edit Test Object</MudButton>
                                                              
                                                              @code {
                                                              
                                                                  public class TestObject
                                                                  {
                                                                      [Required]
                                                                      public string Value { get; set; } = "Test";
                                                                      [Range(1, 100)]
                                                                      public int Number { get; set; }        
                                                                  }
                                                              
                                                              
                                                                  private async Task ShowSampleDialog()
                                                                  {
                                                                      DialogOptionsEx dlgOptions = new()
                                                                      {
                                                                          MaximizeButton = true,
                                                                          CloseButton = true,
                                                                          FullHeight = true,
                                                                          CloseOnEscapeKey = true,
                                                                          MaxWidth = MaxWidth.Medium,
                                                                          FullWidth = true,
                                                                          DragMode = MudDialogDragMode.Simple,
                                                                          Animations = new[] { AnimationType.SlideIn },
                                                                          Position = DialogPosition.CenterRight,
                                                                          DisableSizeMarginY = true,
                                                                          DisablePositionMargin = true
                                                                      };
                                                                      var res = await dialogService.EditObject(new TestObject(), "Auto Generated Editor for TestObject", OnSubmit, dlgOptions, meta => meta.WrapEachInMudItem(i => i.xs = 6));
                                                                      if (!res.Cancelled)
                                                                          await Js.InvokeVoidAsync("alert", "Save");
                                                                  }
                                                                  
                                                              
                                                                  private async Task<string> OnSubmit(TestObject value, MudExObjectEditDialog<TestObject> dialog)
                                                                  {
                                                                      await Task.Delay(2000); // Simulate server save
                                                                      if (value.Value == "Test")
                                                                          return "'Test' is as Value not allowed or already exists";
                                                                      if (value.Value == "Exception")
                                                                          throw new Exception("This is a test exception");
                                                                      return null;
                                                                  }
                                                              
                                                              }
                                                              """;


        public const string DefaultUserComponentsAssemblyBytes =
            "TVqQAAMAAAAEAAAA//8AALgAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAAAA4fug4AtAnNIbgBTM0hVGhpcyBwcm9ncmFtIGNhbm5vdCBiZSBydW4gaW4gRE9TIG1vZGUuDQ0KJAAAAAAAAABQRQAATAEDAJiVwb4AAAAAAAAAAOAAIiALATAAAAoAAAAGAAAAAAAAEigAAAAgAAAAQAAAAAAAEAAgAAAAAgAABAAAAAAAAAAEAAAAAAAAAACAAAAAAgAAAAAAAAMAYIUAABAAABAAAAAAEAAAEAAAAAAAABAAAAAAAAAAAAAAAL4nAABPAAAAAEAAAIADAAAAAAAAAAAAAAAAAAAAAAAAAGAAAAwAAADEJgAAVAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIAAACAAAAAAAAAAAAAAACCAAAEgAAAAAAAAAAAAAAC50ZXh0AAAAGAgAAAAgAAAACgAAAAIAAAAAAAAAAAAAAAAAACAAAGAucnNyYwAAAIADAAAAQAAAAAQAAAAMAAAAAAAAAAAAAAAAAABAAABALnJlbG9jAAAMAAAAAGAAAAACAAAAEAAAAAAAAAAAAAAAAAAAQAAAQgAAAAAAAAAAAAAAAAAAAADyJwAAAAAAAEgAAAACAAUAdCAAAFAGAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAB4CKA0AAAoqOgIoDQAACgIDfQEAAAQqBioeAigOAAAKKgAAAEJTSkIBAAEAAAAAAAwAAAB2NC4wLjMwMzE5AAAAAAUAbAAAACACAAAjfgAAjAIAAMgCAAAjU3RyaW5ncwAAAABUBQAABAAAACNVUwBYBQAAEAAAACNHVUlEAAAAaAUAAOgAAAAjQmxvYgAAAAAAAAACAAABVxUAAAkAAAAA+gEzABYAAAEAAAARAAAABAAAAAEAAAAEAAAAAQAAAA4AAAAQAAAAAQAAAAIAAAAAAMoBAQAAAAAABgAwAT4CBgCCAT4CBgB8ACsCDwBeAgAABgBpAfcBBgD5APcBBgC2APcBBgDTAPcBBgBQAfcBBgCQAPcBBgBJAD4CBgCWAeEBBgCEAuEBBgBkAOEBCgCnAJUCCgApAJUCCgAJAqABAAAAAAEAAAAAAAEAAQAAARAANwBtAjEAAQABAAABEAAYAT4CMQABAAIAAQAQAOgBtQJBAAIAAwAmAO8BLQBQIAAAAACGGCUCBgABAFggAAAAAIYYJQIBAAEAZyAAAAAAxAAKADAAAQBpIAAAAACGGCUCBgACAAAAAQAbAgkAJQIBABEAJQIGABkAJQIKACkAJQIQADEAJQIQADkAJQIQAEEAJQIQAEkAJQIQAFEAJQIQAFkAJQIGAHEAJQIVAHkAJQIQAGEAJQIGAIEAJQIGACcAEgDdAC4ACwA2AC4AEwA/AC4AGwBeAC4AIwBnAC4AKwB/AC4AMwCMAC4AOwCZAC4AQwBnAC4ASwBnAEMAUwCkAEMACgCkAGMAUwCkAGMACgCkAGMAWwCpAIMAYwDQAASAAAABAAAAAAAAAAAAAAAAALUCAAAHAAAAAAAAAAAAAAAbABoAAAAAAAcAAAAAAAAAAAAAACQAlQIAAAAAAAAAPE1vZHVsZT4AQnVpbGRSZW5kZXJUcmVlAFN5c3RlbS5SdW50aW1lAENvbXBvbmVudEJhc2UARW1iZWRkZWRBdHRyaWJ1dGUAQ29tcGlsZXJHZW5lcmF0ZWRBdHRyaWJ1dGUAQXR0cmlidXRlVXNhZ2VBdHRyaWJ1dGUARGVidWdnYWJsZUF0dHJpYnV0ZQBBc3NlbWJseVRpdGxlQXR0cmlidXRlAFJvdXRlQXR0cmlidXRlAEFzc2VtYmx5RmlsZVZlcnNpb25BdHRyaWJ1dGUAQXNzZW1ibHlJbmZvcm1hdGlvbmFsVmVyc2lvbkF0dHJpYnV0ZQBBc3NlbWJseUNvbmZpZ3VyYXRpb25BdHRyaWJ1dGUAUmVmU2FmZXR5UnVsZXNBdHRyaWJ1dGUAQ29tcGlsYXRpb25SZWxheGF0aW9uc0F0dHJpYnV0ZQBBc3NlbWJseVByb2R1Y3RBdHRyaWJ1dGUAQXNzZW1ibHlDb21wYW55QXR0cmlidXRlAFJ1bnRpbWVDb21wYXRpYmlsaXR5QXR0cmlidXRlAE1pY3Jvc29mdC5Bc3BOZXRDb3JlLkNvbXBvbmVudHMuUmVuZGVyaW5nAFRyeS5Vc2VyQ29tcG9uZW50cy5kbGwAU3lzdGVtAF9fTWFpbgBWZXJzaW9uAFN5c3RlbS5SZWZsZWN0aW9u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";

        public const string DefaultRazorFileContentFormat = "<h1>{0}</h1>";

        public static readonly string DefaultCSharpFileContentFormat =
            @$"namespace {CompilationService.DefaultRootNamespace}
{{{{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class {{0}}
    {{{{
    }}}}
}}}}
";
    }
}
