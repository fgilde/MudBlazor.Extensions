namespace Try.Core;

public interface INugetPackageReference
{
    public string Id { get; }
    public string Version { get; }
}

public class NugetResponse
{
    public Context Context { get; set; }
    public int TotalHits { get; set; }
    public NugetPackage[] Data { get; set; }
}

public class Context
{
    public string Vocab { get; set; }
    public string _base { get; set; }
}

public class NugetPackage: INugetPackageReference
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Registration { get; set; }
    public string Version { get; set; }
    public string Description { get; set; }
    public string Summary { get; set; }
    public string Title { get; set; }
    public string IconUrl { get; set; }
    public string LicenseUrl { get; set; }
    public string ProjectUrl { get; set; }
    public string[] Tags { get; set; }
    public string[] Authors { get; set; }
    public string[] Owners { get; set; }
    public long? TotalDownloads { get; set; }
    public bool Verified { get; set; }
    public PackageType[] PackageTypes { get; set; }
    public PackageVersion[] Versions { get; set; }
    public Deprecation Deprecation { get; set; }
}

public class Deprecation
{
    public string Message { get; set; }
    public string[] Reasons { get; set; }
}

public class PackageType
{
    public string Name { get; set; }
}

public class PackageVersion
{
    public string Version { get; set; }
    public int Downloads { get; set; }
    public string Id { get; set; }
}
