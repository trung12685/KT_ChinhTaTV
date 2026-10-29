using ChinhTa.Core;

namespace ChinhTa.Desktop;
public sealed partial class WordSession
{
    internal FontSizeEvidence ReadCheckSizes(Issue probe)
    {
        dynamic range=document!.Range(probe.Start,probe.Start+probe.Length);
        try{return VisibleFormatReader.ReadSizes((object)range,probe.Original);}
        finally{Release(range);}
    }
    internal FontSizeEvidence ReadCheckCurrentSizes(Issue original)
    {
        if(LineCheckArtifactFolder is null)throw new InvalidOperationException("Chỉ dùng trong phiên kiểm Word riêng.");
        var rows=lastFormatLayout.Where(p=>p.Text.TrimEnd('\r','\a','\n')==original.Original).ToArray();
        if(rows.Length!=1)throw new InvalidOperationException("Mẫu kiểm chưa có một vùng phông hiện tại duy nhất.");
        return ReadCheckSizes(original with {Start=rows[0].Start});
    }
    internal Issue ReadCheckCurrentParagraph(string text)
    {
        if(LineCheckArtifactFolder is null)throw new InvalidOperationException("Chỉ dùng trong phiên kiểm Word riêng.");
        var rows=lastFormatLayout.Where(p=>p.Text.TrimEnd('\r','\a','\n')==text).ToArray();
        if(rows.Length!=1)throw new InvalidOperationException("Mẫu kiểm chưa có một đoạn hiện tại duy nhất để đối chiếu viết hoa.");
        return new(rows[0].Start,text.Length,text,text,"",false);
    }
    internal CharacterStyleEvidence ReadCheckCurrentStyles(string text)
    {
        var row=ReadCheckCurrentParagraph(text);
        dynamic range=document!.Range(row.Start,row.Start+row.Length);
        try{return VisibleFormatReader.ReadStyles((object)range,text,includeSpacing:true);}finally{Release(range);}
    }
    internal IReadOnlyList<Issue> CaseTrackingRegions(IReadOnlyList<Issue> plan)=>CasePreservationPlan.Regions(plan,lastFormatLayout);
    internal CharacterStyleEvidence ReadCheckExpectedStyles(Issue probe,IReadOnlyList<Issue> plan)
    {
        if(LineCheckArtifactFolder is null)throw new InvalidOperationException("Chỉ dùng trong phiên kiểm Word riêng.");
        return ExpectedCaseStyles(probe,plan);
    }
    private CharacterStyleEvidence ExpectedCaseStyles(Issue probe,IReadOnlyList<Issue> plan)
    {
        dynamic range=document!.Range(probe.Start,probe.Start+probe.Length);
        try
        {
            var source=VisibleFormatReader.ReadStyles((object)range,probe.Original,includeSpacing:probe.Kind==FixKind.PunctuationSpace ||
                plan.Any(i=>(i.Kind==FixKind.SignatureFormat || i.ParagraphFix is not null || i.SpacingFix is not null || i.CharacterFormat is {Role:ComponentRole.Body,PreserveEmphasis:true}) && i.Start==probe.Start));
            return CasePreservationPlan.Expected(probe,plan,source,item=>
            {
                dynamic prefix=document!.Range(probe.Start,item.Start);
                try{return (string?)prefix.Text??"";}finally{Release(prefix);}
            });
        }
        finally{Release(range);}
    }
    private void VerifyBodySpacing(IReadOnlyList<BodySpacingProof> proofs,string expectedStory)
    {
        if(proofs.Count==0)return;
        Progress(RepairActivity.Verifying,$"Đang kiểm lại khoảng đoạn/giãn dòng và đoạn liền sau ({proofs.Count} mục).");
        CheckCore();
        if(ReadStoryText()!=expectedStory)throw new InvalidOperationException("Nội dung ngoài lựa chọn đã đổi khi kiểm lại khoảng đoạn/giãn dòng; phải phục hồi.");
        // A selected duplicate deletion can make proven prose shorter than the
        // discovery threshold. Re-observe that exact XML-proven span for the
        // postcondition instead of treating the new threshold as a lost proof.
        var texts=proofs.Select(p=>p.Text).Concat(proofs.Where(p=>p.NextText is not null).Select(p=>p.NextText!)).ToHashSet();
        var current=lastBodyParagraphs.ToArray();
        for(int i=0;i<current.Length;i++)
        {
            var row=current[i];
            if(!texts.Contains(row.Text.TrimEnd('\r','\n','\a')) || row.Flow is {Known:true} && row.ParagraphStyle is not null ||
                !row.TextEditable || row.Outline is not {Scope:OutlineScope.PlainBody,HeadingLevel:null})continue;
            var bounds=lastFormatLayout.Where(p=>p.Start==row.Start).ToArray();
            if(bounds.Length==1)current[i]=ObserveComponent(row,true,bounds[0].End);
        }
        lastBodyParagraphs=current;
        BodySpacingVerification.Verify(proofs,lastBodyParagraphs,row=>ReadPrefix(new(row.Start,row.Text.TrimEnd('\r','\a','\n').Length,
            row.Text.TrimEnd('\r','\a','\n'),row.Text.TrimEnd('\r','\a','\n'),"",false)));
    }
    private FontSizeEvidence? ExpectedSizesFor(Issue issue,IReadOnlyList<Issue> edits)
    {
        // Body emphasis repairs always carry a complete retained-style proof,
        // including exact sizes. Avoid an additional per-character size pass.
        if(issue.CharacterFormat is {Role:ComponentRole.Body,PreserveEmphasis:true})return null;
        if(issue.CharacterFormat?.PreserveSize!=true && issue.Format?.MaximumSize is null)return null;
        dynamic range=document!.Range(issue.Start,issue.Start+issue.Length);
        try
        {
            var source=VisibleFormatReader.ReadSizes((object)range,issue.Original);
            if(issue.CharacterFormat is {RepairInvalidSizes:true} selective)
                source=source.CorrectInvalid(selective.SizeRange.Minimum,selective.SizeRange.Maximum);
            else if(issue.Format is {RepairInvalidSizes:true,MaximumSize:float maximum} heading)
                source=source.CorrectInvalid(heading.Size,maximum);
            var contained=edits.Where(i=>!i.IsStyleOnly && i.Format is null && i.Start>=issue.Start &&
                (long)i.Start+i.Length<=(long)issue.Start+issue.Length).ToArray();
            return source.Project(contained,item=>
            {
                dynamic prefix=document!.Range(issue.Start,item.Start);
                try{return (string?)prefix.Text??"";}finally{Release(prefix);}
            });
        }
        finally{Release(range);}
    }
    private Paragraph ObserveComponent(Paragraph row,bool readParagraph=false,int? liveEnd=null,
        WordFormatRunPlan? runPlan=null,FormatReadMeasurements? measurements=null,NativeFormatHint? batch=null)
    {
        if(!row.TextEditable && !row.FormatEditable)return row;
        string text=row.Text.TrimEnd('\r','\a','\n');
        var observations=new List<VisibleFont>();int offset=0;
        if(batch is not null && liveEnd is int bound && batch.Matches(row.Start,bound,row.Text))
        {
            dynamic current=document!.Range(row.Start,bound);
            try{if(!VisibleFormatReader.MatchesHint((object)current,batch))batch=null;}
            finally{Release(current);}
        }
        else batch=null;
        if(batch?.Font is {Known:true} shared)observations.Add(shared);
        // A discretionary line break can carry a different run style from its
        // visible words. Observe each nonblank line; never remove the break.
        foreach(string part in observations.Count>0?Array.Empty<string>():text.Split('\v'))
        {
            if(!string.IsNullOrWhiteSpace(part))
            {
                dynamic range=document!.Range(row.Start+offset,row.Start+offset+part.Length);
                try
                {
                    // A plan is current-scan-only and only applies to the exact
                    // whole visible paragraph; soft breaks use the existing path.
                    observations.AddRange(VisibleFormatReader.Read((object)range,part,allowCharacterFallback:true,
                        runPlan:offset==0 && part==text?runPlan:null,
                        readRange:(start,end)=>(object)document!.Range(start,end),measurements:measurements));
                }
                finally{Release(range);}
            }
            offset+=part.Length+1;
        }
        if(observations.Count==0)return row;
        ObservedParagraphStyle? paragraphStyle=batch?.Paragraph;BodyFlow? flow=null;
        if(readParagraph)
        {
            dynamic range=document!.Range(row.Start,row.Start+text.Length);
            try
            {
                paragraphStyle??=VisibleFormatReader.ReadParagraph((object)range);
                if(liveEnd is int end && end>row.Start)
                {
                    var observed=WordOwnedObjects.Read(own=>
                    {
                        dynamic firstPoint=own((object)document!.Range(row.Start,row.Start));dynamic lastPoint=own((object)document!.Range(end-1,end-1));
                        return new BodyFlow((int)firstPoint.Information[3],(int)lastPoint.Information[3]);
                    },Release);
                    if(observed.Known)flow=observed;
                }
            }
            catch(Exception error) when(error is InvalidOperationException || error is System.Runtime.InteropServices.COMException && !WordConnectionPolicy.IsDisconnected(error))
            {Diagnostics.Write("Word.BodyParagraph.Unavailable",error);}
            finally{Release(range);}
        }
        var first=observations[0];
        return row with {Flow=flow,ParagraphStyle=paragraphStyle,Font=first.Name,Size=first.Size,Bold=first.Bold==-1,Italic=first.Italic!=0,Color=first.Color,FormatObserved=true,
            FormatMixed=observations.Any(o=>o!=first || !o.Known),
            BoldMixed=observations.Any(o=>o.Bold is not (0 or -1)),
            FormatEvidence=new(text,observations.Select(o=>new ObservedFont(o.Name,o.Size,o.Bold,o.Italic,o.Color)))};
    }
}
