param([Parameter(Mandatory=$true)][string]$Source,[Parameter(Mandatory=$true)][ValidateSet('M02','M03','M04','M05','M06','M07','M08','M09','M10','M11','M12','M13','M14','M15','M16','M17','M18','M19','M20')][string]$Task)
$ErrorActionPreference='Stop'
$packageRoot=(Resolve-Path -LiteralPath (Join-Path $Source $Task)).Path
$data=Get-Content -LiteralPath (Join-Path $packageRoot 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if($data.version -ne 1 -or $data.task -cne $Task -or $data.clips.Count -ne 4) { throw 'Invalid monster package.' }
$keys=@($data.clips | ForEach-Object key | Sort-Object -Unique)
if(($keys -join ',') -cne 'attack,death,idle,walk') { throw 'Missing or duplicate clip.' }
$expected=@('portrait.png')
foreach($clip in $data.clips) {
    foreach($page in $clip.pages) {
        if($page.file -cnotmatch ('^'+$clip.key+'-[0-9]{2}\.png$')) { throw 'Invalid atlas filename.' }
        $expected+=$page.file
    }
}
if($data.files.Count -ne $expected.Count -or @($expected | Sort-Object -Unique).Count -ne $expected.Count) { throw 'Duplicate or extra files.' }
foreach($file in $expected) {
    $record=@($data.files | Where-Object { $_.file -ceq $file })
    if($record.Count -ne 1 -or (Get-FileHash -LiteralPath (Join-Path $packageRoot $file) -Algorithm SHA256).Hash -ine $record[0].sha256) { throw "Hash mismatch: $file" }
}
$target=Join-Path (Split-Path -Parent $PSScriptRoot) ('Assets/StreamingAssets/MonsterCombatArt/'+$Task)
New-Item -ItemType Directory -Force -Path $target | Out-Null
foreach($file in ($expected+@('manifest.json'))) { Copy-Item -LiteralPath (Join-Path $packageRoot $file) -Destination (Join-Path $target $file) -Force }
Write-Output "$Task local combat media installed. Not licensed for public redistribution."
