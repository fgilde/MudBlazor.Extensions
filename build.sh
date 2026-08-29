#!/bin/sh
set -e

curl -sSL https://dot.net/v1/dotnet-install.sh > dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh -c 10.0 -InstallDir ./dotnet10
./dotnet10/dotnet --version

# A reused msbuild node keeps the file globs of its first evaluation, which would drop the web
# component bundle from the sample again.
export MSBUILDDISABLENODEREUSE=1

# Must run before anything evaluates the sample project: publishing this drops the web component
# bundle into Samples/MainSample.WebAssembly/wwwroot/wc, and the sample only picks those files up
# if they already exist the first time its project is loaded. That is what makes
# https://www.mudex.org/wc/mudex.js exist.
./dotnet10/dotnet publish -c Release ./Samples/MudEx.WebComponents/MudEx.WebComponents.csproj

./dotnet10/dotnet restore
./dotnet10/dotnet build ./Samples/MainSample.WebAssembly/MainSample.WebAssembly.csproj
./dotnet10/dotnet publish -c Release -o output ./Samples/MainSample.WebAssembly/MainSample.WebAssembly.csproj

# Fail the build instead of publishing a site where every /wc/ path falls through to the spa
if [ ! -f output/wwwroot/wc/mudex.js ]; then
  echo "Web component bundle missing from output/wwwroot/wc - /wc/mudex.js would serve the spa fallback."
  exit 1
fi
echo "Web component bundle in output: $(du -sm output/wwwroot/wc | cut -f1) MB"
