using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        public NugetController(IServiceProvider serviceProvider)
        {
            _httpClient = serviceProvider.GetService<HttpClient>() ?? new HttpClient();
        }

        [HttpGet("package/{packageId}/{version}")]
        public async Task<IActionResult> GetPackage(string packageId, string version)
        {
            var packageUrl = $"https://www.nuget.org/api/v2/package/{packageId}/{version}";
            var responseMessage = await _httpClient.GetAsync(packageUrl);

            if (!responseMessage.IsSuccessStatusCode)
            {
                return StatusCode((int)responseMessage.StatusCode);
            }

            var stream = await responseMessage.Content.ReadAsStreamAsync();
            var contentDisposition = responseMessage.Content.Headers.ContentDisposition?.ToString();

            // id+version is immutable on nuget.org — let the browser cache the nupkg (both wasm instances profit)
            Response.Headers.CacheControl = "public, max-age=31536000, immutable";

            return File(stream, "application/octet-stream", contentDisposition ?? "package.zip");
        }

    }
}
