param(
    [string]$AccountId   = "b4406e14c3591c5124716c1afe6d11bb",
    [string]$ProjectSlug = "mudblazor-extensions",
    [string]$ApiToken    = "bRYQm7GE2ckRCglJ1Q5bhJIpQ7tyZF9sAksfZqEp",
    [int]   $PerPage     = 25,
    [string]$OutputFile  = "deployments.json"
)

Write-Host "COLLECT DEPLOYMENTS" -ForegroundColor Yellow

$baseUri = "https://api.cloudflare.com/client/v4/accounts/$AccountId/pages/projects/$ProjectSlug/deployments"
Write-Host "Base URI: $baseUri" -ForegroundColor Cyan

$allResults = @()
$page       = 1

do {    
    try {
        $uriBuilder      = [UriBuilder]$baseUri
        $uriBuilder.Query = "page=$page&per_page=$PerPage"
        $uri              = $uriBuilder.Uri.AbsoluteUri
    }
    catch {
        Write-Error "Error constructing URI: $($_.Exception.Message)"
        break
    }

    Write-Host "- Call page $page : $uri" -ForegroundColor Yellow

    # 3) API-Call
    try {
        $response = Invoke-RestMethod -Method Get -Uri $uri -Headers @{
            "Authorization" = "Bearer $ApiToken"
            "Content-Type"  = "application/json"
        }
    }
    catch {
        Write-Error "Error on Invoke-RestMethod: $($_.Exception.Message)"
        break
    }

    # 4) Prüfen, ob die API Erfolg meldet
    if (-not $response.success) {
        Write-Error "API-Error: $($response.errors | ConvertTo-Json -Depth 5)"
        break
    }

    # 5) Resultate anhängen und weiterschalten
    $allResults += $response.result
    $page++
}
while ($page -le $response.result_info.total_pages)

# 6) Ergebnis speichern oder Warnung ausgeben
if ($allResults.Count -gt 0) {
    $allResults | ConvertTo-Json -Depth 10 | Set-Content -Path $OutputFile
    Write-Host "✅ Finished: $($allResults.Count) Deployments in '$OutputFile'." -ForegroundColor Green
}
else {
    Write-Warning "No deployments."
}
