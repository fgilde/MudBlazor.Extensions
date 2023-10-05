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
    public class SnippetsController : ControllerBase
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _config;
        private readonly BlobContainerClient containerClient;
        private HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        public SnippetsController(IConfiguration config, IServiceProvider serviceProvider, IMemoryCache cache, IWebHostEnvironment webHostEnvironment)
        {
            _cache = cache;
            _webHostEnvironment = webHostEnvironment;
            _config = config;
            var containerUri = new Uri(_config["SnippetsContainerUrl"]);
            string accessKey = _config["SnippetsAccessKey"];
            if (accessKey == "secret")
            {
                var defaultAzureCredentialOptions = new DefaultAzureCredentialOptions();
                defaultAzureCredentialOptions.ManagedIdentityClientId = _config["ManagedCredentialsId"];
                containerClient = new BlobContainerClient(containerUri,
                    new DefaultAzureCredential(defaultAzureCredentialOptions));
            }
            else
            {
                var blobUri = new BlobUriBuilder(containerUri);
                var acccountName = blobUri.AccountName;
                var key = new StorageSharedKeyCredential(acccountName, accessKey);
                containerClient = new BlobContainerClient(containerUri, key);
            }
            _httpClient = serviceProvider.GetService<HttpClient>() ?? new HttpClient();
        }

        [HttpGet("samples")]
        public IActionResult GetSamples()
        {
            var dataPath = Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot", "data");
            if (!Directory.Exists(dataPath))
                dataPath = Path.Combine(_webHostEnvironment.ContentRootPath, "data");
            if (!Directory.Exists(dataPath))
                dataPath = Path.Combine(_webHostEnvironment.ContentRootPath, "../", "TryMudEx.Client", "wwwroot", "data");
            if (Directory.Exists(dataPath))
            {
                var files = Directory.GetFiles(dataPath, "*.zip");
                return Ok(files.Select(f => f.Replace("""\""", "/")).ToArray());
            }

            return Ok(Array.Empty<string>());
        }

        //[HttpGet("{snippetId}")]
        //public async Task<IActionResult> Get(string snippetId)
        //{
        //    snippetId = DecodeSnippetId(snippetId);
        //    var blob = containerClient.GetBlobClient(BlobPath(snippetId));
        //    var response = await blob.DownloadAsync();
        //    var zipStream = new MemoryStream();
        //    await response.Value.Content.CopyToAsync(zipStream);
        //    zipStream.Position = 0;
        //    return File(zipStream, "application/octet-stream", "snippet.zip");
        //}

        //[HttpPost]
        //public async Task<IActionResult> Post()
        //{
        //    var newSnippetId = NewSnippetId();
        //    await containerClient.UploadBlobAsync(BlobPath(newSnippetId), Request.Body);
        //    return Ok(EncodeSnippetId(newSnippetId));
        //}

        [HttpGet("{snippetId}")]
        public async Task<IActionResult> Get(string snippetId)
        {
            var responseMessage = await _httpClient.GetAsync($"https://try.mudblazor.com/api/snippets/{snippetId}");

            if (!responseMessage.IsSuccessStatusCode)
            {
                // Handle the error appropriately. For example, you can return a 500 status code
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }

            var stream = await responseMessage.Content.ReadAsStreamAsync();
            var contentDisposition = responseMessage.Content.Headers.ContentDisposition?.ToString();

            // Return the stream from the other server directly to your client
            return File(stream, "application/octet-stream", contentDisposition ?? "snippet.zip");
        }


        [HttpPost]
        public async Task<IActionResult> Post()
        {
            using var requestContent = new StreamContent(Request.Body);
            foreach (var header in Request.Headers)
            {
                requestContent.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }

            var responseMessage = await _httpClient.PostAsync("https://try.mudblazor.com/api/snippets", requestContent);

            if (!responseMessage.IsSuccessStatusCode)
            {
                // Handle error
                return StatusCode((int)responseMessage.StatusCode, "Error forwarding the POST request");
            }

            var snippetId = await responseMessage.Content.ReadAsStringAsync();
            return Ok(snippetId);
        }


        [HttpGet("mudex.json")]
        public async Task<IActionResult> GetTemplateSnippets()
        {
            return Ok(await _cache.GetOrCreateAsync("mudexSnippets", async entry =>
            {
                BindingFlags _flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

                var interfaceType = typeof(IMudExComponent);
                var possibleTypes = interfaceType.Assembly.GetTypes().Where(
                    t => t.ImplementsInterface(interfaceType) && !t.IsInterface && !t.IsAbstract)
                    .Concat(typeof(MudButton).Assembly.GetTypes().Where(t => !t.Name.StartsWith("_") && t.IsAssignableTo(typeof(ComponentBase)) && t is { IsInterface: false, IsAbstract: false, IsGenericType: false }))
                    .ToList();

                var tasks = possibleTypes.Select(type => ProcessTypeAsync(type, _flags));

                var results = await Task.WhenAll(tasks);
                return results.SelectMany(r => r).ToList();
            }));
        }

        private Task<List<object>> ProcessTypeAsync(Type type, BindingFlags _flags)
        {
            return Task.Run(() =>
            {
                var snippets = new List<object>();
                var placeholderCounter = 1;

                var properties = type.GetProperties(_flags)
                    .Where(i => i.CanWrite && char.IsUpper(i.Name[0]))
                    .OrderBy(x => x.Name)
                    .Select(i => new ApiMemberInfo<PropertyInfo>(i, type))
                    .ToList();

                var attributesList = new List<string>();
                bool hasRenderFragment = false;

                foreach (var property in properties.Where(property => property.Default != "Unknown"))
                {
                    if (property.TypeName == "RenderFragment")
                    {
                        hasRenderFragment = true;
                        continue;
                    }

                    if (property.TypeName.Contains("MudExSize<") || property.TypeName.Contains("EventCallback"))
                    {
                        continue;
                    }

                    if (property.MemberInfo.PropertyType.IsEnum)
                    {
                        var enumType = property.MemberInfo.PropertyType;
                        var enumValues = Enum.GetValues(enumType)
                            .Cast<Enum>()
                            .Select(e => $"{enumType.Namespace}.{enumType.Name}.{e}")
                            .ToArray();
                        if (enumValues?.Length > 0)
                        {
                            var joinedEnumValues = string.Join(',', enumValues);
                            attributesList.Add($"{property.Name}=\"${{{placeholderCounter++}|{joinedEnumValues}|}}\"");
                        }
                    }
                    else if (property.TypeName is "MudColor" or "MudExColor")
                    {
                        attributesList.Add($"{property.Name}=\"@(new {property.TypeName}(\"${{{placeholderCounter++}:{property.Default}}}\"))\"");
                    }
                    else
                    {
                        var defaultValue = property.Default.ToLower() == "true" || property.Default.ToLower() == "false"
                                           ? property.Default.ToLower()
                                           : property.Default;
                        attributesList.Add($"{property.Name}=\"${{{placeholderCounter++}:{defaultValue}}}\"");
                    }
                }

                string componentStructure;
                componentStructure = hasRenderFragment 
                    ? $"<{type.Name} {string.Join(' ', attributesList)}>${{{placeholderCounter++}:Text}}</{type.Name}>" 
                    : $"<{type.Name} {string.Join(' ', attributesList)}/>";

                var snippet = new
                {
                    prefix = type.Name,
                    description = "Default component",
                    body = new List<string> { componentStructure }
                };

                snippets.Add(snippet);
                return snippets;
            });
        }


        private static string NewSnippetId()
        {
            var yearFolder = DateTime.Now.Year;
            var monthFolder = DateTime.Now.Month;
            var dayFolder = DateTime.Now.Day;
            var time = Convert.ToInt32(DateTime.Now.TimeOfDay.TotalMilliseconds);
            var snippetTime = $"{time:D8}";
            return $"{yearFolder:0000}{monthFolder:00}{dayFolder:00}{snippetTime:D8}";
        }

        private static string BlobPath(string snippetId)
        {
            var yearFolder = snippetId.Substring(0, 4);
            var monthFolder = snippetId.Substring(4, 2);
            var dayFolder = snippetId.Substring(6, 2);
            var time = snippetId.Substring(8);
            var snippetFolder = $"{yearFolder:0000}/{monthFolder:00}/{dayFolder:00}";
            var snippetTime = $"{time:00000000}";
            return $"{snippetFolder}/{snippetTime}";
        }
    }
}
