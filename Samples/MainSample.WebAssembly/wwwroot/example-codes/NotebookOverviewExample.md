```razor
@inherits ExampleBase
@using Doc = MudBlazor.Extensions.Office.Notebook.Notebook

<div style="width: 100%; height: 640px;">
    <MudExNotebookEditor @ref="ComponentRef" @bind-Notebook="_notebook" />
</div>

@code {
    private Doc _notebook = NotebookSamples.CreateNotebook();
}

```
