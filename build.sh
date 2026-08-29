#!/bin/sh
# Cloudflare Pages build for www.mudex.org.
# Deliberately no "set -e": the repo wide restore is allowed to fail (the MAUI sample cannot restore
# on linux) and the build still produces a valid site. Only what really matters is checked.

curl -sSL https://dot.net/v1/dotnet-install.sh > dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh -c 10.0 -InstallDir ./dotnet10
./dotnet10/dotnet --version

export MSBUILDDISABLENODEREUSE=1

WC_PUBLISH=./Samples/MudEx.WebComponents/bin/Release/net10.0/publish/wwwroot

echo "--- publishing web components ---"
./dotnet10/dotnet publish -c Release ./Samples/MudEx.WebComponents/MudEx.WebComponents.csproj
if [ ! -f "$WC_PUBLISH/mudex.js" ]; then
  echo "BUILD FAILED: the web component publish did not produce $WC_PUBLISH/mudex.js"
  exit 1
fi

echo "--- building the sample ---"
./dotnet10/dotnet restore ./Samples/MainSample.WebAssembly/MainSample.WebAssembly.csproj
./dotnet10/dotnet build ./Samples/MainSample.WebAssembly/MainSample.WebAssembly.csproj
./dotnet10/dotnet publish -c Release -o output ./Samples/MainSample.WebAssembly/MainSample.WebAssembly.csproj

if [ ! -f output/wwwroot/index.html ]; then
  echo "BUILD FAILED: the sample publish produced no output/wwwroot/index.html"
  exit 1
fi

# The bundle is a finished artifact and must land verbatim. Going through the sample wwwroot means
# going through its static web asset pipeline, which fingerprints the already fingerprinted files a
# second time (MudBlazor.Extensions.362jvoervk.362jvoervk.lib.module.js) - the runtime then asks for
# a name that does not exist and every /wc/ request falls through to the spa.
echo "--- copying the web component bundle into the output ---"
rm -rf output/wwwroot/wc
mkdir -p output/wwwroot/wc
cp -r "$WC_PUBLISH/." output/wwwroot/wc/
# pre-compressed copies and source maps are dead weight, the cdn compresses on the fly
find output/wwwroot/wc \( -name '*.gz' -o -name '*.br' -o -name '*.map' \) -type f -delete

if [ ! -f output/wwwroot/wc/mudex.js ]; then
  echo "BUILD FAILED: output/wwwroot/wc is missing, /wc/mudex.js would serve the spa fallback"
  exit 1
fi

# The runtime loads these by their fingerprinted name. If the name got mangled, boot dies with
# "Failed to load config file" and nothing renders - so check one of them explicitly.
if ! ls output/wwwroot/wc/_content/MudBlazor.Extensions/MudBlazor.Extensions.*.lib.module.js >/dev/null 2>&1; then
  echo "BUILD FAILED: the MudBlazor.Extensions js initializer is missing from the bundle"
  exit 1
fi
mangled=$(ls output/wwwroot/wc/_content/*/*.lib.module.js 2>/dev/null | grep -E '\.[a-z0-9]{10}\.[a-z0-9]{10}\.lib\.module\.js$')
if [ -n "$mangled" ]; then
  echo "BUILD FAILED: double fingerprinted js initializer in the bundle:"
  echo "$mangled"
  exit 1
fi

echo "--- done ---"
echo "web component bundle: $(du -sm output/wwwroot/wc | cut -f1) MB"
