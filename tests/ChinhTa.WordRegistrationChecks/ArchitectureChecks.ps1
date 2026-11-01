param([Parameter(Mandatory=$true)][string]$RepositoryRoot)
$ErrorActionPreference='Stop'
. (Join-Path $RepositoryRoot 'scripts/WordAddin/Architecture.ps1')
$script:checks=0
function Assert($Value,[string]$Message) { if (!$Value) { throw "FAIL: $Message" };$script:checks++;"PASS $Message" }
function Reject([scriptblock]$Action) { try { & $Action | Out-Null;return $false } catch { return $true } }
$folder=Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString())
New-Item $folder -ItemType Directory | Out-Null
try {
    foreach ($spec in @(@('x86',0x014C),@('x64',0x8664),@('unsupported',0xAA64))) {
        $path=Join-Path $folder ($spec[0]+'.exe')
        $bytes=New-Object byte[] 80
        [BitConverter]::GetBytes([uint16]0x5A4D).CopyTo($bytes,0)
        [BitConverter]::GetBytes([int]64).CopyTo($bytes,60)
        [BitConverter]::GetBytes([uint32]0x4550).CopyTo($bytes,64)
        [BitConverter]::GetBytes([uint16]$spec[1]).CopyTo($bytes,68)
        [IO.File]::WriteAllBytes($path,$bytes)
    }
    Assert ((Read-ExecutableArchitecture (Join-Path $folder 'x86.exe')) -eq 'x86') 'PE x86 is detected independently of the Windows/PowerShell architecture'
    Assert ((Read-ExecutableArchitecture (Join-Path $folder 'x64.exe')) -eq 'x64') 'PE x64 is detected from the executable'
    Assert ((Get-AddinRegistryView 'x86') -eq [Microsoft.Win32.RegistryView]::Registry32) 'Word x86 selects Registry32'
    Assert ((Get-AddinRegistryView 'x64') -eq [Microsoft.Win32.RegistryView]::Registry64) 'Word x64 selects Registry64'
    Assert-WordPackageArchitecture 'x86' 'x86';Assert $true 'Word 32-bit accepts its x86 package on a 64-bit CI operating system'
    Assert-WordPackageArchitecture 'x64' 'x64';Assert $true 'Word 64-bit accepts its x64 package'
    Assert (Reject { Assert-WordPackageArchitecture 'x64' 'x86' }) 'A mismatched x64 package is rejected for Word 32-bit before installation'
    Assert (Reject { Assert-WordPackageArchitecture 'x86' 'x64' }) 'A mismatched x86 package is rejected for Word 64-bit before installation'
    Assert (Reject { Get-AddinRegistryView 'unknown' }) 'Unknown package architecture cannot pick a registry view'
    Assert (Reject { Read-ExecutableArchitecture (Join-Path $folder 'unsupported.exe') }) 'Unsupported PE architecture cannot be mistaken for x86'
    [IO.File]::WriteAllBytes((Join-Path $folder 'short.exe'),[byte[]]@(1,2,3))
    Assert (Reject { Read-ExecutableArchitecture (Join-Path $folder 'short.exe') }) 'A truncated executable is rejected'
    foreach ($name in @('Architecture.ps1','Install-WordAddin.ps1','Uninstall-WordAddin.ps1','Diagnose-WordAddin.ps1')) {
        $parseTokens=$null;$parseErrors=$null
        [void][Management.Automation.Language.Parser]::ParseFile((Join-Path $RepositoryRoot ('scripts/WordAddin/'+$name)),[ref]$parseTokens,[ref]$parseErrors)
        Assert (@($parseErrors).Count -eq 0) ("Windows PowerShell parses "+$name)
    }
} finally { Remove-Item $folder -Recurse -Force }
"$script:checks architecture checks passed. Word NOT_RUN."
