"""Prepare a local real-error development audit, without redistributing source text."""
from pathlib import Path
import argparse,collections,hashlib,itertools,json,re,unicodedata,urllib.request
from prepare import fold,utf16,load_corpus

COMMIT='82e68012bac9d6a14e9b517ba449c9952d8fa0da'
SHA256='7034f103f7be6a9dddbae12222442926da74b1e3ab4634fcde85dc5e548afbc9'
URL=f'https://raw.githubusercontent.com/VSEC2021/VSEC/{COMMIT}/Dataset/VSEC.jsonl'

def key(text):
    return ' '.join(unicodedata.normalize('NFC',text).lower().split())

def category(original,target):
    a=unicodedata.normalize('NFC',original);b=unicodedata.normalize('NFC',target)
    if not b.strip():return 'deletion'
    if a.lower()==b.lower():return 'capitalization'
    if len(a.split())!=len(b.split()):return 'word_boundaries'
    if fold(a).lower()==fold(b).lower():
        # Adding only missing combining marks/strokes differs from replacing an existing mark.
        def letters(s):
            result=[]
            for c in unicodedata.normalize('NFD',s.lower()):
                if unicodedata.combining(c) and result:result[-1][1].add(c)
                else:result.append([c,set()])
            return result
        x,y=letters(a),letters(b)
        adds_only=len(x)==len(y) and all((p==q or p=='d' and q=='đ') and m<=n for (p,m),(q,n) in zip(x,y))
        return 'missing_diacritics' if adds_only else 'wrong_diacritics'
    if not any(c.isalpha() for c in a+b):return 'punctuation_or_symbol'
    return 'letters_or_other'

def align(row):
    """Use exact whitespace-token alignment; never search for the nearest repeated word."""
    text=row.get('text');annotations=row.get('annotations')
    if not isinstance(text,str) or not isinstance(annotations,list):raise ValueError('invalid_schema')
    spans=list(re.finditer(r'\S+',text));nonempty=[]
    for index,a in enumerate(annotations,1):
        if a.get('id')!=index or type(a.get('is_correct')) is not bool or not isinstance(a.get('current_syllable'),str):raise ValueError('invalid_annotation')
        alternatives=a.get('alternative_syllables')
        if not isinstance(alternatives,list) or any(not isinstance(s,str) for s in alternatives):raise ValueError('invalid_alternatives')
        if not a['current_syllable']:
            if not a['is_correct'] or alternatives:raise ValueError('unbound_empty_error')
            continue
        nonempty.append(a)
    if len(spans)!=len(nonempty) or any(m.group()!=a['current_syllable'] for m,a in zip(spans,nonempty)):raise ValueError('unaligned_text')
    edits=[];correct=[]
    for m,a in zip(spans,nonempty):
        original=m.group();start=m.start();alternatives=list(dict.fromkeys(a['alternative_syllables']))
        if a['is_correct']:
            if alternatives:raise ValueError('correct_with_replacements')
            correct.append(dict(Start=utf16(text[:start]),Length=utf16(original),Original=original));continue
        if not alternatives or len(alternatives)>8:raise ValueError('missing_or_excess_alternatives')
        if any(unicodedata.normalize('NFC',s)==unicodedata.normalize('NFC',original) for s in alternatives):raise ValueError('original_is_an_accepted_answer')
        # Remove common punctuation only. The wrong lexical token remains the detection unit.
        while original and all(s and s[0]==original[0] for s in alternatives) and not original[0].isalnum():
            original=original[1:];alternatives=[s[1:] for s in alternatives];start+=1
        while original and all(s and s[-1]==original[-1] for s in alternatives) and not original[-1].isalnum():
            original=original[:-1];alternatives=[s[:-1] for s in alternatives]
        if not original:raise ValueError('empty_error_span')
        groups={category(original,s) for s in alternatives}
        edits.append(dict(Start=utf16(text[:start]),Length=utf16(original),Original=original,Target=alternatives[0],
            AlternativeTargets=alternatives[1:],Group=next(iter(groups)) if len(groups)==1 else 'multiple_answer_types',_Start=start,_Length=len(original)))
    if not edits:raise ValueError('no_error_targets')
    if __import__('math').prod(1+len(e['AlternativeTargets']) for e in edits)>256:raise ValueError('too_many_combined_answers')
    corrected=[]
    for variants in itertools.product(*[[e['Target']]+e['AlternativeTargets'] for e in edits]):
        value=text
        for e,target in reversed(list(zip(edits,variants))):value=value[:e['_Start']]+target+value[e['_Start']+e['_Length']:]
        corrected.append(key(value))
    for e in edits:del e['_Start'],e['_Length']
    return edits,correct,sorted(set(corrected))

def prepare(source,sources,output,fetch=False):
    if not source.exists() and fetch:
        source.parent.mkdir(parents=True,exist_ok=True)
        with urllib.request.urlopen(URL,timeout=60) as response:source.write_bytes(response.read())
    raw=source.read_bytes()
    if hashlib.sha256(raw).hexdigest()!=SHA256:raise ValueError('Pinned VSEC hash mismatch')
    _,source_hashes,train,old_audit=load_corpus(sources,fetch)
    seen_train={key(s) for s in train};seen_audit={key(s) for s in old_audit}
    stats=collections.Counter();valid=[]
    for number,line in enumerate(raw.decode('utf-8-sig').splitlines(),1):
        if not line.strip():continue
        stats['SourceRows']+=1
        row=json.loads(line)
        try:edits,correct,answers=align(row)
        except ValueError as error:stats['Rejected_'+str(error)]+=1;continue
        keys=set(answers+[key(row['text'])])
        if keys & seen_train:stats['ExcludedTrainingOverlap']+=1;continue
        if keys & seen_audit:stats['ExcludedPriorAuditOverlap']+=1;continue
        valid.append(dict(Text=row['text'],Edits=edits,CorrectSpans=correct,_Keys=keys,_Line=number))
    # Union original/corrected duplicates, including different accepted answers,
    # before splitting. No related sentence may cross the development/reserve boundary.
    parent=list(range(len(valid)));owners={}
    def find(i):
        while parent[i]!=i:parent[i]=parent[parent[i]];i=parent[i]
        return i
    for i,row in enumerate(valid):
        for value in row['_Keys']:
            if value in owners:parent[find(i)]=find(owners[value])
            else:owners[value]=i
    clusters=collections.defaultdict(list)
    for i in range(len(valid)):clusters[find(i)].append(i)
    selected=[];reserved_keys=set();development_keys=set()
    for members in clusters.values():
        keys={value for i in members for value in valid[i]['_Keys']}
        fingerprint=hashlib.sha256('\n'.join(sorted(keys)).encode()).hexdigest()
        reserved=int(fingerprint[:8],16)%5==0
        (reserved_keys if reserved else development_keys).update(keys)
        for i in members:
            if reserved:stats['ReservedRows']+=1;stats['ReservedTargets']+=len(valid[i]['Edits']);continue
            row=valid[i];ident=hashlib.sha256(row['Text'].encode()).hexdigest()[:24]
            selected.append(dict(Id='vsec-dev-'+ident,Kind='external_real_error',Group='vsec_development',Terms=[],
                Text=row['Text'],Edits=row['Edits'],CorrectSpans=row['CorrectSpans']))
    if development_keys & reserved_keys:raise ValueError('Partition contamination')
    if len({r['Id'] for r in selected})!=len(selected):raise ValueError('Duplicate source IDs')
    selected.sort(key=lambda r:r['Id']);body=''.join(json.dumps(r,ensure_ascii=False)+'\n' for r in selected)
    stats['DevelopmentRows']=len(selected);stats['DevelopmentTargets']=sum(len(r['Edits']) for r in selected);stats['RelatedSentenceGroups']=len(clusters)
    metadata=dict(Schema=2,Corpus='VSEC real-error development audit',Commit=COMMIT,SourceSHA256=SHA256,TrainingSourceHashes=source_hashes,
        Counts=dict(stats),KindCounts={'external_real_error':len(selected)},CasesSHA256=hashlib.sha256(body.encode()).hexdigest(),
        ErrorGroupCounts=dict(collections.Counter(e['Group'] for row in selected for e in row['Edits'])),
        ExactTrainingOverlap=0,PriorAuditOverlap=0,ReserveEvaluated=False,PartitionRule='Union all original/corrected equivalents; SHA256(sorted keys) first8 mod5 ==0 reserved',
        RuntimeCountsRebuiltAndMatched=True,IndependentBlindGoldAvailable=False,PreviouslyUsedForDevelopment=True,
        SourceRedistributed=False,ModelTrainedOnThisSource=False,License='Not specified in the pinned upstream repository; source text is not redistributed or bundled.',
        Limitations=['Upstream manual labels have not been independently double-reviewed by this project.',
            'Only sentence-level duplicate isolation is possible; original document boundaries are unavailable.',
            'Agreement with upstream error labels is not certification of production precision.',
            'No error-free source sentences exist in this dataset; correct-token spans within erroneous sentences are a separate diagnostic.',
            'Development results may guide implementation; reserved rows are neither exported nor scored.'])
    output.mkdir(parents=True,exist_ok=True);(output/'cases.jsonl').write_text(body,encoding='utf-8',newline='\n')
    (output/'metadata.json').write_text(json.dumps(metadata,ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
    print(json.dumps(dict(Counts=metadata['Counts'],ErrorGroupCounts=metadata['ErrorGroupCounts'],ReserveEvaluated=False)))

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--source',type=Path,required=True);p.add_argument('--training-sources',type=Path,required=True)
    p.add_argument('--output',type=Path,required=True);p.add_argument('--fetch',action='store_true');a=p.parse_args();prepare(a.source,a.training_sources,a.output,a.fetch)
