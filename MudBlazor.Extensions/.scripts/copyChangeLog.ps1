# Read the changelog
$changelog = Get-Content -Path ..\Docs\CHANGELOG.md

# Get the latest 5 changes
$latestChanges = $changelog | Where-Object { $_ -match ">\s" } | Select-Object -First 5

# Read the README
$readme = Get-Content -Path ..\..\README.md

# Find where the CHANGELOG:START and CHANGELOG:END are
$startIndex = $readme.IndexOf("<!-- CHANGELOG:START -->") + 1
$endIndex = $readme.IndexOf("<!-- CHANGELOG:END -->")

# Replace the old changelog with the new one
$updatedReadme = $readme[0..($startIndex-1)] + $latestChanges + $readme[$endIndex..($readme.Length-1)]

# Write the new README back to the file
$updatedReadme | Out-File -FilePath ..\..\README.md
