#!/bin/sh
curl -sSL https://dot.net/v1/dotnet-install.sh > dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh -c 8.0 -InstallDir ./dotnet8
./dotnet8/dotnet --version
./dotnet8/dotnet restore
./dotnet8/dotnet build ./Samples/MainSample.WebAssembly/MainSample.WebAssembly.csproj
./dotnet8/dotnet publish -c Release -o output ./Samples/MainSample.WebAssembly/MainSample.WebAssembly.csproj