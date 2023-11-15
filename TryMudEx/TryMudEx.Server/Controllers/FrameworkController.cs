using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MudBlazor.Examples.Data;
using System;
using System.Collections.Generic;
using System.IO;

namespace TryMudEx.Server.Controllers;

[Route("dynamic-dlls")]
[ApiController]
public class DynamicAssemblyController : ControllerBase
{
    private readonly string _dynamicAssembliesPath;

    public DynamicAssemblyController(IConfiguration configuration)
    {
        _dynamicAssembliesPath = configuration.GetValue<string>("DynamicAssembliesPath");
    }

    [HttpGet("{dll}")]
    public IActionResult Get(string dll)
    {
        var filePath = Path.Combine(_dynamicAssembliesPath, dll + ".dll");

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        return File(fileBytes, "application/octet-stream", dll + ".dll");
    }
}
