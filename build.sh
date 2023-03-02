#!/bin/sh
curl -sSL https://dot.net/v1/dotnet-install.sh > dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh -c 7.0 -InstallDir ./dotnet7
./dotnet7/dotnet --version
./dotnet7/dotnet restore
./dotnet7/dotnet build
./dotnet7/dotnet publish -c Release -o output ./Samples/MainSample.WebAssembly/MainSample.WebAssembly.csproj