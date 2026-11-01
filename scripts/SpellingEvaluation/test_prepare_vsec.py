"""First-party fixtures only: no upstream sentences are redistributed here."""
import hashlib,json,tempfile,unittest
from pathlib import Path
from unittest.mock import patch
import prepare_vsec as subject

def row(text,parts):
    return {'text':text,'annotations':[dict(id=i,current_syllable=s,is_correct=answers is None,alternative_syllables=answers or []) for i,(s,answers) in enumerate(parts,1)]}

class ImportChecks(unittest.TestCase):
    def test_emoji_offsets_and_alternatives(self):
        edits,correct,answers=subject.align(row('😀 Hom qua.',[('😀',None),('Hom',['Hôm','Hòm']),('qua.',None)]))
        self.assertEqual((edits[0]['Start'],edits[0]['Length']),(3,3));self.assertEqual(edits[0]['AlternativeTargets'],['Hòm'])
        self.assertEqual(len(answers),2);self.assertEqual(correct[1]['Original'],'qua.')
    def test_correct_empty_annotations_are_not_fabricated_errors(self):
        edits,_,_=subject.align(row('Hom  qua',[('Hom',['Hôm']),('',None),('qua',None)]));self.assertEqual(edits[0]['Original'],'Hom')
    def test_deletion_and_boundary_answers(self):
        edits,_,_=subject.align(row('thừa netu',[('thừa',['']),('netu',['nét từ'])]));self.assertEqual([e['Group'] for e in edits],['deletion','word_boundaries'])
    def test_shared_punctuation_does_not_shift_wrong_word(self):
        edits,_,_=subject.align(row('(Hom),',[('(Hom),',['(Hôm),'])]));self.assertEqual((edits[0]['Start'],edits[0]['Original'],edits[0]['Target']),(1,'Hom','Hôm'))
    def test_missing_marks_and_replaced_marks_are_distinct(self):
        self.assertEqual(subject.category('da','đã'),'missing_diacritics');self.assertEqual(subject.category('sữa','sửa'),'wrong_diacritics')
        self.assertEqual(subject.category('Hôm','hôm'),'capitalization')
    def test_case_only_correction_is_not_rejected_as_identical(self):
        edits,_,_=subject.align(row('hôm',[('hôm',['Hôm'])]));self.assertEqual(edits[0]['Group'],'capitalization')
    def test_repeated_words_cannot_trigger_nearest_match_guessing(self):
        with self.assertRaisesRegex(ValueError,'unaligned_text'):subject.align(row('hom hom',[('hom',['hôm']),('hôm',None)]))
    def test_original_accepted_as_answer_is_not_a_certain_error(self):
        with self.assertRaisesRegex(ValueError,'original_is_an_accepted_answer'):subject.align(row('Hom',[('Hom',['Hom','Hôm'])]))
    def test_wrong_empty_annotation_is_rejected(self):
        with self.assertRaisesRegex(ValueError,'unbound_empty_error'):subject.align(row('Hom',[('',['x']),('Hom',None)]))
    def test_partition_groups_alternative_equivalents_and_excludes_training(self):
        rows=[row('Hom qua',[('Hom',['Hôm','Hòm']),('qua',None)]),row('Hòm qua',[('Hòm',['Hôm']),('qua',None)]),row('Thuc hiện',[('Thuc',['Thực']),('hiện',None)])]
        # Add enough unrelated first-party rows to exercise both deterministic partitions.
        rows += [row('ma '+str(i),[('ma',['má']), (str(i),None)]) for i in range(50)]
        raw=''.join(json.dumps(r,ensure_ascii=False)+'\n' for r in rows).encode()
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp)
            source=root/'local.jsonl';source.write_bytes(raw);output=root/'output'
            with patch.object(subject,'SHA256',hashlib.sha256(raw).hexdigest()),patch.object(subject,'load_corpus',return_value=({}, {},['Thực hiện'],[])):
                subject.prepare(source,root,output)
            metadata=json.loads((output/'metadata.json').read_text(encoding='utf-8'));cases=[json.loads(s) for s in (output/'cases.jsonl').read_text(encoding='utf-8').splitlines()]
            self.assertEqual(metadata['Counts']['ExcludedTrainingOverlap'],1);self.assertFalse(metadata['ReserveEvaluated']);self.assertGreater(metadata['Counts']['ReservedRows'],0)
            included={r['Text'] for r in cases};self.assertEqual('Hom qua' in included,'Hòm qua' in included)
            self.assertNotIn('Thuc hiện',included);self.assertEqual(hashlib.sha256((output/'cases.jsonl').read_bytes()).hexdigest(),metadata['CasesSHA256'])

if __name__=='__main__':unittest.main()
