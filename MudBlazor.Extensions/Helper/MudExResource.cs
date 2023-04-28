using Microsoft.Extensions.FileProviders;
using System.Reflection;
using System.Text;
using System.Xml;

namespace MudBlazor.Extensions.Helper;

public class MudExResource
{
    public static async Task<string> GetDocumentationAsync(Type type)
    {
        if (type == null)
            return null;
        try
        {
            Assembly assembly = Assembly.GetAssembly(type);
            var resourceName = $"{assembly.GetName().Name}.xml";
        
            var embeddedProvider = new EmbeddedFileProvider(assembly);
            var fileInfo = embeddedProvider.GetFileInfo($"wwwroot/docs/{resourceName}");
            await using var resourceStream = fileInfo.CreateReadStream();

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(resourceStream);
            var typeName = type.FullName;
            var xpath = $"//member[@name='T:{typeName}']/summary";
            var summaryNode = xmlDoc.SelectSingleNode(xpath);

            return summaryNode?.InnerText.Trim();
        }
        catch
        {
            return null;
        }
    }

    internal static async Task<string> GetEmbeddedFileContentAsync(string file, Assembly assembly = null)
    {
        var embeddedProvider = new EmbeddedFileProvider(assembly ?? Assembly.GetExecutingAssembly());
        var fileInfo = embeddedProvider.GetFileInfo(file);
        await using var stream = fileInfo.CreateReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}