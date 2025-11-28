```razor
@inherits ExampleBase

<MudGrid>
    <MudItem xs="12" md="6">
        <MudExEnumSelect @ref="ComponentRef" TEnum="FilePermissions" @bind-Value="@Permissions" Label="@L["File Permissions"]" />
    </MudItem>
    <MudItem xs="12" md="6">
        <MudText Typo="Typo.body1" Class="mt-3">@L["Selected"]: <strong>@Permissions</strong></MudText>
        <MudText Typo="Typo.caption" Color="Color.Secondary">
            @L["Numeric value"]: @((int)Permissions)
        </MudText>
    </MudItem>
</MudGrid>

@code {
    public FilePermissions Permissions { get; set; } = FilePermissions.Read | FilePermissions.Write;

    [Flags]
    public enum FilePermissions
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4,
        Delete = 8,
        Share = 16,
        Admin = 32
    }
}

```
