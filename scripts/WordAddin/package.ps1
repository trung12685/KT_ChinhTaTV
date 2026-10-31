param([Parameter(Mandatory=$true)][string]$Output)
$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
New-Item $Output -ItemType Directory -Force | Out-Null
$Output=(Resolve-Path $Output).Path
$hostProject=Join-Path $root 'src/ChinhTa.WordAddin/ChinhTa.WordAddin.csproj'
$workerProject=Join-Path $root 'src/ChinhTa.WordWorker/ChinhTa.WordWorker.csproj'
dotnet build $hostProject -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet publish $workerProject -c Release -r win-x64 --self-contained true -o (Join-Path $Output 'worker')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Copy-Item (Join-Path $root 'src/ChinhTa.WordAddin/bin/Release/net48/ChinhTa.WordAddin.dll') $Output
Copy-Item (Join-Path $root 'src/ChinhTa.WordAddin/bin/Release/net48/ChinhTa.WordProtocol.dll') $Output
Copy-Item (Join-Path $PSScriptRoot 'Install-WordAddin.ps1'),(Join-Path $PSScriptRoot 'Uninstall-WordAddin.ps1'),(Join-Path $PSScriptRoot 'CAI-ADDIN.cmd'),(Join-Path $PSScriptRoot 'GO-ADDIN.cmd') $Output
Copy-Item (Join-Path $root 'docs/WORD-ADDIN-0.1.88.md') $Output
if (Test-Path (Join-Path $Output 'Office.dll')) { throw 'Office PIA must be embedded, not redistributed with the preview' }
# Run the actual self-contained worker with isolated first-party input.
$probeRoot=Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString())
$process=New-Object System.Diagnostics.Process
try {
    $process.StartInfo=New-Object System.Diagnostics.ProcessStartInfo
    $process.StartInfo.FileName=Join-Path $Output 'worker/ChinhTa.WordWorker.exe'
    $process.StartInfo.Arguments='--test-stdio "'+$probeRoot+'"'
    $process.StartInfo.UseShellExecute=$false;$process.StartInfo.CreateNoWindow=$true
    $process.StartInfo.RedirectStandardInput=$true;$process.StartInfo.RedirectStandardOutput=$true;$process.StartInfo.RedirectStandardError=$true
    $process.StartInfo.StandardOutputEncoding=[Text.Encoding]::UTF8
    [void]$process.Start()
    $text='Mục'+"`t"+'Cần sử lý.'+"`r"+[char]7
    $request=@{Version=2;Id=[Guid]::NewGuid().ToString();Operation='scan';DocumentId=[Guid]::NewGuid().ToString();SnapshotHash=('A'*64);Paragraphs=@(@{Index=1;Start=100;End=(99+$text.Length);Text=$text;InTable=$true})} | ConvertTo-Json -Depth 6 -Compress -EscapeHandling EscapeNonAscii
    $process.StandardInput.WriteLine($request);$process.StandardInput.Close()
    if (!$process.WaitForExit(20000)) { $process.Kill();throw 'Packaged worker smoke timed out' }
    $reply=$process.StandardOutput.ReadToEnd() | ConvertFrom-Json
    if ($process.ExitCode -ne 0 -or !$reply.Success -or $reply.Version -ne 2 -or $reply.ScannedTableParagraphs -ne 1 -or $reply.ScannedSegments -ne 2 -or $reply.Source -notlike 'Bộ kèm ứng dụng v1*' -or @($reply.Findings).Count -ne 1 -or $reply.Findings[0].Original -ne 'sử' -or $reply.Findings[0].Offset -ne $text.IndexOf('sử')) { throw ('Packaged worker failed to load its dictionary and analyze a table/tab paragraph with string offsets: '+$process.StandardError.ReadToEnd()) }
    'Packaged worker table/dictionary/stdio smoke passed; Word NOT_RUN.'
} finally { $process.Dispose();if (Test-Path $probeRoot) { Remove-Item $probeRoot -Recurse -Force } }
$files=[ordered]@{}
Get-ChildItem $Output -Recurse -File | Sort-Object FullName | ForEach-Object { $relative=$_.FullName.Substring($Output.Length+1).Replace('\','/');$files[$relative]=(Get-FileHash $_.FullName -Algorithm SHA256).Hash }
@{Version='0.1.88';Word='NOT_RUN';Commit=$env:GITHUB_SHA;Files=$files} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $Output 'addin-package.json') -Encoding UTF8
& (Join-Path $Output 'Install-WordAddin.ps1') -DryRun
& (Join-Path $Output 'Uninstall-WordAddin.ps1') -DryRun
