function Get-AddinRegistryView([string]$Architecture) {
    switch ($Architecture) {
        'x86' { return [Microsoft.Win32.RegistryView]::Registry32 }
        'x64' { return [Microsoft.Win32.RegistryView]::Registry64 }
        default { throw 'Kiến trúc gói phải là x86 hoặc x64.' }
    }
}

function Read-ExecutableArchitecture([string]$Path) {
    $stream=[IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
    $reader=New-Object IO.BinaryReader($stream)
    try {
        if ($stream.Length -lt 64 -or $reader.ReadUInt16() -ne 0x5A4D) { throw 'Thiếu đầu tệp thực thi Windows.' }
        $stream.Position=60;$offset=$reader.ReadInt32()
        if ($offset -lt 64 -or $offset -gt $stream.Length-6) { throw 'Đầu tệp thực thi Windows không hợp lệ.' }
        $stream.Position=$offset
        if ($reader.ReadUInt32() -ne 0x4550) { throw 'Sai chữ ký PE của tệp thực thi Windows.' }
        switch ($reader.ReadUInt16()) {
            0x014C { return 'x86' }
            0x8664 { return 'x64' }
            default { throw 'Kiến trúc tệp thực thi chưa được hỗ trợ trong gói thử.' }
        }
    } finally { $reader.Dispose() }
}

function Read-RegistryValue($Hive,$View,[string]$Path,[string]$Name='') {
    $regBase=[Microsoft.Win32.RegistryKey]::OpenBaseKey($Hive,$View)
    $key=$null
    try { $key=$regBase.OpenSubKey($Path);if ($key) { return $key.GetValue($Name) } }
    finally { if ($key) { $key.Close() };$regBase.Close() }
}

function Get-WordArchitecture {
    $views=@([Microsoft.Win32.RegistryView]::Registry32)
    if ([Environment]::Is64BitOperatingSystem) { $views+= [Microsoft.Win32.RegistryView]::Registry64 }
    $userHive=[Microsoft.Win32.RegistryHive]::CurrentUser
    $machineHive=[Microsoft.Win32.RegistryHive]::LocalMachine
    # Windows resolves a per-user App Paths entry before the machine entry.
    foreach ($hive in @($userHive,$machineHive)) {
        $facts=@()
        foreach ($view in $views) {
            $value=Read-RegistryValue $hive $view 'Software\Microsoft\Windows\CurrentVersion\App Paths\WINWORD.EXE'
            if ($value) {
                $path=[Environment]::ExpandEnvironmentVariables(([string]$value).Trim().Trim('"'))
                if ([IO.File]::Exists($path)) { $facts+= [pscustomobject]@{Architecture=(Read-ExecutableArchitecture $path);Source='WINWORD.EXE';Path=$path} }
            }
        }
        if ($facts.Count -gt 0) {
            if (@($facts.Architecture | Select-Object -Unique).Count -ne 1) { throw 'Có nhiều đăng ký Word khác kiến trúc; chưa cài để tránh đăng ký nhầm.' }
            return $facts[0]
        }
    }
    $facts=@();$platforms=@()
    foreach ($view in $views) {
        foreach ($version in @('16.0','15.0','14.0')) {
            foreach ($hive in @($userHive,$machineHive)) {
                $folder=Read-RegistryValue $hive $view ("Software\Microsoft\Office\$version\Word\InstallRoot") 'Path'
                if ($folder) {
                    $path=Join-Path ([Environment]::ExpandEnvironmentVariables([string]$folder)) 'WINWORD.EXE'
                    if ([IO.File]::Exists($path)) { $facts+= [pscustomobject]@{Architecture=(Read-ExecutableArchitecture $path);Source='Word InstallRoot';Path=$path} }
                }
            }
        }
        $config='Software\Microsoft\Office\ClickToRun\Configuration'
        $platform=Read-RegistryValue $machineHive $view $config 'Platform'
        if ($platform -in @('x86','x64')) { $platforms+= [string]$platform }
        $folder=Read-RegistryValue $machineHive $view $config 'InstallationPath'
        if ($folder) {
            foreach ($suffix in @('root\Office16\WINWORD.EXE','Office16\WINWORD.EXE','root\Office15\WINWORD.EXE')) {
                $path=Join-Path ([Environment]::ExpandEnvironmentVariables([string]$folder)) $suffix
                if ([IO.File]::Exists($path)) { $facts+= [pscustomobject]@{Architecture=(Read-ExecutableArchitecture $path);Source='ClickToRun WINWORD.EXE';Path=$path} }
            }
        }
    }
    if ($facts.Count -gt 0) {
        if (@($facts.Architecture | Select-Object -Unique).Count -ne 1) { throw 'Tìm thấy các tệp Word khác kiến trúc; chưa cài để tránh đăng ký nhầm.' }
        return $facts[0]
    }
    $platforms=@($platforms | Select-Object -Unique)
    if ($platforms.Count -eq 1) { return [pscustomobject]@{Architecture=$platforms[0];Source='ClickToRun Platform';Path=''} }
    throw 'Chưa nhận diện chắc Word 32-bit hay 64-bit. Hãy kiểm tra Word > Tệp > Tài khoản > Giới thiệu về Word và giữ thông báo này.'
}

function Assert-WordPackageArchitecture([string]$PackageArchitecture,[string]$OfficeArchitecture) {
    [void](Get-AddinRegistryView $PackageArchitecture)
    if ($PackageArchitecture -ne $OfficeArchitecture) {
        $wordBits=if ($OfficeArchitecture -eq 'x86') { '32' } else { '64' }
        throw "Word đang dùng $wordBits-bit. Hãy tải gói win-$OfficeArchitecture; gói win-$PackageArchitecture hiện tại chưa được cài."
    }
}

function Get-WordAddinTarget($Manifest) {
    return Join-Path $env:LOCALAPPDATA ("ChinhTaViet\WordAddin\0.1.91-"+$Manifest.Commit.Substring(0,8)+'-win-'+$Manifest.Architecture)
}

function Get-WordAddinClasses {
    return @(
        @{Id='{E0382B17-15A1-49D9-AC6A-9322607C6AF7}';ProgId='ChinhTaViet.WordAddin.Connect';Class='ChinhTa.WordAddin.Connect'},
        @{Id='{493F2F97-913D-4E26-8F9C-7B461D073F9F}';ProgId='ChinhTaViet.WordAddin.Pane';Class='ChinhTa.WordAddin.TaskPaneView'}
    )
}

function Get-WordAddinKeys {
    return @('Software\Microsoft\Office\Word\Addins\ChinhTaViet.WordAddin.Connect','Software\Classes\CLSID\{E0382B17-15A1-49D9-AC6A-9322607C6AF7}','Software\Classes\CLSID\{493F2F97-913D-4E26-8F9C-7B461D073F9F}','Software\Classes\ChinhTaViet.WordAddin.Connect','Software\Classes\ChinhTaViet.WordAddin.Pane')
}
