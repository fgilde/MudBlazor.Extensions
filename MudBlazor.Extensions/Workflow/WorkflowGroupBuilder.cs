public class WorkflowGroupBuilder
{
    private readonly WorkflowGroup _group;

    public WorkflowGroupBuilder(WorkflowGroup group)
    {
        _group = group;
    }

    public WorkflowGroupBuilder SetName(string name)
    {
        _group.Name = name;
        return this;
    }

    public WorkflowGroupBuilder SetBgColor(string color)
    {
        _group.BgColor = color;
        return this;
    }

    public WorkflowGroupBuilder SetFgColor(string color)
    {
        _group.FgColor = color;
        return this;
    }

    public WorkflowGroupBuilder AddCommand(Action<WorkflowCommandBuilder> configure)
    {
        var command = new WorkflowCommand();
        var commandBuilder = new WorkflowCommandBuilder(command);
        configure(commandBuilder);
        _group.Commands.Add(command);
        return this;
    }

    public WorkflowGroupBuilder ConnectTo(string targetGroupId)
    {
        _group.ConnectionTo = targetGroupId;
        return this;
    }
}