using MudBlazor.Extensions.Components.ObjectEdit.Options;
using SampleApplication.Client.Pages;
using SampleApplication.Client.Shared;

namespace SampleApplication.Client.ObjectEditMetaConfig;

public static class MySimpleTypeRegistrations
{
    public static void RegisterRenderDefaults()
    {
        RenderDataDefaults.RegisterDefault<ICollection<Page_ObjectEditConfigured.ProgramingSkill>, IEnumerable<Page_ObjectEditConfigured.ProgramingSkill>, ProgrammingSkillSelect>(s => s.Selected);
    }
}