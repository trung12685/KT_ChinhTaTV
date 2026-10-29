import json,pathlib,subprocess,tempfile,shutil,os,struct,xml.etree.ElementTree as ET
root=pathlib.Path(__file__).resolve().parents[2]
dotnet=shutil.which('dotnet')
if dotnet is None: raise SystemExit('Install .NET SDK 8 and build scripts/PreviewMetadata first.')
version=ET.parse(root/'src/ChinhTa.Desktop/ChinhTa.Desktop.csproj').findtext('PropertyGroup/Version')
if not version: raise SystemExit('Missing application version.')
dll=root/'scripts/PreviewMetadata/bin/Release/net8.0/PreviewMetadata.dll'
checks=0
with tempfile.TemporaryDirectory(prefix='preview-metadata-tests-') as temp:
 base=pathlib.Path(temp); folder=base/'package'; folder.mkdir()
 required=['ChinhTa.Desktop.exe','ChinhTa.Desktop.dll','ChinhTa.Core.dll','ChinhTa.Desktop.deps.json','hostfxr.dll','hostpolicy.dll','coreclr.dll','PresentationFramework.dll','WindowsBase.dll']
 executable=bytearray(128);executable[:2]=b'MZ';struct.pack_into('<I',executable,0x3c,64);executable[64:68]=b'PE\x00\x00';struct.pack_into('<H',executable,68,0x8664)
 for name in required:(folder/name).write_bytes(executable if name.endswith('.exe') else b'fixture')
 config={'runtimeOptions':{'includedFrameworks':[{'name':'Microsoft.WindowsDesktop.App','version':'8.0.0'}]}}
 (folder/'ChinhTa.Desktop.runtimeconfig.json').write_text(json.dumps(config))
 (folder/'samples').mkdir(); (folder/'samples/sample.docx').write_bytes(b'synthetic-docx-payload')
 (folder/'KIEM-TRA-WORD.cmd').write_text('@echo off\n')
 evidence={'Version':version,'CoreChecks':123,'WpfChecks':45,'Commit':'a'*40,'RunUrl':'https://github.com/trung12685/KT_ChinhTaTV/actions/runs/123'}
 (folder/'build-checks.json').write_text(json.dumps(evidence))
 guide=base/f'PREVIEW-{version}.md';guide.write_text('# Synthetic preview guide\nWord NOT_RUN\n')
 def run(command,*extra,ok=True,target=folder):
  global checks
  p=subprocess.run([dotnet,str(dll),command,str(target),version,*map(str,extra)],capture_output=True,text=True)
  if (p.returncode==0)!=ok:raise AssertionError(f'{command} expected ok={ok}, got {p.returncode}: {p.stdout}\n{p.stderr}')
  checks+=1
 run('prepare',guide)
 run('manifest')
 run('verify')
 metadata=json.loads((folder/'preview-manifest.json').read_text(encoding='utf-8-sig'))
 assert metadata['WordRuntime']=='NOT_RUN' and metadata['AutomaticallyRepairsExactlyAtLeast'] is False; checks+=1
 assert '| C01 | Khoảng trắng thừa |' in (folder/f'SUPPORTED-FIXES-{version}.md').read_text(encoding='utf-8-sig'); checks+=1
 pristine=base/'pristine';shutil.copytree(folder,pristine)
 def restore():
  shutil.rmtree(folder);shutil.copytree(pristine,folder)
 (folder/'ChinhTa.Core.dll').write_bytes(b'changed')
 run('verify',ok=False);restore()
 (folder/'unexpected.txt').write_text('unlisted')
 run('verify',ok=False);restore()
 (folder/'samples/sample.docx').unlink()
 run('verify',ok=False);restore()
 # Regeneration must not silently overwrite an already prepared package.
 run('prepare',guide,ok=False);restore()
 # Evidence drift is not legitimized by current version alone.
 changed=dict(evidence,CoreChecks=124)
 (folder/'build-checks.json').write_text(json.dumps(changed))
 run('verify',ok=False);restore()
 (folder/f'ChinhTaViet-{version}-tien-do.txt').write_text('stale report')
 run('verify',ok=False);restore()
 report=folder/f'ChinhTaViet-{version}-tien-do.txt';text=report.read_text(encoding='utf-8-sig');report.write_text(text.replace('Core/OpenXML','wrong-label'))
 run('verify',ok=False);restore()
 (folder/f'SUPPORTED-FIXES-{version}.md').write_text('missing capability list')
 run('verify',ok=False);restore()
 (folder/'KIEM-TRA-WORD.cmd').unlink()
 run('verify',ok=False);restore()
 m=folder/'FILES.sha256';m.write_text('0'*64+'  ../escape\n')
 run('verify',ok=False);restore()
 # Link rejection must happen before recursive descent.
 try:
  (folder/'link').symlink_to(base,target_is_directory=True)
 except OSError:
  print('SKIP symlink creation: runner does not grant symbolic-link creation.')
 else:
  run('verify',ok=False);restore()
 config['runtimeOptions']={'frameworks':[{'name':'Microsoft.WindowsDesktop.App','version':'8.0.0'}]}
 (folder/'ChinhTa.Desktop.runtimeconfig.json').write_text(json.dumps(config))
 run('verify',ok=False);restore()
 (folder/'ChinhTa.Desktop.exe').write_bytes(b'not-windows')
 run('verify',ok=False);restore()
 changed=bytearray(executable);struct.pack_into('<H',changed,68,0xaa64)
 (folder/'ChinhTa.Desktop.exe').write_bytes(changed)
 run('verify',ok=False);restore()
 changed=bytearray(executable);struct.pack_into('<I',changed,0x3c,0xffffffff)
 (folder/'ChinhTa.Desktop.exe').write_bytes(changed)
 run('verify',ok=False);restore()
 (folder/'hostfxr.dll').unlink()
 run('verify',ok=False);restore()
 # Zero tests never qualify as build evidence.
 (folder/'build-checks.json').write_text(json.dumps(dict(evidence,WpfChecks=0)))
 run('verify',ok=False);restore()
 # A tampered assertion remains invalid even with a freshly rehashed manifest.
 (folder/'FILES.sha256').unlink();(folder/f'ChinhTaViet-{version}-tien-do.txt').write_text('tampered report')
 run('manifest',ok=False)
 print(f'{checks} preview-metadata checks passed (synthetic payloads only; no Windows/Word execution).')
