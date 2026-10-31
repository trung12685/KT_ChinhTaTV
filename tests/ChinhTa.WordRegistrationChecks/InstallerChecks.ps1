param([Parameter(Mandatory=$true)][string]$PackagePath,[Parameter(Mandatory=$true)][ValidateSet('x86','x64')][string]$Architecture,[Parameter(Mandatory=$true)][string]$ProbeExe)
$ErrorActionPreference='Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'This fixture is only for a disposable GitHub Actions Windows runner, not a user PC.' }
$PackagePath=(Resolve-Path $PackagePath).Path
$ProbeExe=(Resolve-Path $ProbeExe).Path
. (Join-Path $PackagePath 'Architecture.ps1')
$manifest=Get-Content (Join-Path $PackagePath 'addin-package.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$target=Get-WordAddinTarget $manifest
$dataRoot=Join-Path $env:LOCALAPPDATA 'ChinhTaViet\WordAddin'
$appPath='Software\Microsoft\Windows\CurrentVersion\App Paths\WINWORD.EXE'
$owned=Get-WordAddinKeys
$views=@([Microsoft.Win32.RegistryView]::Registry32,[Microsoft.Win32.RegistryView]::Registry64)
foreach ($view in $views) {
    $regBase=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,$view)
    try {
        foreach ($path in (@($owned)+@($appPath))) {
            $key=$regBase.OpenSubKey($path)
            if ($key) { $key.Close();throw 'Refuse to overwrite an existing Office/Add-in registration in the test environment.' }
        }
    } finally { $regBase.Close() }
}
if (Test-Path $dataRoot) { throw 'Refuse to touch an existing personal Add-in data folder in the test environment.' }
$script:checks=0
function Assert($Value,[string]$Message) { if (!$Value) { throw "FAIL: $Message" };$script:checks++;"PASS $Message" }
function Write-WordFixture([string]$Path,[string]$Bitness) {
    # PE header fixture for installer detection only. Never executed as Word.
    $bytes=New-Object byte[] 80
    [BitConverter]::GetBytes([uint16]0x5A4D).CopyTo($bytes,0)
    [BitConverter]::GetBytes([int]64).CopyTo($bytes,60)
    [BitConverter]::GetBytes([uint32]0x4550).CopyTo($bytes,64)
    $machine=if ($Bitness -eq 'x86') { 0x014C } else { 0x8664 }
    [BitConverter]::GetBytes([uint16]$machine).CopyTo($bytes,68)
    [IO.File]::WriteAllBytes($Path,$bytes)
}
$fixtureRoot=Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString())
$sentinel='Software\Microsoft\Office\Word\Addins\ChinhTaViet.CI.'+[Guid]::NewGuid().ToString('N')
$regBase=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,(Get-AddinRegistryView $Architecture))
try {
    New-Item $fixtureRoot -ItemType Directory | Out-Null
    New-Item $dataRoot -ItemType Directory | Out-Null
    $personal=Join-Path $dataRoot 'personal-terms.json'
    [IO.File]::WriteAllText($personal,'{"Version":1,"Revision":1,"Terms":[{"Text":"TenRiengCI","Key":"tenriengci"}]}')
    $personalHash=(Get-FileHash $personal -Algorithm SHA256).Hash
    $key=$regBase.CreateSubKey($sentinel);$key.SetValue('FriendlyName','Unrelated add-in sentinel');$key.Close()
    $wordFile=Join-Path $fixtureRoot 'WINWORD.EXE'
    $other=if ($Architecture -eq 'x86') { 'x64' } else { 'x86' }
    Write-WordFixture $wordFile $other
    $key=$regBase.CreateSubKey($appPath);$key.SetValue('',$wordFile);$key.Close()
    $rejected=$false
    try { & (Join-Path $PackagePath 'Install-WordAddin.ps1') | Out-Null } catch { $rejected=$_.Exception.Message.Contains("win-$other") }
    Assert ($rejected -and !(Test-Path $target)) 'A real mismatched installation stops before creating its target directory'
    $key=$regBase.OpenSubKey($owned[0]);$notRegistered=$null -eq $key;if ($key) { $key.Close() }
    Assert $notRegistered 'A mismatched installation does not register an Add-in'
    Write-WordFixture $wordFile $Architecture
    $detected=Get-WordArchitecture
    Assert ($detected.Architecture -eq $Architecture -and $detected.Source -eq 'WINWORD.EXE') 'Installer reads the fixture WINWORD PE rather than inferring Office bitness from Windows'
    & (Join-Path $PackagePath 'Install-WordAddin.ps1')
    $key=$regBase.OpenSubKey($owned[0])
    try { Assert ($key -and $key.GetValue('ChinhTaVietArchitecture') -eq $Architecture -and $key.GetValue('ChinhTaVietInstallRoot') -eq $target) 'Actual user registration records the package architecture and installed directory' }
    finally { if ($key) { $key.Close() } }
    $otherRegistry=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,(Get-AddinRegistryView $other))
    try {
        $key=$otherRegistry.OpenSubKey('Software\Classes\CLSID\{E0382B17-15A1-49D9-AC6A-9322607C6AF7}')
        $absent=$null -eq $key;if ($key) { $key.Close() }
        Assert $absent 'The connector CLSID is not mistakenly written to the opposite architecture view'
    } finally { $otherRegistry.Close() }
    if ($env:GITHUB_REF -like 'refs/heads/diagnostic/*') {
        $framework=if ($Architecture -eq 'x64') { 'Framework64' } else { 'Framework' }
        $regAsm=Join-Path $env:SystemRoot ("Microsoft.NET\$framework\v4.0.30319\RegAsm.exe")
        $reference=Join-Path $fixtureRoot 'regasm-reference.reg'
        & $regAsm (Join-Path $target 'ChinhTa.WordAddin.dll') "/regfile:$reference" /codebase /nologo
        if ($LASTEXITCODE -ne 0) { throw 'RegAsm reference generation failed' }
        'Reference registry generated without registering it:'
        Get-Content $reference
    }
    & $ProbeExe $Architecture $target
    if ($LASTEXITCODE -ne 0) { throw 'Registered COM activation probe failed' }
    Assert ((Get-FileHash $personal -Algorithm SHA256).Hash -eq $personalHash) 'Installation and COM activation preserve the personal dictionary bytes'
    # A stale uninstall package cannot remove the new registration.
    $stale=Join-Path $fixtureRoot 'stale';New-Item $stale -ItemType Directory | Out-Null
    Copy-Item (Join-Path $PackagePath 'Architecture.ps1'),(Join-Path $PackagePath 'Uninstall-WordAddin.ps1') $stale
    $manifest.Commit=('f'*40);$manifest | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $stale 'addin-package.json') -Encoding UTF8
    $rejected=$false;try { & (Join-Path $stale 'Uninstall-WordAddin.ps1') | Out-Null } catch { $rejected=$true }
    $key=$regBase.OpenSubKey($owned[0]);$stillRegistered=$null -ne $key;if ($key) { $key.Close() }
    Assert ($rejected -and $stillRegistered) 'A stale package is refused before removing the active registration'
    & (Join-Path $PackagePath 'Uninstall-WordAddin.ps1')
    $remaining=0
    foreach ($path in $owned) { $key=$regBase.OpenSubKey($path);if ($key) { $remaining++;$key.Close() } }
    Assert ($remaining -eq 0) 'Actual uninstall removes all five owned Add-in/COM keys'
    Assert ((Get-FileHash $personal -Algorithm SHA256).Hash -eq $personalHash) 'Uninstall preserves the personal dictionary bytes'
    $key=$regBase.OpenSubKey($sentinel)
    try { Assert ($key -and $key.GetValue('FriendlyName') -eq 'Unrelated add-in sentinel') 'Install and uninstall preserve an unrelated Word Add-in' }
    finally { if ($key) { $key.Close() } }
} finally {
    # The initial checks proved these keys/folders were absent; remove only fixtures.
    foreach ($path in (@($owned)+@($appPath,$sentinel))) { $regBase.DeleteSubKeyTree($path,$false) }
    $regBase.Close()
    if (Test-Path $dataRoot) { Remove-Item $dataRoot -Recurse -Force }
    if (Test-Path $fixtureRoot) { Remove-Item $fixtureRoot -Recurse -Force }
}
"$script:checks installer checks passed. Fixtures only; Microsoft Word: NOT_RUN."
