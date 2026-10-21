param([Parameter(Mandatory=$true)][string]$Source)
$ErrorActionPreference='Stop'
$slugSource=(Resolve-Path -LiteralPath $Source).Path
$slugData=Get-Content -LiteralPath (Join-Path $slugSource 'manifest.json') -Raw | ConvertFrom-Json
if($slugData.version -ne 1 -or $slugData.cardId -ne 'CARD_ORIGINIUM_SLUG' -or $slugData.clips.Count -ne 6) { throw 'Not an approved-format slug package.' }
$slugRequired=@('idle','walk','start','stop','attack','death')
$slugKeys=@($slugData.clips | ForEach-Object key | Sort-Object -Unique)
if(($slugKeys -join ',') -ne (($slugRequired | Sort-Object) -join ',')) { throw 'Incomplete slug clips.' }
$slugFiles=@('manifest.json','portrait.png')
foreach($slugClip in $slugData.clips) {
    foreach($slugPage in $slugClip.pages) {
        if([IO.Path]::GetFileName($slugPage.file) -ne $slugPage.file -or $slugPage.file -notmatch '\.png$') { throw 'Invalid local filename.' }
        $slugFiles+=$slugPage.file
    }
}
$slugFiles=$slugFiles | Sort-Object -Unique
foreach($slugFile in $slugFiles) {
    if(-not (Test-Path -LiteralPath (Join-Path $slugSource $slugFile) -PathType Leaf)) { throw "Missing $slugFile" }
}
$slugProject=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$slugDestination=Join-Path $slugProject 'Assets/StreamingAssets/OriginiumSlugOriginal'
New-Item -ItemType Directory -Force -Path $slugDestination | Out-Null
foreach($slugFile in $slugFiles) { Copy-Item -LiteralPath (Join-Path $slugSource $slugFile) -Destination (Join-Path $slugDestination $slugFile) -Force }
Write-Output "Installed $($slugFiles.Count) local files. Original media remains Git-ignored; no redistribution rights are granted."
