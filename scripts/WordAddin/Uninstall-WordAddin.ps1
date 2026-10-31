param([switch]$DryRun)
$ErrorActionPreference='Stop'
$keys=@('Software\Microsoft\Office\Word\Addins\ChinhTaViet.WordAddin.Connect','Software\Classes\CLSID\{E0382B17-15A1-49D9-AC6A-9322607C6AF7}','Software\Classes\CLSID\{493F2F97-913D-4E26-8F9C-7B461D073F9F}','Software\Classes\ChinhTaViet.WordAddin.Connect','Software\Classes\ChinhTaViet.WordAddin.Pane')
if ($DryRun) { 'Would remove only the five owned user COM/Add-in keys. Personal dictionary and Word files retained.'; return }
if (Get-Process WINWORD -ErrorAction SilentlyContinue) { throw 'Đóng Word trước khi gỡ. Không tự dừng Word.' }
$registry=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,[Microsoft.Win32.RegistryView]::Registry64)
try { foreach ($path in $keys) { $registry.DeleteSubKeyTree($path,$false) } } finally { $registry.Close() }
'Đã gỡ đăng ký Add-in. Giữ nguyên từ điển cá nhân, tệp Word đã đồng bộ và các tài liệu.'
