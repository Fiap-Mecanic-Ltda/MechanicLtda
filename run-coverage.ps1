# Script to run tests with code coverage locally
# Usage: .\run-coverage.ps1

Write-Host "Cleaning previous coverage reports..." -ForegroundColor Cyan
Remove-Item -Path "./coverage" -Recurse -ErrorAction SilentlyContinue -Force

Write-Host "Creating coverage directory..." -ForegroundColor Cyan
New-Item -ItemType Directory -Path "./coverage" -Force | Out-Null

Write-Host "Restoring dependencies..." -ForegroundColor Cyan
dotnet restore

Write-Host "Building solution..." -ForegroundColor Cyan
dotnet build --no-restore --configuration Release

Write-Host "Running tests with coverage collection..." -ForegroundColor Cyan
dotnet test --no-build --configuration Release --logger "trx" --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

Write-Host "Merging coverage reports..." -ForegroundColor Cyan
$coverageFiles = Get-ChildItem -Path "./tests" -Filter "coverage.opencover.xml" -Recurse -ErrorAction SilentlyContinue | Sort-Object -Property LastWriteTime -Descending
if ($coverageFiles.Count -gt 0) {
	Write-Host "Found $($coverageFiles.Count) coverage file(s)" -ForegroundColor Yellow
	Copy-Item -Path $coverageFiles[0].FullName -Destination "./coverage/SonarQubeCoverage.xml" -Force
	Write-Host "Coverage report merged to ./coverage/SonarQubeCoverage.xml" -ForegroundColor Green
	Write-Host "Source file: $($coverageFiles[0].FullName)" -ForegroundColor Cyan
} else {
	Write-Host "No coverage files found in tests directory!" -ForegroundColor Red
	exit 1
}

Write-Host "`nCoverage collection completed!" -ForegroundColor Green
Write-Host "Report location: ./coverage/SonarQubeCoverage.xml" -ForegroundColor Cyan
