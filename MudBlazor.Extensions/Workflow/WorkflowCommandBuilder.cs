public class WorkflowCommandBuilder
{
    private readonly WorkflowCommand _command;

    public WorkflowCommandBuilder(WorkflowCommand command)
    {
        _command = command;
    }

    public WorkflowCommandBuilder SetName(string name)
    {
        _command.Name = name;
        return this;
    }

    public WorkflowCommandBuilder SetBgColor(string color)
    {
        _command.BgColor = color;
        return this;
    }

    public WorkflowCommandBuilder SetFgColor(string color)
    {
        _command.FgColor = color;
        return this;
    }

    public WorkflowCommandBuilder SetOnClick(string javascriptAction)
    {
        _command.OnClick = javascriptAction;
        return this;
    }
}