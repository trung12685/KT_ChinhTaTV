using ChinhTa.Core;

// Reproduce the desktop's bounded native observation: after ordinal40, Page
// and InTable are placeholders. Current Word/XML proof still covers the body.
public static class SignatureNativeBoundaryChecks
{
    public static void Run(Action<bool,string> assert)
    {
        foreach(int padding in new[]{0,35,39,40,41,60,150})
        foreach(bool shared in new[]{false,true})
        foreach(bool soft in new[]{false,true})
        {
            var sample=SignatureChecks.Sample(soft,padding);
            var native=sample.Rows.Select((p,i)=>p with{InTable=i<40 && p.InTable,Page=i<40?1:0,
                Outline=null,TextEditable=true,FormatEditable=false,FormatObserved=false}).ToArray();
            var live=native.Select(p=>new WordParagraphSnapshot(p.Start,p.Start+p.Text.Length,p.Text,p.Page,p.InTable,false)).ToArray();
            var outline=new Dictionary<int,OutlineParagraphInfo>();var formats=new HashSet<int>();
            void Describe(int start,OutlineParagraphInfo info)=>outline[start]=info;
            void Format(int start,bool can){if(can)formats.Add(start);}
            var editable=shared?WordTextProof.Read(WordXmlSnapshot.Parse(sample.Xml.ToString()),live,onOutline:Describe,onFormatEditable:Format):
                WordTextProof.Read(sample.Xml.ToString(),live,onOutline:Describe,onFormatEditable:Format);
            var rows=WordTextProof.Apply(native,editable,outline,formats).ToArray();
            var targets=SignatureRules.ObservationTargets(rows,DocumentProfile.State);
            assert(outline.Values.Count(x=>x.Scope==OutlineScope.Signature)==2 && targets.Count==2,
                $"Current XML signature proof survives bounded native table flags (padding={padding}, shared={shared}, soft={soft}, recognized={targets.Count})");
            assert(rows.Zip(native).All(x=>x.First.InTable==x.Second.InTable && x.First.Page==x.Second.Page),
                "Using a proven signature role does not manufacture native table geometry or page numbers");
            var observed=rows.Select(p=>targets.Any(t=>t.Start==p.Start)?p with{Font="Arial",Size=12,Color=255,Italic=true,Bold=false,FormatObserved=true}:p).ToArray();
            var issues=SignatureRules.Check(observed,DocumentProfile.State);
            assert(issues.Count==2 && issues.All(i=>i.Kind==FixKind.SignatureFormat && i.CanFix && i.Original==i.Replacement),
                "Both current signature roles receive content-preserving format proposals on either side of ordinal40");
            var corrected=observed.Select(p=>targets.Any(t=>t.Start==p.Start)?p with{Font="Times New Roman",Size=13,Color=0,Italic=false,Bold=true}:p).ToArray();
            assert(SignatureRules.Check(corrected,DocumentProfile.State).Count==0,
                "A fresh long-document signature scan removes the corrected findings");
            foreach(var role in new[]{ComponentRole.SignatureAuthority,ComponentRole.SignatureName})
            {
                var blocked=rows.Select(p=>p.Outline?.SignatureRole==role?p with{TextEditable=false,FormatEditable=false}:p).ToArray();
                assert(SignatureRules.ObservationTargets(blocked,DocumentProfile.State).Count==0,
                    "The full signing group still needs current edit/format proof for each role");
            }
            var signer=targets.Single(t=>t.Outline!.SignatureRole==ComponentRole.SignatureName);
            var stale=live.Select(p=>p.Start==signer.Start?p with{Text="Khác nội dung\r"}:p).ToArray();
            outline.Clear();formats.Clear();
            var staleEditable=WordTextProof.Read(sample.Xml.ToString(),stale,_=>{},Describe,Format);
            var staleRows=WordTextProof.Apply(native,staleEditable,outline,formats);
            assert(SignatureRules.ObservationTargets(staleRows,DocumentProfile.State).Count==0,
                "A mismatch before the name does not borrow the XML role to authorize the partially proven authority");
        }
    }
}
