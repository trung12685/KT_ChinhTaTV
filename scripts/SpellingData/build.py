"""Rebuild distributable offline spelling data from pinned public sources.
Data files retain their own GPL-2.0 and CC-BY-SA-4.0 licenses. No user ZIP.
Run: python scripts/SpellingData/build.py --sources /path/to/verified/sources
"""
from pathlib import Path
import argparse, collections, hashlib, json, re, unicodedata, shutil, zipfile
ROOT=Path(__file__).resolve().parents[2]
WORD_COMMIT='688b0953c8cdb21ee090ce80b83cefdb052fe2cd'
UD_COMMIT='3c971fd1c5d03561e00b267d3d0057b81a9bea84'

def tokens(text):
    # Same UTF-16-independent lookup categories as the C# spelling tokenizer.
    result=[];word=''
    for ch in text:
        category=unicodedata.category(ch)
        if category[0]=='L' or category=='Nd' or ch=='_' or word and category[0]=='M':word+=ch
        elif word:result.append(unicodedata.normalize('NFC',word).lower());word=''
    if word:result.append(unicodedata.normalize('NFC',word).lower())
    return result

def build(source,output):
    output.mkdir(parents=True,exist_ok=True)
    word=source/'vietnamese-wordlist';ud=source/'UD_Vietnamese-VTB'
    files={'words.txt':word/'Viet74K.txt','GPL-2.0.txt':word/'LICENSE','WORDLIST-README.md':word/'README.md','CC-BY-SA-4.0.txt':ud/'LICENSE.txt','UD-README.md':ud/'README.md'}
    for dest,src in files.items():shutil.copyfile(src,output/dest)
    sentences=[];seen=set();source_hashes={}
    for split in ('train','dev','test'):
        path=ud/f'vi_vtb-ud-{split}.conllu';source_hashes[path.name]=hashlib.sha256(path.read_bytes()).hexdigest()
        for line in path.read_text(encoding='utf-8').splitlines():
            if not line.startswith('# text ='):continue
            text=line.partition('=')[2].strip();key=unicodedata.normalize('NFC',text).lower()
            if key not in seen:seen.add(key);sentences.append(text)
    # Fixed deterministic holdout excluded from every count, across UD splits.
    train=[];holdout=[]
    for text in sentences:
        (holdout if int(hashlib.sha256(text.encode()).hexdigest()[:8],16)%5==0 else train).append(text)
    uni=collections.Counter();bi=collections.Counter();tri=collections.Counter()
    for text in train:
        ts=tokens(text);uni.update(ts);bi.update(zip(ts,ts[1:]));tri.update(zip(ts,ts[1:],ts[2:]))
    lm={'total':sum(uni.values()),'uni':dict(sorted(uni.items())),'bi':[[*k,v] for k,v in sorted(bi.items())],'tri':[[*k,v] for k,v in sorted(tri.items())]}
    (output/'lm.json').write_text(json.dumps(lm,ensure_ascii=False,separators=(',',':'))+'\n',encoding='utf-8',newline='\n')
    # This is data attribution and change identification, kept beside the assets.
    notice=f'''Offline Vietnamese spelling data v1 / Chính tả Việt 0.1.86

Word list: Viet74K, Hồ Ngọc Đức, redistributed by duyet/vietnamese-wordlist.
Source: https://github.com/duyet/vietnamese-wordlist/tree/{WORD_COMMIT}
License: GNU GPL version 2 (GPL-2.0.txt). words.txt is the unmodified source
word list, also the preferred form for modification. WORDLIST-README.md
retains upstream attribution. The application normalizes lookup keys only.

Context counts: adapted from UD_Vietnamese-VTB, VLSP treebank.
Source: https://github.com/UniversalDependencies/UD_Vietnamese-VTB/tree/{UD_COMMIT}
License: CC BY-SA 4.0, https://creativecommons.org/licenses/by-sa/4.0/
Full license: CC-BY-SA-4.0.txt; upstream attribution: UD-README.md.
Contributors: Nguyễn Thị Lương; Hà Mỹ Linh; Lê Hồng Phương; Nguyễn Thị Minh Huyền.
Reference: Nguyen et al. (2009), Building a Large Syntactically-Annotated
Corpus of Vietnamese, Third Linguistic Annotation Workshop, pp.182–185.
Changes by the Chính tả Việt project: deduplicate sentence text, deterministic
80/20 holdout, NFC/lowercase token lookup, count unigrams/bigrams/trigrams,
omit annotations and sentence text from runtime model. Adapted lm.json is
provided under CC BY-SA 4.0. This is a pinned repository snapshot, not a claim
of a released UD edition or a Vietnamese spelling standard.
Rebuild recipe: scripts/SpellingData/build.py in the application source repo.
No endorsement by the upstream authors. No user documents or uploaded ZIP data.
The data files are separate from application code and retain their own licenses.
'''
    (output/'NOTICE.txt').write_text(notice,encoding='utf-8',newline='\n')
    names=sorted([*files,'lm.json','NOTICE.txt'])
    # Deterministic ZIP keeps the preferred word-list source and readable LM
    # together; runtime reads entries directly, without extracting files.
    with zipfile.ZipFile(output/'default-data.zip','w',compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
        for name in ('words.txt','lm.json'):
            item=zipfile.ZipInfo(name,date_time=(1980,1,1,0,0,0));item.compress_type=zipfile.ZIP_DEFLATED;item.external_attr=0o644<<16
            z.writestr(item,(output/name).read_bytes(),compresslevel=9)
    container_hash=hashlib.sha256((output/'default-data.zip').read_bytes()).hexdigest()
    manifest={'containerSha256':container_hash,'id':'vi-offline-v1','wordlistCommit':WORD_COMMIT,'contextCommit':UD_COMMIT,'sourceHashes':source_hashes,'trainingSentences':len(train),'heldOutSentences':len(holdout),'tokens':lm['total'],'unigrams':len(uni),'bigrams':len(bi),'trigrams':len(tri),'licenses':{'words.txt':'GPL-2.0','lm.json':'CC-BY-SA-4.0'},'files':{name:hashlib.sha256((output/name).read_bytes()).hexdigest() for name in names}}
    (output/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
    (output/'words.txt').unlink();(output/'lm.json').unlink()
    return manifest,holdout

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--sources',type=Path,required=True);p.add_argument('--output',type=Path,default=ROOT/'assets/spelling');p.add_argument('--holdout',type=Path)
    args=p.parse_args();manifest,holdout=build(args.sources,args.output)
    if args.holdout:args.holdout.write_text(json.dumps(holdout,ensure_ascii=False),encoding='utf-8')
    print(json.dumps({k:v for k,v in manifest.items() if k!='files'},ensure_ascii=False))
    print('manifest sha256:',hashlib.sha256((args.output/'manifest.json').read_bytes()).hexdigest())
