#!/bin/sh
# Cloudflare Pages build for www.mudex.org.
# Deliberately no "set -e": the repo wide restore below is allowed to fail (the MAUI sample cannot
# restore on linux) and the build still produces a valid site. Only the steps that really matter
# are checked explicitly.

curl -sSL https://dot.net/v1/dotnet-install.sh > dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh -c 10.0 -InstallDir ./dotnet10
./dotnet10/dotnet --version

# A reused msbuild node keeps the file globs of its first evaluation, which would drop the web
# component bundle from the sample again.
export MSBUILDDISABLENODEREUSE=1

# Must run before anything evaluates the sample project: publishing this drops the web component
# bundle into Samples/MainSample.WebAssembly/wwwroot/wc, and the sample only picks those files up if
# they already exist the first time its project is loaded. This is what makes
# https://www.mudex.org/wc/mudex.js exist.
echo "--- publishing web components ---"
./dotnet10/dotnet publish -c Release ./Samples/MudEx.WebComponents/MudEx.WebComponents.csproj
if [ ! -f ./Samples/MainSample.WebAssembly/wwwroot/wc/mudex.js ]; then
  echo "BUILD FAILED: the web component publish did not produce wwwroot/wc/mudex.js (see the output above)."
  exit 1
fi

echo "--- building the sample ---"
./dotnet10/dotnet restore ./Samples/MainSample.WebAssembly/MainSample.WebAssembly.csproj
./dotnet10/dotnet build ./Samples/MainSample.WebAssembly/MainSample.WebAssembly.csproj
./dotnet10/dotnet publish -c Release -o output ./Samples/MainSample.WebAssembly/MainSample.WebAssembly.csproj

if [ ! -f output/wwwroot/index.html ]; then
  echo "BUILD FAILED: the sample publish produced no output/wwwroot/index.html."
  exit 1
fi

# Without this every /wc/ path falls through to the blazor spa and the web components are dead.
if [ ! -f output/wwwroot/wc/mudex.js ]; then
  echo "BUILD FAILED: output/wwwroot/wc is missing, /wc/mudex.js would serve the spa fallback."
  exit 1
fi

echo "--- done ---"
echo "web component bundle: $(du -sm output/wwwroot/wc | cut -f1) MB"
