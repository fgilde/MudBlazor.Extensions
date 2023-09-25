using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using static TryMudEx.Server.Utilities.SnippetsEncoder;

namespace TryMudEx.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SnippetsController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly BlobContainerClient containerClient;
        public SnippetsController(IConfiguration config)
        {
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
        }

        [HttpGet("{snippetId}")]
        public async Task<IActionResult> Get(string snippetId)
        {
            snippetId = DecodeSnippetId(snippetId);
            var blob = containerClient.GetBlobClient(BlobPath(snippetId));
            var response = await blob.DownloadAsync();
            var zipStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(zipStream);
            zipStream.Position = 0;
            return File(zipStream, "application/octet-stream", "snippet.zip");
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            var newSnippetId = NewSnippetId();
            await containerClient.UploadBlobAsync(BlobPath(newSnippetId), Request.Body);
            return Ok(EncodeSnippetId(newSnippetId));
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
