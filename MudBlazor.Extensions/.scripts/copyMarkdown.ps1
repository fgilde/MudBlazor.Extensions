# Script to call copyChangeLog.ps1 with different parameters


.\copyFromMarkdown.ps1 -SourceFile "CHANGELOG.md" -Tag "CHANGELOG" -Count 10 -AddLink 0
.\copyFromMarkdown.ps1 -SourceFile "DialogExtensions.md" -Tag "DIALOG_EXT" -SourceTag "DIALOG_EXT"
.\copyFromMarkdown.ps1 -SourceFile "MudExFileDisplay.md" -Tag "FILEDISPLAY" -SourceTag "FILEDISPLAY"
.\copyFromMarkdown.ps1 -SourceFile "MudExUploadEdit.md" -Tag "UPLOADEDIT" -SourceTag "UPLOADEDIT"
.\copyFromMarkdown.ps1 -SourceFile "ObjectEdit.md" -Tag "OBJECTEDIT" -SourceTag "OBJECTEDIT"
.\copyFromMarkdown.ps1 -SourceFile "https://raw.githubusercontent.com/wiki/fgilde/MudBlazor.Extensions/Showcase.md" -Tag "WIKI" -All 1 -AddLink 0

# .\copyFromMarkdown.ps1 -SourceFile "ObjectEdit.md" -Tag "TEST" -SourceTag "TEST"