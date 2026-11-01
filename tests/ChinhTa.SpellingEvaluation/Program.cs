using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using ChinhTa.Core;
using ChinhTa.WordProtocol;
using ChinhTa.WordWorker;

if(args.Length==1 && args[0]=="--self-check"){EvaluationChecks.Run();return;}
if(args.Length!=2)throw new ArgumentException("Expected evaluation directory and output JSON path");
string input=Path.GetFullPath(args[0]),output=Path.GetFullPath(args[1]);
using var metadata=JsonDocument.Parse(File.ReadAllText(Path.Combine(input,"metadata.json")));
string hash=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(input,"cases.jsonl"))));
if(!metadata.RootElement.TryGetProperty("CasesSHA256",out var expectedHash) || !hash.Equals(expectedHash.GetString(),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Evaluation input hash mismatch");
var rows=File.ReadLines(Path.Combine(input,"cases.jsonl")).Where(l=>!string.IsNullOrWhiteSpace(l)).Select(l=>JsonSerializer.Deserialize<Case>(l)!).ToArray();
if(rows.Any(r=>r is null || r.Text is null || r.Edits is null || r.Terms is null) || rows.Select(r=>r.Id).Distinct().Count()!=rows.Length)throw new InvalidOperationException("Invalid evaluation rows");
foreach(var row in rows)EvaluationScoring.Validate(row);
string root=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".spelling-evaluation");Directory.CreateDirectory(root);
try
{
    var load=Stopwatch.StartNew();var model=VietnameseSpellingDefaults.Load(Path.Combine(AppContext.BaseDirectory,"spelling"));load.Stop();
    var syllables=SyllableDictionary.Load(Path.Combine(AppContext.BaseDirectory,"proofing","Vietnamese.words.txt"));
    var cold=Stopwatch.StartNew();model.Check("Hom qua tôi đã gửi thư.",null,true);cold.Stop();
    var reports=new Dictionary<string,object>();
    foreach(bool candidate in new[]{false,true})
    {
        var services=new Dictionary<string,WorkerService>();var scores=new List<Score>();var details=new List<object>();var targets=new List<EvaluationScoring.TargetResult>();
        foreach(var row in rows)
        {
            string key=BridgeContract.Hash(string.Join('\u001f',row.Terms.Order(StringComparer.Ordinal)));
            if(!services.TryGetValue(key,out var service))
            {
                var personal=new PersonalLexiconStore(Path.Combine(root,candidate?"candidate":"baseline",key,"terms.json"));
                foreach(string term in row.Terms)personal.Add(term);
                services[key]=service=new(model,personal,Path.Combine(root,key+".dic"),syllables,candidate);
                // Exclude first-use JIT/service setup from warm paragraph timing.
                string warm="Văn bản đã được kiểm tra.\r";
                var warmed=service.Handle(new BridgeRequest{Id=Guid.NewGuid().ToString(),Operation="scan",ReviewMode="spelling",StrictSpelling=true,
                    DocumentId=Guid.NewGuid().ToString(),SnapshotHash=BridgeContract.Hash(warm),Paragraphs=[new(){Index=1,Start=0,End=warm.Length,Text=warm}]});
                if(!warmed.Success)throw new InvalidOperationException("Worker warmup failed");
            }
            string text=row.Text+"\r";var request=new BridgeRequest{Id=Guid.NewGuid().ToString(),Operation="scan",ReviewMode="spelling",StrictSpelling=true,
                DocumentId=Guid.NewGuid().ToString(),SnapshotHash=BridgeContract.Hash(text),Paragraphs=[new(){Index=1,Start=0,End=text.Length,Text=text}]};
            var clock=Stopwatch.StartNew();var reply=service.Handle(request);clock.Stop();
            if(!reply.Success || reply.Truncated)throw new InvalidOperationException("Worker failed or truncated on "+row.Id+": "+reply.Error);
            foreach(var f in reply.Findings)if(f.Offset<0 || f.Length<=0 || f.Offset>text.Length-f.Length || text.Substring(f.Offset,f.Length)!=f.Original)throw new InvalidOperationException("Unbound finding: "+row.Id);
            var red=reply.Findings.Where(f=>f.Category=="Spelling").ToArray();
            var measured=EvaluationScoring.Measure(row,red);
            targets.AddRange(measured.Targets.Select((t,i)=>row.Kind=="external_real_error"?t with{OriginalInLexicon=model.Recognizes(row.Edits[i].Original)}:t));
            var score=new Score(row.Id,row.Group,row.Kind,VietnameseSpellingModel.Tokenize(row.Text).Count,row.Edits.Length,
                measured.Targets.Count(e=>e.Hit),measured.Targets.Count(e=>e.Top1),measured.Targets.Count(e=>e.Top3),red.Length,
                measured.ExtraWarnings,clock.Elapsed.TotalMilliseconds,measured.MatchedWarnings,measured.WarningsOnCorrectSpans,measured.CorrectTokens,measured.UnlabeledWarnings,reply.SkippedParagraphs);scores.Add(score);
            if(row.Kind=="developer_fixture")details.Add(new{row.Id,row.Group,row.Text,Targets=row.Edits,score.Hits,score.Top1,score.Top3,Warnings=red});
        }
        reports[candidate?"candidate":"baseline_0_1_91_compatible"]=new
        {
            Groups=scores.GroupBy(s=>(s.Kind,s.Group)).Select(g=>Summary(g.Key.Kind,g.Key.Group,g.ToArray())).ToArray(),
            All=Summary("all","all",scores.ToArray()),DeveloperFixtures=details,
            ErrorGroups=targets.GroupBy(t=>t.Group).Select(g=>new{Group=g.Key,Targets=g.Count(),Hits=g.Count(t=>t.Hit),Top1=g.Count(t=>t.Top1),Top3=g.Count(t=>t.Top3)}).ToArray(),
            SourceWordGroups=targets.Where(t=>t.OriginalInLexicon.HasValue).GroupBy(t=>t.OriginalInLexicon!.Value).Select(g=>new{OriginalInLexicon=g.Key,Targets=g.Count(),Hits=g.Count(t=>t.Hit),Top1=g.Count(t=>t.Top1),Top3=g.Count(t=>t.Top3)}).ToArray(),
            SourceLabelAgreement=LabelAgreement(scores.Where(s=>s.Kind=="external_real_error").ToArray()),
            AuditWarningRatePer1000Tokens=Rate(scores.Where(s=>s.Kind=="unannotated_corpus").Sum(s=>s.Warnings),scores.Where(s=>s.Kind=="unannotated_corpus").Sum(s=>s.Tokens),1000),
            Note="Both modes use the same data and full worker. The baseline reproduces 0.1.91; candidate enables generic restoration and experimental strict-lookup name/lexicon policies."
        };
    }
    var result=new{Schema=2,ModelFingerprint=model.Source.Fingerprint,model.Source,BundleLoadMs=load.Elapsed.TotalMilliseconds,CandidateIndexAndFirstReviewMs=cold.Elapsed.TotalMilliseconds,
        Input=metadata.RootElement.Clone(),Reports=reports,
        ReleaseReadiness="NOT_QUALIFIED",Reasons=new[]{"No independent, double-reviewed real-error/correct-text gold set is available.","Synthetic target recovery and warnings on unannotated text do not establish production precision or recall.","Word runtime/visual placement and user-machine latency are not measured by this evaluator."}};
    Directory.CreateDirectory(Path.GetDirectoryName(output)!);File.WriteAllText(output,JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}),new UTF8Encoding(false));
    Console.WriteLine("Measured "+rows.Length+" rows with the full worker pipeline in both modes. Quality report: "+output);
    Console.WriteLine("Release readiness: NOT_QUALIFIED. CI correctness is not proofing quality certification.");
}
finally{Directory.Delete(root,true);}

static double? Rate(int numerator,int denominator,double factor=1)=>denominator==0?null:factor*numerator/denominator;
static object LabelAgreement(Score[] scores)=>new
{
    Rows=scores.Length,Targets=scores.Sum(s=>s.Targets),Hits=scores.Sum(s=>s.Hits),Warnings=scores.Sum(s=>s.Warnings),MatchedWarnings=scores.Sum(s=>s.MatchedWarnings),
    TargetRecall=Rate(scores.Sum(s=>s.Hits),scores.Sum(s=>s.Targets)),OverlapWarningPrecision=Rate(scores.Sum(s=>s.MatchedWarnings),scores.Sum(s=>s.Warnings)),
    WarningsOnSourceLabeledCorrectSpans=scores.Sum(s=>s.CorrectWarnings),SourceLabeledCorrectTokens=scores.Sum(s=>s.CorrectTokens),
    WarningsPer1000SourceLabeledCorrectTokens=Rate(scores.Sum(s=>s.CorrectWarnings),scores.Sum(s=>s.CorrectTokens),1000),
    UnlabeledWarnings=scores.Sum(s=>s.UnlabeledWarnings),SkippedRows=scores.Count(s=>s.Skipped>0),
    Definition="Overlap agreement with upstream labels; not independent human certification. Correct spans occur inside erroneous sentences, not an error-free corpus."
};
static object Summary(string kind,string group,Score[] scores)
{
    var latency=scores.Select(s=>s.Ms).Order().ToArray();int targets=scores.Sum(s=>s.Targets);
    return new{Kind=kind,Group=group,Rows=scores.Length,Tokens=scores.Sum(s=>s.Tokens),Targets=targets,Hits=scores.Sum(s=>s.Hits),
        TargetDetectionRate=Rate(scores.Sum(s=>s.Hits),targets),Top1TargetCorrectionRate=Rate(scores.Sum(s=>s.Top1),targets),Top3TargetCorrectionRate=Rate(scores.Sum(s=>s.Top3),targets),
        RedWarnings=scores.Sum(s=>s.Warnings),WarningsOutsideAnnotatedTargets=scores.Sum(s=>s.Extra),
        RowsWithExtraWarnings=scores.Count(s=>s.Extra>0),P95WarmWorkerMs=latency.Length==0?0:latency[(int)Math.Ceiling(latency.Length*.95)-1]};
}
public sealed record Case(string Id,string Group,string Kind,string Text,Edit[] Edits,string[] Terms,CorrectSpan[]? CorrectSpans=null);
public sealed record CorrectSpan(int Start,int Length,string Original);
public sealed record Edit(int Start,int Length,string Original,string Target,string[]? AlternativeTargets=null,string? Group=null,bool? OriginalInLexicon=null);
public sealed record Score(string Id,string Group,string Kind,int Tokens,int Targets,int Hits,int Top1,int Top3,int Warnings,int Extra,double Ms,int MatchedWarnings,int CorrectWarnings,int CorrectTokens,int UnlabeledWarnings,int Skipped);
