param([string]$BuildDirectory='Builds/ProjectF-Web-preview')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$source=[IO.Path]::GetFullPath((Join-Path $projectRoot $BuildDirectory))
$allowed=[IO.Path]::GetFullPath((Join-Path $projectRoot 'Builds'))+[IO.Path]::DirectorySeparatorChar
if(-not $source.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Web output must be inside project Builds.'}
if(-not (Test-Path -LiteralPath (Join-Path $source 'index.html'))){throw 'Build web player first.'}
$raw=$source
$source=Join-Path $projectRoot 'Builds/WebPublish'
if(Test-Path -LiteralPath $source){throw 'WebPublish already exists; preserve it or choose a new destination before repackaging.'}
New-Item -ItemType Directory -Path $source | Out-Null
foreach($name in @('index.html','Build','StreamingAssets','TemplateData')){
    $item=Join-Path $raw $name
    if(Test-Path -LiteralPath $item){Copy-Item -LiteralPath $item -Destination $source -Recurse}
}
$buildFiles=@(Get-ChildItem -LiteralPath (Join-Path $source 'Build') -File)
foreach($pattern in @('*.loader.js','*.data.unityweb','*.wasm.unityweb','*.framework.js.unityweb')){
    if(@($buildFiles | Where-Object Name -Like $pattern).Count -ne 1){throw "Expected one $pattern"}
}
$licenses=Join-Path $source 'Licenses'
New-Item -ItemType Directory -Force -Path $licenses | Out-Null
$notices=[ordered]@{
    'NanumGothic-OFL.txt'='Assets/ThirdParty/NanumGothic/OFL.txt'
    'LiberationSans-OFL.txt'='Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt'
    'QuaterniusTanks.txt'='Assets/ThirdParty/QuaterniusTanks/License.txt'
    'KenneyParticles.txt'='Assets/ThirdParty/KenneyParticles/License.txt'
    'PolyHavenTerrain.txt'='Assets/ThirdParty/PolyHavenRockyTerrain/License.txt'
    'PolyHavenBattlefield.txt'='Assets/ThirdParty/PolyHavenBattlefield/License.txt'
    'PolyforkShell.txt'='Assets/ThirdParty/PolyforkShell/License.txt'
}
$links=foreach($notice in $notices.GetEnumerator()){
    Copy-Item -LiteralPath (Join-Path $projectRoot $notice.Value) -Destination (Join-Path $licenses $notice.Key)
    '<li><a href="'+$notice.Key+'">'+$notice.Key+'</a></li>'
}
$licenseHtml='<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>프로젝트 F · 에셋 라이선스</title><style>body{font:18px/1.8 system-ui;max-width:760px;margin:50px auto;padding:20px;background:#10191d;color:#eee}a{color:#ffad71}</style><h1>사용 에셋 · 라이선스</h1><p>탱크·지형·파티클·포탄·글꼴의 출처와 라이선스입니다. 게임에서 생성한 추가 모델과 음향도 사용됩니다.</p><ul>'+($links -join '')+'</ul><a href="../">게임으로 돌아가기</a></html>'
[IO.File]::WriteAllText((Join-Path $licenses 'index.html'),$licenseHtml,[Text.UTF8Encoding]::new($false))
$unsafe=@(Get-ChildItem -LiteralPath $source -Recurse -File | Where-Object {$_.Name -match '\.(pdb|mdb|cs|blend|fbx|symbols\.json)(\.gz|\.br)?$' -or $_.FullName -match 'DoNotShip|BackUpThisFolder'})
if($unsafe.Count){throw 'Unexpected source/debug artifacts in web output; inspect before publishing.'}
$files=@(Get-ChildItem -LiteralPath $source -Recurse -File | Where-Object Name -ne 'release.json' | ForEach-Object {
    [ordered]@{path=[IO.Path]::GetRelativePath($source,$_.FullName).Replace('\','/');bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLower()}
})
[ordered]@{platform='PC WebGL';sourceCommit=(git -C $projectRoot rev-parse HEAD);workingTreeAssetsMayDiffer=$true;actor='editor-tooling-codex';createdUtc=[DateTime]::UtcNow.ToString('o');files=$files} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $source 'release.json') -Encoding utf8
$files | Format-Table path,bytes
