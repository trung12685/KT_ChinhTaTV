param([switch]$DryRun)
$ErrorActionPreference='Stop'
$manifest=Get-Content (Join-Path $PSScriptRoot 'addin-package.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.Version -ne '0.1.91' -or $manifest.Architecture -notin @('x86','x64') -or $manifest.Commit -notmatch '^[0-9a-f]{40}$') { throw 'Thiếu thông tin phiên bản/kiến trúc gói để gỡ đúng Add-in.' }
. (Join-Path $PSScriptRoot 'Architecture.ps1')
$keys=Get-WordAddinKeys
$view=Get-AddinRegistryView $manifest.Architecture
$target=Get-WordAddinTarget $manifest
if ($DryRun) { "Would remove only the five owned user COM/Add-in keys in $view for $target. Personal dictionary and Word files retained."; return }
if (Get-Process WINWORD -ErrorAction SilentlyContinue) { throw 'Đóng Word trước khi gỡ. Không tự dừng Word.' }
$registry=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,$view)
try {
    $key=$registry.OpenSubKey($keys[0])
    try { if ($key -and $key.GetValue('ChinhTaVietInstallRoot') -ne $target) { throw 'Đang có phiên bản Add-in khác. Hãy dùng GO-ADDIN.cmd trong gói đang cài để tránh gỡ nhầm.' } }
    finally { if ($key) { $key.Close() } }
    foreach ($item in (Get-WordAddinClasses)) {
        $key=$registry.OpenSubKey("Software\Classes\CLSID\$($item.Id)\InprocServer32")
        try { if ($key -and $key.GetValue('CodeBase') -ne ([Uri](Join-Path $target 'ChinhTa.WordAddin.dll')).AbsoluteUri) { throw 'Lớp COM đang thuộc phiên bản khác; chưa gỡ để tránh ảnh hưởng bản đang dùng.' } }
        finally { if ($key) { $key.Close() } }
    }
    foreach ($path in $keys) { $registry.DeleteSubKeyTree($path,$false) }
} finally { $registry.Close() }
'Đã gỡ đăng ký Add-in. Giữ nguyên từ điển cá nhân, tệp Word đã đồng bộ và các tài liệu.'
