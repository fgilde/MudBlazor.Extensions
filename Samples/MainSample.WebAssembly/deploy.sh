#!/bin/sh
curl -sSL https://dot.net/v1/dotnet-install.sh > dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh -c 10.0 -InstallDir ./dotnet10
./dotnet10/dotnet --version
./dotnet10/dotnet restore
./dotnet10/dotnet build
./dotnet10/dotnet publish -c Release -o output MainSample.WebAssembly.csproj