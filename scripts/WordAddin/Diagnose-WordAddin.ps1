param()
$ErrorActionPreference='Stop'
$manifest=Get-Content (Join-Path $PSScriptRoot 'addin-package.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.Architecture -notin @('x86','x64')) { throw 'Gói chẩn đoán thiếu kiến trúc x86/x64.' }
# COM must be probed inside the same Framework/bitness as Word, not PowerShell 7.
$is64=[Environment]::Is64BitProcess
if ($PSVersionTable.PSEdition -eq 'Core' -or ($manifest.Architecture -eq 'x86' -and $is64) -or ($manifest.Architecture -eq 'x64' -and !$is64)) {
    $folder='System32'
    if ([Environment]::Is64BitOperatingSystem -and $manifest.Architecture -eq 'x86') { $folder='SysWOW64' }
    if ([Environment]::Is64BitOperatingSystem -and !$is64 -and $manifest.Architecture -eq 'x64') { $folder='Sysnative' }
    if (![Environment]::Is64BitOperatingSystem -and $manifest.Architecture -eq 'x64') { throw 'Gói x64 không chạy trên Windows 32-bit.' }
    & "$env:SystemRoot\$folder\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath
    exit $LASTEXITCODE
}
. (Join-Path $PSScriptRoot 'Architecture.ps1')
$lines=New-Object 'System.Collections.Generic.List[string]'
function Record([string]$Text) { $lines.Add($Text);Write-Output $Text }
function Record-Error([string]$Stage,$ErrorValue) {
    $e=$ErrorValue.Exception
    while ($e) { Record ($Stage+': '+$e.GetType().FullName+' HRESULT=0x'+$e.HResult.ToString('X8')+' '+$e.Message);$e=$e.InnerException }
}
$dataRoot=Join-Path $env:LOCALAPPDATA 'ChinhTaViet\WordAddin'
$report=Join-Path $dataRoot 'load-diagnostic.txt'
$exitCode=1
try {
    Record ('ChinhTaViet load diagnostic '+$manifest.Version+' '+[DateTimeOffset]::Now.ToString('O'))
    Record ('PackageArchitecture='+$manifest.Architecture+'; ProcessArchitecture='+$(if ($is64) {'x64'} else {'x86'})+'; CLR='+[Environment]::Version+'; PS='+$PSVersionTable.PSVersion)
    $identity=[Security.Principal.WindowsIdentity]::GetCurrent()
    try { $principal=New-Object Security.Principal.WindowsPrincipal($identity);Record ('AdministratorRole='+$principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) } finally { $identity.Dispose() }
    Record ('WordActual=NOT_RUN; RunningWordProcesses='+@(Get-Process WINWORD -ErrorAction SilentlyContinue).Count)
    try { $word=Get-WordArchitecture;Record ('DetectedWord='+$word.Architecture+'; Source='+$word.Source) } catch { Record-Error 'DetectWord' $_ }
    $release=(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -ErrorAction SilentlyContinue).Release
    Record ('FrameworkRelease='+$release)
    $target=Get-WordAddinTarget $manifest
    Record ('ExpectedInstallRoot='+$target)
    $views=@([Microsoft.Win32.RegistryView]::Registry32)
    if ([Environment]::Is64BitOperatingSystem) { $views+=[Microsoft.Win32.RegistryView]::Registry64 }
    $owned='Software\Microsoft\Office\Word\Addins\ChinhTaViet.WordAddin.Connect'
    foreach ($view in $views) {
        foreach ($hive in @([Microsoft.Win32.RegistryHive]::CurrentUser,[Microsoft.Win32.RegistryHive]::LocalMachine)) {
            Record ("Registry $hive $view LoadBehavior="+(Read-RegistryValue $hive $view $owned 'LoadBehavior'))
            foreach ($class in (Get-WordAddinClasses)) {
                $path="Software\Classes\CLSID\$($class.Id)\InprocServer32"
                Record ("Registry $hive $view $($class.ProgId) Assembly="+(Read-RegistryValue $hive $view $path 'Assembly')+'; CodeBase='+(Read-RegistryValue $hive $view $path 'CodeBase'))
            }
            foreach ($version in @('16.0','15.0','14.0')) {
                $policy=Read-RegistryValue $hive $view ("Software\Policies\Microsoft\Office\$version\Word\Resiliency\AddinList") 'ChinhTaViet.WordAddin.Connect'
                if ($null -ne $policy) { Record ("ManagedAddinPolicy $hive $view Office$version=$policy") }
            }
        }
    }
    $view=Get-AddinRegistryView $manifest.Architecture
    $user=[Microsoft.Win32.RegistryHive]::CurrentUser
    $registeredRoot=Read-RegistryValue $user $view $owned 'ChinhTaVietInstallRoot'
    if ($registeredRoot -ne $target) { throw 'Đăng ký hiện tại chưa thuộc gói này. Cài đúng gói rồi chạy lại chẩn đoán.' }
    $installedManifest=Get-Content (Join-Path $target 'addin-package.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($installedManifest.Commit -ne $manifest.Commit -or $installedManifest.Version -ne $manifest.Version) { throw 'Thông tin gói đã cài không khớp gói chẩn đoán.' }
    $verified=0
    foreach ($item in $manifest.Files.PSObject.Properties) {
        if ($item.Name.Contains('..') -or [IO.Path]::IsPathRooted($item.Name)) { throw 'Đường dẫn kiểm tệp không hợp lệ.' }
        $file=Join-Path $target $item.Name
        if (!(Test-Path $file -PathType Leaf) -or (Get-FileHash $file -Algorithm SHA256).Hash -ne $item.Value) { throw ('Tệp đã cài thiếu/hỏng: '+$item.Name) }
        $zone=Get-Content -LiteralPath $file -Stream Zone.Identifier -ErrorAction SilentlyContinue
        foreach ($line in $zone) { if ($line -match '^ZoneId=([0-9]+)$') { Record ('DownloadZone '+$item.Name+' '+$line) } }
        $verified++
    }
    if ($verified -eq 0) { throw 'Gói không có danh sách hash để xác minh.' }
    Record ('VerifiedInstalledFiles='+$verified)
    foreach ($class in (Get-WordAddinClasses)) {
        $code=Read-RegistryValue $user $view ("Software\Classes\CLSID\$($class.Id)\InprocServer32") 'CodeBase'
        if ($code -ne ([Uri](Join-Path $target 'ChinhTa.WordAddin.dll')).AbsoluteUri) { throw 'CodeBase không trỏ tới DLL đã kiểm hash; chưa thử kích hoạt.' }
    }
    # No explicit Assembly.LoadFrom fallback: this result means actual COM activation.
    $connector=[Activator]::CreateInstance([type]::GetTypeFromProgID('ChinhTaViet.WordAddin.Connect',$true))
    try {
        $loaded=[AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq 'ChinhTa.WordAddin' }
        if (!$loaded -or $loaded.Location -ne (Join-Path $target 'ChinhTa.WordAddin.dll')) { throw 'COM không nạp DLL từ thư mục đã xác minh.' }
        Record ('NativeCOM=PASS; LoadedVersion='+$loaded.GetName().Version)
        $contract=$loaded.GetType('ChinhTa.WordAddin.IOfficeConnection')
        foreach ($method in $contract.GetMethods()) {
            $parameters=$method.GetParameters();$custom=$parameters[$parameters.Length-1]
            $marshal=$custom.GetCustomAttributes([Runtime.InteropServices.MarshalAsAttribute],$false)
            Record ('Contract '+$method.Name+' In='+$custom.IsIn+' Marshal='+$marshal.Value+' Element='+$marshal.SafeArraySubType)
        }
        $xml=[xml]$connector.GetCustomUI('Microsoft.Word.Document')
        if (!$xml.DocumentElement) { throw 'COM chưa trả được XML Ribbon.' }
        Record 'RibbonXML=PASS; WordRibbonAndTaskPane=NOT_RUN'
        $exitCode=0
    } finally { if ($null -ne $connector -and [Runtime.InteropServices.Marshal]::IsComObject($connector)) { [void][Runtime.InteropServices.Marshal]::ReleaseComObject($connector) } }
} catch { Record-Error 'LoadDiagnostic' $_ }
finally {
    Record ('DiagnosticExit='+$exitCode+'; WordActual=NOT_RUN')
    New-Item $dataRoot -ItemType Directory -Force | Out-Null
    $lines | Set-Content $report -Encoding UTF8
    Write-Output ('Đã lưu báo cáo: '+$report)
    Write-Output 'Báo cáo không đọc tài liệu, vùng chọn hay từ cá nhân. Có đường dẫn cài đặt của tài khoản Windows; bạn có thể che tên tài khoản trước khi gửi.'
}
exit $exitCode
