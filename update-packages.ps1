$regex = 'PackageReference Include="([^"]*)" Version="([^"]*)"'

ForEach ($file in get-childitem . -recurse | where {$_.extension -like "*proj"})
{
    $packages = Get-Content $file.FullName |
        select-string -pattern $regex -AllMatches | 
        ForEach-Object {$_.Matches} | 
        ForEach-Object {$_.Groups[1].Value.ToString()}| 
        sort -Unique
    
    ForEach ($package in $packages)
    {
        write-host "Update $file package :$package"  -foreground 'magenta'
        $fullName = $file.FullName
        iex "dotnet add $fullName package $package"
    }
}