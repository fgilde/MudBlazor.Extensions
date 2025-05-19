param(
    [string]$AccountId     = "b4406e14c3591c5124716c1afe6d11bb",
    [string]$ProjectSlug   = "mudblazor-extensions",
    [string]$ApiToken      = "bRYQm7GE2ckRCglJ1Q5bhJIpQ7tyZF9sAksfZqEp",
    [int]   $PerPage       = 25,
    [string]$OutputFile    = "deployments.json",
    # Path relative to the git repo root for git show
    [string]$CsprojPath    = "MudBlazor.Extensions/MudBlazor.Extensions.csproj"
)

# Store initial working directory
$initialDir = Get-Location

# Try to get the repo root for git operations
try {
    $repoRoot = git rev-parse --show-toplevel 2>$null
} catch {
    Write-Warning "Warning: could not determine Git root; git show may fail if not in a Git repo."
    $repoRoot = $null
}

Write-Host "COLLECTING DEPLOYMENTS..." -ForegroundColor Yellow
$baseUri = "https://api.cloudflare.com/client/v4/accounts/$AccountId/pages/projects/$ProjectSlug/deployments"
Write-Host "Base URI: $baseUri" -ForegroundColor Cyan

$allResults = @(); $page = 1

do {
    try { $uriBuilder = [UriBuilder]$baseUri; $uriBuilder.Query = "page=$page&per_page=$PerPage"; $uri = $uriBuilder.Uri.AbsoluteUri }
    catch { Write-Error "Error constructing URI: $($_.Exception.Message)"; break }

    Write-Host ("- Requesting page {0}: {1}" -f $page, $uri) -ForegroundColor Yellow
    try { $response = Invoke-RestMethod -Method Get -Uri $uri -Headers @{"Authorization" = "Bearer $ApiToken"; "Content-Type" = "application/json"} }
    catch { Write-Error "Error invoking API: $($_.Exception.Message)"; break }

    if (-not $response.success) { Write-Error "API Error: $($response.errors | ConvertTo-Json -Depth 5)"; break }
    $allResults += $response.result; $page++
} while ($page -le $response.result_info.total_pages)

if ($allResults.Count -eq 0) { Write-Warning "No deployments found."; return }

# Cache for version lookups
$versionCache = @{}

function Get-VersionsFromCommit {
    param([string]$CommitHash)
    if ($versionCache.ContainsKey($CommitHash)) { return $versionCache[$CommitHash] }

    # Use repoRoot context for git show
    if ($repoRoot) { Push-Location $repoRoot }
    try { $xmlText = git show "$CommitHash`:$CsprojPath" 2>$null } catch { $xmlText = $null }
    if ($repoRoot) { Pop-Location }

    if (-not $xmlText) {
        Write-Warning ("Commit {0}: '{1}' not found" -f $CommitHash, $CsprojPath)
        $obj = [pscustomobject]@{ AssemblyVersion = $null; MudBlazorVersion = $null }
        $versionCache[$CommitHash] = $obj; return $obj
    }

    [xml]$xml = $xmlText
    # Read version variables
    $versionProps = $xml.Project.PropertyGroup |
        Where-Object { $_.MajorVersion -and $_.MinorVersion -and $_.PatchVersion } |
        Select-Object -First 1
    if ($versionProps) {
        $major = $versionProps.MajorVersion; $minor = $versionProps.MinorVersion; $patch = $versionProps.PatchVersion
    } else {
        Write-Host ("[Info] Commit {0}: No version variables found" -f $CommitHash) -ForegroundColor DarkYellow
        $major = $minor = $patch = $null
    }

    # Read raw version tags
    $assemblyRaw = ($xml.Project.PropertyGroup | Where-Object { $_.AssemblyVersion } | Select-Object -First 1 -ExpandProperty AssemblyVersion)
    $packageRaw  = ($xml.Project.PropertyGroup | Where-Object { $_.PackageVersion }   | Select-Object -First 1 -ExpandProperty PackageVersion)
    $fileRaw     = ($xml.Project.PropertyGroup | Where-Object { $_.FileVersion }      | Select-Object -First 1 -ExpandProperty FileVersion)

    # Determine final AssemblyVersion
    if ($assemblyRaw -and $assemblyRaw -notmatch '\$\(') {
        $assemblyVersion = $assemblyRaw
    } elseif ($packageRaw -and $packageRaw -notmatch '\$\(' -and $packageRaw -ne '0.0.0-dev') {
        Write-Host ("[Info] Commit {0}: using PackageVersion fallback '{1}'" -f $CommitHash, $packageRaw) -ForegroundColor DarkYellow
        $assemblyVersion = $packageRaw
    } elseif ($packageRaw -and $packageRaw -eq '0.0.0-dev') {
        # Try nerdbank version.json
        $versionJsonPath = "$(Split-Path $CsprojPath -Parent)/version.json"
        if ($repoRoot) { Push-Location $repoRoot }
        try { $verText = git show "$CommitHash`:$versionJsonPath" 2>$null } catch { $verText = $null }
        if ($repoRoot) { Pop-Location }
        if ($verText) {
            try {
                $verJson = $verText | ConvertFrom-Json
                $assemblyVersion = $verJson.version
                Write-Host ("[Info] Commit {0}: using nerdbank version.json '{1}'" -f $CommitHash, $assemblyVersion) -ForegroundColor DarkYellow
            } catch {
                Write-Host ("[Warning] Commit {0}: failed to parse version.json" -f $CommitHash) -ForegroundColor DarkYellow
                $assemblyVersion = $null
            }
        } else {
            Write-Host ("[Warning] Commit {0}: version.json not found at {1}" -f $CommitHash, $versionJsonPath) -ForegroundColor DarkYellow
            $assemblyVersion = $null
        }
    } elseif ($fileRaw -and $fileRaw -notmatch '\$\(') {
        Write-Host ("[Info] Commit {0}: using FileVersion fallback '{1}'" -f $CommitHash, $fileRaw) -ForegroundColor DarkYellow
        $assemblyVersion = $fileRaw
    } elseif ($major -and $minor -and $patch) {
        Write-Host ("[Info] Commit {0}: resolving to variables '{1}.{2}.{3}'" -f $CommitHash, $major, $minor, $patch) -ForegroundColor DarkYellow
        $assemblyVersion = "{0}.{1}.{2}" -f $major, $minor, $patch
    } else {
        Write-Host ("[Warning] Commit {0}: could not resolve any version" -f $CommitHash) -ForegroundColor DarkYellow
        $assemblyVersion = $null
    }

    # Read MudBlazor version
    $mudVer = ($xml.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq 'MudBlazor' } | Select-Object -First 1 -ExpandProperty Version)
    if (-not $mudVer) { Write-Host ("[Info] Commit {0}: No MudBlazor PackageReference found" -f $CommitHash) -ForegroundColor DarkYellow }

    $obj = [pscustomobject]@{ AssemblyVersion = $assemblyVersion; MudBlazorVersion = $mudVer }
    $versionCache[$CommitHash] = $obj; return $obj
}

# Augment deployments
$augmented = $allResults | ForEach-Object {
    $hash = $_.deployment_trigger.metadata.commit_hash
    $versions = Get-VersionsFromCommit -CommitHash $hash
    $_ | Add-Member -NotePropertyName AssemblyVersion   -NotePropertyValue $versions.AssemblyVersion   -PassThru |
         Add-Member -NotePropertyName MudBlazorVersion  -NotePropertyValue $versions.MudBlazorVersion  -PassThru
}

# Change back to initial dir and write output
Set-Location $initialDir
$outDir = Split-Path -Path $OutputFile -Parent
if ($outDir -and -not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
$augmented | ConvertTo-Json -Depth 10 | Set-Content -Path $OutputFile
$fullPath = (Resolve-Path -Path $OutputFile | Select-Object -ExpandProperty Path)
Write-Host ("✅ Finished: {0} deployments written to '{1}'" -f $augmented.Count, $fullPath) -ForegroundColor Green
