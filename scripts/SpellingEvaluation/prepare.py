"""Reproduce a development audit from pinned sources; never train on its rows."""
from pathlib import Path
import argparse, collections, hashlib, json, re, unicodedata, urllib.request, zipfile

ROOT=Path(__file__).resolve().parents[2]
COMMIT='3c971fd1c5d03561e00b267d3d0057b81a9bea84'

def fold(text):
    return ''.join(c for c in unicodedata.normalize('NFD',text.replace('đ','d').replace('Đ','D')) if not unicodedata.combining(c))

def utf16(text):
    return len(text.encode('utf-16-le'))//2

def prepare(sources,output,fetch=False):
    output.mkdir(parents=True,exist_ok=True)
    manifest=json.loads((ROOT/'assets/spelling/manifest.json').read_text(encoding='utf-8'))
    source_hashes={};sentences=[];seen=set()
    for split in ('train','dev','test'):
        name=f'vi_vtb-ud-{split}.conllu';path=sources/name
        if not path.exists() and fetch:
            sources.mkdir(parents=True,exist_ok=True)
            with urllib.request.urlopen(f'https://raw.githubusercontent.com/UniversalDependencies/UD_Vietnamese-VTB/{COMMIT}/{name}') as response:
                path.write_bytes(response.read())
        raw=path.read_bytes();digest=hashlib.sha256(raw).hexdigest()
        if digest!=manifest['sourceHashes'][name]:raise ValueError('Pinned corpus hash mismatch: '+name)
        source_hashes[name]=digest
        for line in raw.decode('utf-8').splitlines():
            if not line.startswith('# text ='):continue
            text=line.partition('=')[2].strip();key=unicodedata.normalize('NFC',text).lower()
            if key not in seen:seen.add(key);sentences.append(text)
    train=[];audit=[]
    for text in sentences:
        (audit if int(hashlib.sha256(text.encode()).hexdigest()[:8],16)%5==0 else train).append(text)
    if len(train)!=manifest['trainingSentences'] or len(audit)!=manifest['heldOutSentences']:raise ValueError('Split mismatch')
    # Recheck every count against the advertised split, not just sentence IDs.
    import sys
    sys.path.insert(0,str(ROOT/'scripts/SpellingData'))
    from build import tokens
    uni=collections.Counter();bi=collections.Counter();tri=collections.Counter()
    for text in train:
        ts=tokens(text);uni.update(ts);bi.update(zip(ts,ts[1:]));tri.update(zip(ts,ts[1:],ts[2:]))
    with zipfile.ZipFile(ROOT/'assets/spelling/default-data.zip') as z:lm=json.loads(z.read('lm.json'))
    if dict(uni)!=lm['uni'] or dict(bi)!={(a,b):n for a,b,n in lm['bi']} or dict(tri)!={(a,b,c):n for a,b,c,n in lm['tri']}:raise ValueError('Runtime LM contains different counts/split')
    fixture=ROOT/'tests/data/spelling-quality/developer-fixtures.jsonl'
    rows=[json.loads(line) for line in fixture.read_text(encoding='utf-8').splitlines() if line.strip()]
    for text in audit:
        ident=hashlib.sha256(text.encode()).hexdigest()[:16]
        rows.append(dict(Id='audit-clean-'+ident,Group='unannotated_news',Kind='unannotated_corpus',Text=text,Edits=[],Terms=[]))
        eligible=[m for m in re.finditer(r'[^\W\d_]+',text,re.UNICODE) if len(m.group())>=2 and fold(m.group())!=m.group()]
        if not eligible:continue
        chosen=min(eligible,key=lambda m:hashlib.sha256((ident+':'+str(m.start())).encode()).digest())
        original=chosen.group();changed=fold(original);noisy=text[:chosen.start()]+changed+text[chosen.end():]
        rows.append(dict(Id='audit-drop-'+ident,Group='synthetic_drop_marks',Kind='synthetic_counterfactual',Text=noisy,
            Edits=[dict(Start=utf16(text[:chosen.start()]),Length=utf16(changed),Original=changed,Target=original)],Terms=[]))
    if len({r['Id'] for r in rows})!=len(rows):raise ValueError('Duplicate evaluation ID')
    body=''.join(json.dumps(row,ensure_ascii=False)+'\n' for row in rows)
    (output/'cases.jsonl').write_text(body,encoding='utf-8',newline='\n')
    metadata=dict(Schema=1,Corpus='UD Vietnamese-VTB',Commit=COMMIT,SourceHashes=source_hashes,
        TrainingSentences=len(train),AuditSentences=len(audit),ExactTrainingOverlap=0,RuntimeCountsRebuiltAndMatched=True,
        PreviouslyUsedForDevelopment=True,IndependentBlindGoldAvailable=False,
        FixtureSHA256=hashlib.sha256(fixture.read_bytes()).hexdigest(),CasesSHA256=hashlib.sha256(body.encode()).hexdigest(),
        KindCounts=dict(collections.Counter(r['Kind'] for r in rows)),
        Limitations=['Developer fixtures are not independent human annotation.','Unannotated news warnings are not automatically false positives.',
            'Synthetic edits are counterfactual stress cases, not verified real misspellings.','This historical holdout was used before; it is a development audit, not a fresh blind test.'],
        License='CC-BY-SA-4.0 for adapted news audit; see assets/spelling/NOTICE.txt and CC-BY-SA-4.0.txt')
    (output/'metadata.json').write_text(json.dumps(metadata,ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
    print(json.dumps({k:metadata[k] for k in ['TrainingSentences','AuditSentences','ExactTrainingOverlap','RuntimeCountsRebuiltAndMatched','IndependentBlindGoldAvailable','KindCounts']},ensure_ascii=False))

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--sources',type=Path,required=True);p.add_argument('--output',type=Path,required=True);p.add_argument('--fetch',action='store_true');a=p.parse_args();prepare(a.sources,a.output,a.fetch)
