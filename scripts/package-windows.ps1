param(
    [string]$BuildDirectory = 'Builds/ProjectF-Windows-0.1.0-preview',
    [string]$Version = '0.1.0-preview'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$source = [IO.Path]::GetFullPath((Join-Path $projectRoot $BuildDirectory))
if (-not (Test-Path -LiteralPath (Join-Path $source 'ProjectF.exe'))) { throw 'Build ProjectF.exe first.' }
if (-not (Test-Path -LiteralPath (Join-Path $source 'ProjectF_Data'))) { throw 'Missing player data.' }
if (-not (Test-Path -LiteralPath (Join-Path $source 'UnityPlayer.dll'))) { throw 'Missing Unity player runtime.' }
if (-not (Test-Path -LiteralPath (Join-Path $source 'ProjectF_Data/globalgamemanagers')) -and -not (Test-Path -LiteralPath (Join-Path $source 'ProjectF_Data/data.unity3d'))) { throw 'Incomplete player data.' }
if ((Test-Path -LiteralPath (Join-Path $source 'ProjectF_Data/Managed')) -and -not (Test-Path -LiteralPath (Join-Path $source 'MonoBleedingEdge'))) { throw 'Missing Mono runtime.' }
if ($Version -notmatch '^[a-zA-Z0-9.-]+$') { throw 'Invalid version.' }
$releaseName = "ProjectF-Windows-$Version"
$destination = Join-Path $projectRoot "Builds/Downloads/$releaseName"
$archive = "$destination.zip"
if ((Test-Path -LiteralPath $destination) -or (Test-Path -LiteralPath $archive)) { throw 'Release already exists; choose a new version.' }
New-Item -ItemType Directory -Path $destination -Force | Out-Null
foreach ($item in Get-ChildItem -LiteralPath $source -Force) {
    if ($item.Name -match 'DoNotShip|BackUpThisFolder|BurstDebugInformation|\.pdb$|\.mdb$|\.log$') { continue }
    Copy-Item -LiteralPath $item.FullName -Destination $destination -Recurse
}
# Debug symbols in generated subdirectories are unnecessary for this public preview.
Get-ChildItem -LiteralPath $destination -Recurse -File | Where-Object { $_.Extension -in '.pdb','.mdb' } | ForEach-Object { Remove-Item -LiteralPath $_.FullName }
$licenses = Join-Path $destination 'Licenses'
New-Item -ItemType Directory -Path $licenses | Out-Null
$notices = @{
    'NanumGothic-OFL.txt' = 'Assets/ThirdParty/NanumGothic/OFL.txt'
    'LiberationSans-OFL.txt' = 'Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt'
    'QuaterniusTanks.txt' = 'Assets/ThirdParty/QuaterniusTanks/License.txt'
    'KenneyParticles.txt' = 'Assets/ThirdParty/KenneyParticles/License.txt'
    'PolyHavenTerrain.txt' = 'Assets/ThirdParty/PolyHavenRockyTerrain/License.txt'
    'PolyHavenBattlefield.txt' = 'Assets/ThirdParty/PolyHavenBattlefield/License.txt'
    'PolyforkShell.txt' = 'Assets/ThirdParty/PolyforkShell/License.txt'
}
foreach ($notice in $notices.GetEnumerator()) { Copy-Item -LiteralPath (Join-Path $projectRoot $notice.Value) -Destination (Join-Path $licenses $notice.Key) }
Copy-Item -LiteralPath (Join-Path $projectRoot 'Docs/download-guide.html') -Destination (Join-Path $destination 'START-HERE.html')
$sourceCommit = git -C $projectRoot rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Cannot read source revision.' }
[ordered]@{ version=$Version; platform='Windows x64'; builtFromCommit=$sourceCommit; workingTreeAssetsMayDiffer=$true; packagedAtUtc=[DateTime]::UtcNow.ToString('o'); actor='editor-tooling-codex' } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $destination 'release.json') -Encoding utf8
Compress-Archive -LiteralPath $destination -DestinationPath $archive -CompressionLevel Optimal
$hash = Get-FileHash -LiteralPath $archive -Algorithm SHA256
"$($hash.Hash.ToLower())  $releaseName.zip" | Set-Content -LiteralPath "$archive.sha256" -Encoding ascii
Get-Item -LiteralPath $archive | Select-Object FullName,Length
$hash | Select-Object Algorithm,Hash
