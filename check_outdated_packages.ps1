# Path to the .NET project file
$projectPath = Get-ChildItem -Path . -Filter *.csproj | Select-Object -ExpandProperty FullName

if (-not $projectPath) {
    Write-Host "No .NET project file found in the current directory." -ForegroundColor Red
    exit 1
}

Write-Host "Found project file: $projectPath" -ForegroundColor Green

# Run `dotnet list package --outdated` to get outdated packages
Write-Host "Checking for outdated NuGet packages..." -ForegroundColor Cyan
$outdatedPackages = dotnet list $projectPath package --outdated

if ($outdatedPackages -match "has the following updates to its packages") {
    Write-Host "`nOutdated NuGet packages:" -ForegroundColor Yellow
    $outdatedPackages
    exit 0
}

exit 0