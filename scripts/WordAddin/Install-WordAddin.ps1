param([switch]$DryRun)
$ErrorActionPreference='Stop'
$source=$PSScriptRoot
$manifestPath=Join-Path $source 'addin-package.json'
if (!(Test-Path $manifestPath)) { throw 'Thiếu thông tin gói Add-in. Giải nén toàn bộ ZIP trước khi cài.' }
$manifest=Get-Content $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.Version -ne '0.1.88') { throw 'Sai phiên bản gói Add-in.' }
foreach ($item in $manifest.Files.PSObject.Properties) {
    if ($item.Name.Contains('..') -or [IO.Path]::IsPathRooted($item.Name)) { throw 'Đường dẫn gói chưa hợp lệ.' }
    $file=Join-Path $source $item.Name
    if (!(Test-Path $file -PathType Leaf) -or (Get-FileHash $file -Algorithm SHA256).Hash -ne $item.Value) { throw "Gói thiếu/hỏng tệp: $($item.Name)" }
}
if ($manifest.Commit -notmatch '^[0-9a-f]{40}$') { throw 'Thiếu định danh mã nguồn của gói.' }
$target=Join-Path $env:LOCALAPPDATA ("ChinhTaViet\WordAddin\0.1.88-"+$manifest.Commit.Substring(0,8))
$classes=@(
    @{Id='{E0382B17-15A1-49D9-AC6A-9322607C6AF7}';ProgId='ChinhTaViet.WordAddin.Connect';Class='ChinhTa.WordAddin.Connect'},
    @{Id='{493F2F97-913D-4E26-8F9C-7B461D073F9F}';ProgId='ChinhTaViet.WordAddin.Pane';Class='ChinhTa.WordAddin.TaskPaneView'}
)
if ($DryRun) { "Verified $(@($manifest.Files.PSObject.Properties).Count) files. Proposed user-only install: $target (Word x64); no registry or files changed."; return }
if (![Environment]::Is64BitOperatingSystem -or ![Environment]::Is64BitProcess) { throw 'Chạy Windows PowerShell 64-bit. Bản thử dành cho Word 64-bit.' }
if (Get-Process WINWORD -ErrorAction SilentlyContinue) { throw 'Đóng các cửa sổ Word trước khi cài. Chương trình không tự đóng hoặc dừng Word.' }
$office=Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Office\ClickToRun\Configuration' -ErrorAction SilentlyContinue
if ($office -and $office.Platform -eq 'x86') { throw 'Máy này dùng Office 32-bit; gói thử hiện dành cho Word 64-bit.' }
$release=(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -ErrorAction SilentlyContinue).Release
if (!$release -or $release -lt 528040) { throw 'Cần .NET Framework 4.8 trước khi dùng Add-in.' }
New-Item $target -ItemType Directory -Force | Out-Null
Get-ChildItem $source | Where-Object Name -ne 'addin-package.json' | Copy-Item -Destination $target -Recurse -Force
Copy-Item $manifestPath $target -Force
foreach ($item in $manifest.Files.PSObject.Properties) {
    if ((Get-FileHash (Join-Path $target $item.Name) -Algorithm SHA256).Hash -ne $item.Value) { throw "Tệp đã cài chưa khớp: $($item.Name)" }
}
$dll=Join-Path $target 'ChinhTa.WordAddin.dll'
$assembly=[Reflection.AssemblyName]::GetAssemblyName($dll).FullName
$codeBase=([Uri]$dll).AbsoluteUri
$registry=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,[Microsoft.Win32.RegistryView]::Registry64)
try {
    foreach ($item in $classes) {
        $key=$registry.CreateSubKey("Software\Classes\CLSID\$($item.Id)")
        $key.SetValue('',$item.Class);$key.Close()
        $key=$registry.CreateSubKey("Software\Classes\CLSID\$($item.Id)\InprocServer32")
        $key.SetValue('','mscoree.dll');$key.SetValue('ThreadingModel','Both');$key.SetValue('Class',$item.Class);$key.SetValue('Assembly',$assembly);$key.SetValue('RuntimeVersion','v4.0.30319');$key.SetValue('CodeBase',$codeBase);$key.Close()
        $key=$registry.CreateSubKey("Software\Classes\CLSID\$($item.Id)\InprocServer32\0.1.88.0")
        $key.SetValue('Class',$item.Class);$key.SetValue('Assembly',$assembly);$key.SetValue('RuntimeVersion','v4.0.30319');$key.SetValue('CodeBase',$codeBase);$key.Close()
        $key=$registry.CreateSubKey("Software\Classes\CLSID\$($item.Id)\ProgId");$key.SetValue('',$item.ProgId);$key.Close()
        $key=$registry.CreateSubKey("Software\Classes\$($item.ProgId)\CLSID");$key.SetValue('',$item.Id);$key.Close()
    }
    $key=$registry.CreateSubKey('Software\Classes\CLSID\{493F2F97-913D-4E26-8F9C-7B461D073F9F}\Control');$key.Close()
    $key=$registry.CreateSubKey('Software\Microsoft\Office\Word\Addins\ChinhTaViet.WordAddin.Connect')
    $key.SetValue('FriendlyName','Chính tả Việt — thử nghiệm 0.1.88');$key.SetValue('Description','Kiểm tra chữ, chọn vị trí để người dùng sửa trong Word; từ điển cá nhân.');$key.SetValue('LoadBehavior',3,[Microsoft.Win32.RegistryValueKind]::DWord);$key.Close()
} finally { $registry.Close() }
"Đã cài Add-in cho người dùng hiện tại. Mở Word, chọn tab Chính tả Việt. Từ cá nhân được giữ ngoài thư mục phiên bản."
