param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$project = Join-Path $repoRoot "src\Pickwise\Pickwise.csproj"
$artifacts = Join-Path $repoRoot "artifacts"
$publishDir = Join-Path $artifacts "publish\Pickwise-$Runtime"
$zipPath = Join-Path $artifacts "Pickwise-$Runtime.zip"

New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}
if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

dotnet publish $project -c $Configuration -r $Runtime --self-contained false -o $publishDir
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force

Write-Host "Created $zipPath"
