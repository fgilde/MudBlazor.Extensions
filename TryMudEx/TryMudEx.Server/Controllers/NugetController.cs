using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Extensions.Api;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Helper;
using static TryMudEx.Server.Utilities.SnippetsEncoder;

namespace TryMudEx.Server.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class NugetController : ControllerBase
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _config;
        private readonly BlobContainerClient containerClient;
        private HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        public NugetController(IConfiguration config, IServiceProvider serviceProvider, IMemoryCache cache, IWebHostEnvironment webHostEnvironment)
        {
            _cache = cache;
            _httpClient = serviceProvider.GetService<HttpClient>() ?? new HttpClient();
        }

        [HttpGet("package/{packageId}/{version}")]
        public async Task<IActionResult> GetSamples(string packageId, string version)
        {
            var packageUrl = $"https://www.nuget.org/api/v2/package/{packageId}/{version}";
            var responseMessage = await _httpClient.GetAsync(packageUrl);

            if (!responseMessage.IsSuccessStatusCode)
            {
                // Handle the error appropriately. For example, you can return a 500 status code
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }

            var stream = await responseMessage.Content.ReadAsStreamAsync();
            var contentDisposition = responseMessage.Content.Headers.ContentDisposition?.ToString();

            // Return the stream from the other server directly to your client
            return File(stream, "application/octet-stream", contentDisposition ?? "package.zip");
        }

    }
}
