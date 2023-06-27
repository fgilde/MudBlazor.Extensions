param(
    [Parameter(Mandatory=$true)]
    [string]$SourceFile,

    [Parameter(Mandatory=$true)]
    [string]$Tag,

    [Parameter(Mandatory=$false)]
    [int]$Count = 5,

    [Parameter(Mandatory=$false)]
    [bool]$AddLink = $true,
    
    [Parameter(Mandatory=$false)]
    [string]$SourceTag,

    [Parameter(Mandatory=$false)]
    [bool]$All = $false
)

$linkTo = "";
# Check if the SourceFile is a URL or a local file path
if ($SourceFile -match '^http(s)?://') {
    # SourceFile is a URL, fetch content from the web
    $response = Invoke-WebRequest -Uri $SourceFile
    $source = $response.Content -split "`n"
    $linkTo = $SourceFile
} else {
    # SourceFile is a local file path, read content from the file
    $sourceFilePath = Join-Path ".." (Join-Path "Docs" $SourceFile)
    $source = Get-Content -Path $sourceFilePath
    $linkTo = "https://github.com/fgilde/MudBlazor.Extensions/blob/main/MudBlazor.Extensions/Docs/"+$SourceFile
}


if ($SourceTag -or $All) {
    if ($All) {
		$changes = $source
	} else {
        $startTag = "<!-- "+$SourceTag+":START -->"
        $endTag = "<!-- "+$SourceTag+":END -->"
        $record = $false
        $result = ""

        foreach ($line in $source)
        {
            if ($line.Trim() -eq $endTag)
            {
                $record = $false
            }

            if ($record)
            {
                $result += $line + "`n"
            }

            if ($line.Trim() -eq $startTag)
            {
                $record = $true
            }
        }
        
        $changes = $result
    }
}
 else {
    $changes = $source | Where-Object { $_ -match ">\s" } | Select-Object -First $Count
}


$changes = "<!-- Copied from $SourceFile on $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') -->`r`n$($changes -join "`r`n")"

if($AddLink -eq $true) {
    $changes = $changes + "`r`n[More]("+$linkTo+")"
}

# Read the README
$readme = Get-Content -Path ..\..\README.md

# Find where the CHANGELOG:START and CHANGELOG:END are
$startIndex = $readme.IndexOf("<!-- "+$Tag+":START -->") + 1
$endIndex = $readme.IndexOf("<!-- "+$Tag+":END -->")

# Replace the old changelog with the new one
$updatedReadme = $readme[0..($startIndex-1)] + $changes + $readme[$endIndex..($readme.Length-1)]

# Write the new README back to the file
# $updatedReadme | Out-File -FilePath ..\..\README.md

$updatedReadme | Out-File -FilePath ..\..\README.md -Encoding utf8
#$updatedReadme | Out-File -FilePath ..\..\README.md -Encoding utf8NoBOM

