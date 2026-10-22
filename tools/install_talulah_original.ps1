param([Parameter(Mandatory=$true)][string]$Source)
$ErrorActionPreference='Stop'
$taskSource=(Resolve-Path -LiteralPath $Source).Path
$taskManifestPath=Join-Path $taskSource 'manifest.json'
$taskManifest=Get-Content -LiteralPath $taskManifestPath -Raw | ConvertFrom-Json
if($taskManifest.version -ne 1 -or $taskManifest.cardId -ne 'CARD_TALULAH_TRIAL' -or $taskManifest.clips.Count -ne 4) { throw 'Not a supported Talulah local-art package.' }
$taskNames=@('manifest.json','portrait.png')
foreach($taskClip in $taskManifest.clips) {
    foreach($taskPage in $taskClip.pages) {
        if([IO.Path]::GetFileName($taskPage.file) -ne $taskPage.file -or $taskPage.file -notmatch '\.png$') { throw 'Invalid atlas filename.' }
        $taskNames+=$taskPage.file
    }
}
$taskNames=$taskNames | Sort-Object -Unique
foreach($taskName in $taskNames) {
    if(-not (Test-Path -LiteralPath (Join-Path $taskSource $taskName) -PathType Leaf)) { throw "Missing $taskName" }
}
$taskProjectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskDestination=Join-Path $taskProjectRoot 'Assets/StreamingAssets/TalulahOriginal'
New-Item -ItemType Directory -Force -Path $taskDestination | Out-Null
foreach($taskName in $taskNames) {
    Copy-Item -LiteralPath (Join-Path $taskSource $taskName) -Destination (Join-Path $taskDestination $taskName) -Force
}
Write-Output "Installed $($taskNames.Count) local files into $taskDestination"
Write-Output 'Runtime art is tracked with source; this installer is optional. This command grants no redistribution license.'
