public class WorkflowPageBuilder
{
    private readonly WorkflowPage _page;

    public WorkflowPageBuilder(WorkflowPage page)
    {
        _page = page;
    }

    public WorkflowPageBuilder SetText(string title)
    {
        _page.Title = title;
        return this;
    }

    public WorkflowPageBuilder AddGroup(Action<WorkflowGroupBuilder> configure)
    {
        var group = new WorkflowGroup();
        var groupBuilder = new WorkflowGroupBuilder(group);
        configure(groupBuilder);
        _page.Groups.Add(group);
        return this;
    }
}