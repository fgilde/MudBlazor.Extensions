```razor
@using MudBlazor.Extensions.Core.Enums
@inherits ExampleBase

<MudExTreeView @bind-SelectedNode="_selectedNode"
               @ref="ComponentRef"
               Style="max-height: 50vh; overflow: auto"
               Virtualize="false"
               ViewMode="TreeViewMode.Default"
               Items="@Entries">
    <ItemContentTemplate>
        <div style="display: flex; align-items: center; gap: 8px;">
            <MudIcon Icon="@GetIconForNode(context.Value)" Size="Size.Small" />
            <span>@context.Value.Name</span>
        </div>
    </ItemContentTemplate>
</MudExTreeView>

@code {
    private SampleTreeItem _selectedNode;
    public HashSet<SampleTreeItem> Entries { get; set; }

    protected override void OnInitialized()
    {
        Entries = new HashSet<SampleTreeItem>
        {
            new SampleTreeItem("Documents")
            {
                Children = new HashSet<SampleTreeItem>
                {
                    new SampleTreeItem("Resume.pdf"),
                    new SampleTreeItem("CoverLetter.docx"),
                    new SampleTreeItem("Reports")
                    {
                        Children = new HashSet<SampleTreeItem>
                        {
                            new SampleTreeItem("Q1_Report.xlsx"),
                            new SampleTreeItem("Q2_Report.xlsx")
                        }
                    }
                }
            },
            new SampleTreeItem("Pictures")
            {
                Children = new HashSet<SampleTreeItem>
                {
                    new SampleTreeItem("Vacation.jpg"),
                    new SampleTreeItem("Family.png"),
                    new SampleTreeItem("Work")
                    {
                        Children = new HashSet<SampleTreeItem>
                        {
                            new SampleTreeItem("Meeting.jpg"),
                            new SampleTreeItem("Presentation.pptx")
                        }
                    }
                }
            },
            new SampleTreeItem("Music")
            {
                Children = new HashSet<SampleTreeItem>
                {
                    new SampleTreeItem("Favorites")
                    {
                        Children = new HashSet<SampleTreeItem>
                        {
                            new SampleTreeItem("Song1.mp3"),
                            new SampleTreeItem("Song2.mp3")
                        }
                    }
                }
            }
        };
    }

    private string GetIconForNode(SampleTreeItem node)
    {
        if (node.Children?.Any() == true)
        {
            return Icons.Material.Filled.Folder;
        }
        
        var name = node.Name?.ToLower() ?? "";
        
        if (name.EndsWith(".pdf"))
            return Icons.Material.Filled.PictureAsPdf;
        if (name.EndsWith(".docx") || name.EndsWith(".doc"))
            return Icons.Material.Filled.Description;
        if (name.EndsWith(".xlsx") || name.EndsWith(".xls"))
            return Icons.Material.Filled.TableChart;
        if (name.EndsWith(".jpg") || name.EndsWith(".png") || name.EndsWith(".gif"))
            return Icons.Material.Filled.Image;
        if (name.EndsWith(".mp3") || name.EndsWith(".wav"))
            return Icons.Material.Filled.AudioFile;
        if (name.EndsWith(".pptx") || name.EndsWith(".ppt"))
            return Icons.Material.Filled.Slideshow;
            
        return Icons.Material.Filled.InsertDriveFile;
    }
}

```
