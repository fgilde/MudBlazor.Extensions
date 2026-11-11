import json
import os
from pathlib import Path
import xml.etree.ElementTree as ET

project = ET.parse("MudBlazor.Extensions/MudBlazor.Extensions.csproj")
version = ".".join(project.findtext(".//" + part) for part in ["MajorVersion", "MinorVersion", "PatchVersion"])
if not version:
    raise SystemExit("Library version could not be resolved.")
revision = os.environ.get("GITHUB_SHA", "local")[:12]
base = version.split("+", 1)[0]
print(base + (".source." if "-" in base else "-source.") + revision)
