using ChinhTa.Core;

public static class LineReferenceBindingChecks
{
    public static void Run(Action<bool,string> assert)
    {
        const string story="CƠ QUAN\r\aĐộc lập – Tự do – Hạnh phúc\r\aBÁO CÁO\rTrích yếu\r";
        var targets=new[]{new LineTarget(LineRole.Agency,0,"CƠ QUAN",0,9,true),
            new LineTarget(LineRole.Motto,8,"Độc lập – Tự do – Hạnh phúc",8,36,true),
            new LineTarget(LineRole.Subject,44,"Trích yếu",36,55,true)};
        var selected=targets.Select(t=>t.Probe with {CanFix=true,LineFix=new(7,100,106,90,t.Role)}).ToArray();
        var current=targets.Select((t,i)=>t with {Start=t.Start+i+1,RegionStart=t.RegionStart+i+1,RegionEnd=t.RegionEnd+i+1}).ToArray();
        var bound=LineReferenceBinding.Rebind(selected,current,story,story);
        assert(bound.Select(i=>i.Start).SequenceEqual(current.Select(t=>t.Start)) &&
            bound.Zip(selected).All(p=>p.First.Original==p.Second.Original && p.First.LineFix==p.Second.LineFix),
            "Unchanged story and uniquely verified role/text rebind to current Word CPs without changing geometry");
        bool Reject(IReadOnlyList<LineTarget> layout,string updated=story)
        {
            try{LineReferenceBinding.Rebind(selected,layout,story,updated);return false;}
            catch(InvalidOperationException){return true;}
        }
        assert(Reject(current,story+"x"),"Changed story blocks reference rebinding even when target labels still match");
        assert(Reject(current[..2]),"Missing current Line role cannot be accepted by reusing the old target");
        assert(Reject([..current,current[1]]),"Duplicate role/text cannot choose a first matching reference");
        assert(Reject(current.Select((t,i)=>i==1?t with {Text=t.Text+"x"}:t).ToArray()),"Changed reference text cannot be rebound by role alone");
        assert(Reject(current.Select((t,i)=>i==1?t with {Centered=false}:t).ToArray()),"A reference no longer eligible for automatic placement blocks rebinding");
        int stringOffset=story.IndexOf(targets[1].Text,StringComparison.Ordinal);
        var anchor=WordPreviewAnchor.Create(1,story,story[..stringOffset],targets[1].Text);
        var span=PreviewLocator.Locate(anchor,story)!;
        assert(anchor.Offset==stringOffset && span.Length==targets[1].Text.Length &&
            story.Substring(span.Start,span.Length).EndsWith("phúc",StringComparison.Ordinal),
            "Preview prefix mapping retains the final character when Word CP differs from string offset");
        var oldSpan=PreviewLocator.Locate(new(1,story,targets[1].Start,targets[1].Text.Length),story)!;
        assert(oldSpan.Length<targets[1].Text.Length && !story.Substring(oldSpan.Start,oldSpan.Length).EndsWith("phúc",StringComparison.Ordinal),
            "The old CP-as-string-index calculation reproduces an incomplete Motto reference");
        bool BadAnchor(string prefix,string text)
        {
            try{WordPreviewAnchor.Create(1,story,prefix,text);return false;}
            catch(InvalidOperationException){return true;}
        }
        assert(BadAnchor("wrong prefix",targets[1].Text) && BadAnchor(story,targets[1].Text) &&
            BadAnchor(story[..stringOffset],"wrong text"),"Invalid prefix or source span cannot yield a guessed preview anchor");
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTaBinding-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            string before=Path.Combine(folder,"before.docx"),working=Path.Combine(folder,"working.docx");
            File.WriteAllText(before,"original");File.Copy(before,working);
            var repair=bound.Where(i=>i.LineFix!.Role!=LineRole.Agency).ToArray();
            var starts=new List<int>();
            AtomicLineBatch.Apply(working,before,selected,(i,_)=>{starts.Add(i.Start);File.AppendAllText(working,"/edit");},
                ()=>File.Copy(before,working,true),()=>{if(starts.Count!=5)throw new Exception("not complete");},()=>new(bound,repair));
            assert(starts.Skip(3).Order().SequenceEqual(repair.Select(i=>i.Start).Order()) && File.ReadAllText(before)=="original",
                "A batch can settle only its verified current selection after Word CPs change and keep the original Undo checkpoint");
            bool wrongScope=false;File.Copy(before,working,true);
            try{AtomicLineBatch.Apply(working,before,selected,(_,_)=>File.AppendAllText(working,"/edit"),
                ()=>File.Copy(before,working,true),remainingIssues:()=>new([..bound.Take(2),bound[2] with {Original="different"}],repair));}
            catch(InvalidOperationException){wrongScope=true;}
            assert(wrongScope && File.ReadAllText(working)=="original","Rebinding cannot broaden the original selection to different text");
        }
        finally{Directory.Delete(folder,true);}
    }
}
