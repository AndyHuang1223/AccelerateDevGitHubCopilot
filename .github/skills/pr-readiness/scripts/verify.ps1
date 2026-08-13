$ErrorActionPreference = "Stop"
dotnet build
dotnet test
git diff --check

