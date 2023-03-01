using MainSample.WebAssembly.Pages;
using MainSample.WebAssembly.Shared;
using MudBlazor.Extensions.Components.ObjectEdit.Options;

namespace MainSample.WebAssembly.ObjectEditMetaConfig;

public static class MySimpleTypeRegistrations
{
    public static void RegisterRenderDefaults()
    {
        RenderDataDefaults.RegisterDefault<ICollection<Page_ObjectEditConfigured.ProgramingSkill>, IEnumerable<Page_ObjectEditConfigured.ProgramingSkill>, ProgrammingSkillSelect>(s => s.Selected);
    }
}