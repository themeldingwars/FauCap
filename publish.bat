dotnet clean
dotnet restore
dotnet build -c release
dotnet publish -c release -r win7-x64 -o FauCap/bin/dist/win-x64 --self-contained false
dotnet publish -c release -r linux-x64 -o FauCap/bin/dist/linux-x64 --self-contained false
dotnet publish -c release -r osx-x64 -o FauCap/bin/dist/osx-x64 --self-contained false

REM publish the package to github nuget
dotnet pack --configuration Release
dotnet nuget push "FauCap/bin/Release/FauCap.1.1.0.nupkg" --source "tmw"
dotnet nuget push "FauCapParser/bin/Release/FauCapParser.1.0.0.nupkg" --source "tmw"