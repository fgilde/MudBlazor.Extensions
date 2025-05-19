using MainSample.WebAssembly.Types;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;

namespace MainSample.WebAssembly.Services;

public class DeploymentsService(NavigationManager navigationManager)
{
    private readonly NavigationManager _navigationManager = navigationManager;
    private Deployment[]? _deployments = null;
    
    public async Task<Deployment[]> GetAllDeploymentsAsync()
    {
        if (_deployments != null)
        {
            return _deployments;
        }
        var client = new HttpClient { BaseAddress = new Uri(_navigationManager.BaseUri) };
        var json = await client.GetStringAsync("/deployments.json");
        // TODO: github deployments
        _deployments = (JsonConvert.DeserializeObject<Deployment[]>(json) ?? []).Concat([new Deployment()
        {
            Url = "https://fgilde.github.io/MudBlazor.Extensions/",
            Stages = [new Stage { Name = "Deploy", Status = "success" }],
            Environment = "GitHub",
            ProjectName = "mudblazor-extensions",
            CreatedOn = new DateTime(2022, 8, 1),
            ModifiedOn = new DateTime(2022, 8, 1)
        }]).ToArray();
        return _deployments;
    }

    public async Task<Deployment[]> GetLatestSuccessFullDeploymentsPerDayAsync()
    {
        var all = await GetAllDeploymentsAsync();
        var successful = all
            .Where(d => d.Stages.All(s => s.Status == "success"));

        var latestPerDay = successful
            .GroupBy(d => d.CreatedOn.Date)
            .Select(g => g.OrderByDescending(d => d.CreatedOn)
                .First())
            .ToArray();
        return latestPerDay;
    }

}