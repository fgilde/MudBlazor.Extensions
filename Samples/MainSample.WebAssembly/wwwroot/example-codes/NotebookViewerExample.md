```razor
@inherits ExampleBase
@using Doc = MudBlazor.Extensions.Office.Notebook.Notebook

<div style="width: 100%; height: 520px;">
    <MudExNotebookEditor @ref="ComponentRef" Notebook="_notebook" ReadOnly="true" />
</div>

@code {
    private readonly Doc _notebook = NotebookSamples.CreateNotebook();
}

```
