param([Parameter(Mandatory=$true)][string]$Source)
$ErrorActionPreference = 'Stop'
$sourceRoot = (Resolve-Path -LiteralPath $Source).Path
$manifestPath = Join-Path $sourceRoot 'manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.version -ne 1 -or $manifest.task -ne 'M08' -or $manifest.sourceModel -ne 'enemy_1003_ncbow') { throw 'Not the approved M08 media package.' }
$expected = @('portrait.png')
foreach ($clipName in @('idle','walk','attack')) {
    for ($frameNumber = 0; $frameNumber -lt 60; $frameNumber++) { $expected += ('{0}/{1:0000}.png' -f $clipName,$frameNumber) }
}
if (@($manifest.files).Count -ne $expected.Count) { throw 'Unexpected media file count.' }
foreach ($relativeName in $expected) {
    $record = @($manifest.files | Where-Object { $_.path -ceq $relativeName })
    if ($record.Count -ne 1) { throw "Missing or duplicate manifest entry: $relativeName" }
    $inputFile = Join-Path $sourceRoot $relativeName
    if (!(Test-Path -LiteralPath $inputFile -PathType Leaf)) { throw "Missing media: $relativeName" }
    if ((Get-FileHash -LiteralPath $inputFile -Algorithm SHA256).Hash -ine $record[0].sha256) { throw "Media hash mismatch: $relativeName" }
}
# All inputs are checked before writing. Only fixed, known relative paths are copied.
$projectRoot = Split-Path -Parent $PSScriptRoot
$destination = Join-Path $projectRoot 'Assets/StreamingAssets/ArcherOriginal'
foreach ($relativeName in ($expected + 'manifest.json')) {
    $outputFile = Join-Path $destination $relativeName
    New-Item -ItemType Directory -Path (Split-Path -Parent $outputFile) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $sourceRoot $relativeName) -Destination $outputFile -Force
}
Write-Output 'M08 local media installed. In Unity: Yesterday Convoy > Art > M08 Archer Approved Preview.'
