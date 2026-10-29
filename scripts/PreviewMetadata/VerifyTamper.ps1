[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PackageFolder,
    [Parameter(Mandatory = $true)][string]$Version
)
$ErrorActionPreference = 'Stop'
$source = (Resolve-Path -LiteralPath $PackageFolder).Path
$helper = Join-Path $PSScriptRoot 'bin/Release/net8.0/PreviewMetadata.dll'
if (-not (Test-Path -LiteralPath $helper -PathType Leaf)) { throw 'Build PreviewMetadata before its package tamper checks' }
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('ChinhTa-PreviewTamper-' + [Guid]::NewGuid().ToString('N'))
$copy = Join-Path $temporary 'package'
$probeCount = 0
$controlCount = 0
New-Item -ItemType Directory -Path $temporary | Out-Null

function Verify-Expected {
    param([string]$Folder, [bool]$ShouldPass, [string]$Label)
    # Read the actual process exit code. Expected negative tests do not depend
    # on PowerShell's native stderr/ErrorActionPreference behavior.
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = 'dotnet'
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in @($helper, 'verify', $Folder, $Version)) { $start.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $start
    try {
        if (-not $process.Start()) { throw 'Could not start the package verifier' }
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(120000)) {
            try { $process.Kill() } catch { }
            throw "Package verifier timed out during $Label"
        }
        $process.WaitForExit()
        $output = $stdout.GetAwaiter().GetResult()
        $errorOutput = $stderr.GetAwaiter().GetResult()
        $code = $process.ExitCode
        if (($code -eq 0) -ne $ShouldPass) {
            Write-Output ($output + $errorOutput)
            throw "Unexpected package verifier result during $Label (exit $code; expected success=$ShouldPass)"
        }
        Write-Output "PASS $Label (verifier exit $code)"
    }
    finally { $process.Dispose() }
}

try {
    Verify-Expected $source $true 'original package before probes'
    $controlCount++
    Copy-Item -LiteralPath $source -Destination $copy -Recurse
    if (-not (Test-Path -LiteralPath (Join-Path $copy 'FILES.sha256') -PathType Leaf)) { throw 'The private package copy has the wrong layout' }
    Verify-Expected $copy $true 'private copy before probes'
    $controlCount++

    $sample = Get-ChildItem -LiteralPath (Join-Path $copy 'samples') -File -Filter '*.docx' | Sort-Object FullName | Select-Object -First 1
    if ($null -eq $sample) { throw 'No synthetic DOCX sample available for the tamper probe' }
    $sampleBytes = [IO.File]::ReadAllBytes($sample.FullName)
    if ($sampleBytes.Length -eq 0) { throw 'The selected sample is empty' }
    $changedBytes = [byte[]]$sampleBytes.Clone()
    $changedBytes[$changedBytes.Length - 1] = $changedBytes[$changedBytes.Length - 1] -bxor 1
    [IO.File]::WriteAllBytes($sample.FullName, $changedBytes)
    Verify-Expected $copy $false 'changed DOCX sample byte is rejected'
    $probeCount++
    [IO.File]::WriteAllBytes($sample.FullName, $sampleBytes)

    $extra = Join-Path $copy ('unexpected-' + [Guid]::NewGuid().ToString('N') + '.txt')
    $stream = [IO.File]::Open($extra, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $stream.WriteByte(1) } finally { $stream.Dispose() }
    Verify-Expected $copy $false 'unlisted extra file is rejected'
    $probeCount++
    [IO.File]::Delete($extra)

    $manifest = Join-Path $copy 'FILES.sha256'
    $manifestBytes = [IO.File]::ReadAllBytes($manifest)
    [IO.File]::AppendAllText($manifest, (('0' * 64) + "  ../outside`n"), [Text.UTF8Encoding]::new($false))
    Verify-Expected $copy $false 'manifest traversal entry is rejected'
    $probeCount++
    [IO.File]::WriteAllBytes($manifest, $manifestBytes)

    Verify-Expected $copy $true 'restored private copy after probes'
    $controlCount++
    Verify-Expected $source $true 'original package remains unchanged after probes'
    $controlCount++
    if ($probeCount -ne 3 -or $controlCount -ne 4) { throw 'The real-package probe set did not complete' }
    $summary = "Real extracted-package tamper probes: $probeCount passed; untouched/restored package controls: $controlCount passed."
    Write-Output $summary
    if ($env:GITHUB_STEP_SUMMARY) { "`n$summary No probe changes were applied to the published ZIP or extracted source package." >> $env:GITHUB_STEP_SUMMARY }
}
finally {
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Recurse -Force }
}
