public class WorkflowPage
{
    public string Title { get; set; }
    public List<WorkflowGroup> Groups { get; } = new();
}

public class WorkflowGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string BgColor { get; set; } = "#d9e1f2"; // Default light blue
    public string FgColor { get; set; } = "#000000"; // Default black
    public string ConnectionTo { get; set; }
    public List<WorkflowCommand> Commands { get; } = new();
}

public class WorkflowCommand
{
    public string Name { get; set; }
    public string BgColor { get; set; } = "#ffffff"; // Default white
    public string FgColor { get; set; } = "#000000"; // Default black
    public string OnClick { get; set; } // JavaScript action
}