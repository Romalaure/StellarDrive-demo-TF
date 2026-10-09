# Builds the mod in Release and creates dist/StellarDrive-demo-TF.zip in the layout
# StellarModManager expects: mod.json at the root, the DLL under Mods/.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src\StellarDriveDemoTF\StellarDriveDemoTF.csproj'
$dist = Join-Path $root 'dist'
$stage = Join-Path $dist 'stage'

dotnet build $project -c Release -p:DeployToGame=false
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

Remove-Item $dist -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force (Join-Path $stage 'Mods') | Out-Null
Copy-Item (Join-Path $root 'Mods\mod.json') $stage
Copy-Item (Join-Path $root 'src\StellarDriveDemoTF\bin\Release\StellarDriveDemoTF.dll') (Join-Path $stage 'Mods')

$version = (Get-Content (Join-Path $root 'Mods\mod.json') -Raw | ConvertFrom-Json).version
$zip = Join-Path $dist 'StellarDrive-demo-TF.zip'
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip
Remove-Item $stage -Recurse -Force
Write-Host "Packaged $zip (upload it to a GitHub release tagged v$version)"
