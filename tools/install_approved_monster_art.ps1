param([Parameter(Mandatory=$true)][string]$Source)
$ErrorActionPreference = 'Stop'
$sourceRoot = (Resolve-Path -LiteralPath $Source).Path
$definitions = @{
    M02 = @{ idle=60; walk=48; attack=60 }
    M03 = @{ idle=60; run=32; attack=60 }
    M04 = @{ idle=60; move=60; attack=150; death=60 }
    M05 = @{ idle=120; move=120; attack=80 }
}
$copies = @()
foreach ($taskName in @('M02','M03','M04','M05')) {
    $packageRoot = Join-Path $sourceRoot $taskName
    $manifest = Get-Content -LiteralPath (Join-Path $packageRoot 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($manifest.version -ne 1 -or $manifest.task -cne $taskName -or $manifest.fps -ne 60) { throw "Invalid package: $taskName" }
    $expected = @('portrait.png')
    foreach ($clipName in $definitions[$taskName].Keys) {
        for ($frameNumber=0; $frameNumber -lt $definitions[$taskName][$clipName]; $frameNumber++) {
            $expected += ('{0}/{1:0000}.png' -f $clipName,$frameNumber)
        }
    }
    if (@($manifest.files).Count -ne $expected.Count) { throw "Incorrect file count: $taskName" }
    foreach ($relativeName in $expected) {
        $record = @($manifest.files | Where-Object { $_.path -ceq $relativeName })
        if ($record.Count -ne 1) { throw "Missing or duplicate entry: $taskName/$relativeName" }
        $inputFile = Join-Path $packageRoot $relativeName
        if ((Get-FileHash -LiteralPath $inputFile -Algorithm SHA256).Hash -ine $record[0].sha256) { throw "Hash mismatch: $taskName/$relativeName" }
        $copies += @{ input=$inputFile; relative=($taskName+'/'+$relativeName) }
    }
    $copies += @{ input=(Join-Path $packageRoot 'manifest.json'); relative=($taskName+'/manifest.json') }
}
# Validate all packages before any writes; only fixed known relative paths are copied.
$destinationRoot = Join-Path (Split-Path -Parent $PSScriptRoot) 'Assets/StreamingAssets/ApprovedMonsterArt'
foreach ($copy in $copies) {
    $outputFile = Join-Path $destinationRoot $copy.relative
    New-Item -ItemType Directory -Path (Split-Path -Parent $outputFile) -Force | Out-Null
    Copy-Item -LiteralPath $copy.input -Destination $outputFile -Force
}
Write-Output 'M02/M03/M04/M05 local art installed. Unity: Yesterday Convoy > Art > Approved Monster Previews.'
