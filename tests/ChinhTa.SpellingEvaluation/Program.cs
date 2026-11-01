using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ChinhTa.Core;
using ChinhTa.WordProtocol;
using ChinhTa.WordWorker;

if(args.Length!=2)throw new ArgumentException("Expected evaluation directory and output JSON path");
string input=Path.GetFullPath(args[0]),output=Path.GetFullPath(args[1]);
var rows=File.ReadLines(Path.Combine(input,"cases.jsonl")).Where(l=>!string.IsNullOrWhiteSpace(l)).Select(l=>JsonSerializer.Deserialize<Case>(l)!).ToArray();
if(rows.Any(r=>r is null || r.Text is null || r.Edits is null || r.Terms is null) || rows.Select(r=>r.Id).Distinct().Count()!=rows.Length)throw new InvalidOperationException("Invalid evaluation rows");
foreach(var row in rows)foreach(var e in row.Edits)
    if(e.Start<0 || e.Length<=0 || e.Start>row.Text.Length-e.Length || row.Text.Substring(e.Start,e.Length)!=e.Original)throw new InvalidOperationException("Invalid UTF-16 annotation: "+row.Id);
string root=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".spelling-evaluation");Directory.CreateDirectory(root);
try
{
    var load=Stopwatch.StartNew();var model=VietnameseSpellingDefaults.Load(Path.Combine(AppContext.BaseDirectory,"spelling"));load.Stop();
    var syllables=SyllableDictionary.Load(Path.Combine(AppContext.BaseDirectory,"proofing","Vietnamese.words.txt"));
    var cold=Stopwatch.StartNew();model.Check("Hom qua tôi đã gửi thư.",null,true);cold.Stop();
    var reports=new Dictionary<string,object>();
    foreach(bool candidate in new[]{false,true})
    {
        var services=new Dictionary<string,WorkerService>();var scores=new List<Score>();var details=new List<object>();
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
            if(!reply.Success)throw new InvalidOperationException("Worker failed on "+row.Id+": "+reply.Error);
            foreach(var f in reply.Findings)if(f.Offset<0 || f.Length<=0 || f.Offset>text.Length-f.Length || text.Substring(f.Offset,f.Length)!=f.Original)throw new InvalidOperationException("Unbound finding: "+row.Id);
            var red=reply.Findings.Where(f=>f.Category=="Spelling").ToArray();
            int hits=row.Edits.Count(e=>red.Any(f=>Overlap(f,e))),top1=row.Edits.Count(e=>red.Any(f=>Overlap(f,e) && Correct(f,e,row,f.Suggestions.Take(1)))),
                top3=row.Edits.Count(e=>red.Any(f=>Overlap(f,e) && Correct(f,e,row,f.Suggestions.Take(3))));
            var score=new Score(row.Id,row.Group,row.Kind,VietnameseSpellingModel.Tokenize(row.Text).Count,row.Edits.Length,hits,top1,top3,red.Length,
                red.Count(f=>!row.Edits.Any(e=>Overlap(f,e))),clock.Elapsed.TotalMilliseconds);scores.Add(score);
            if(row.Kind=="developer_fixture")details.Add(new{row.Id,row.Group,row.Text,Targets=row.Edits,score.Hits,score.Top1,score.Top3,Warnings=red});
        }
        reports[candidate?"candidate":"baseline_0_1_91_compatible"]=new
        {
            Groups=scores.GroupBy(s=>(s.Kind,s.Group)).Select(g=>Summary(g.Key.Kind,g.Key.Group,g.ToArray())).ToArray(),
            All=Summary("all","all",scores.ToArray()),DeveloperFixtures=details,
            AuditWarningRatePer1000Tokens=Rate(scores.Where(s=>s.Kind=="unannotated_corpus").Sum(s=>s.Warnings),scores.Where(s=>s.Kind=="unannotated_corpus").Sum(s=>s.Tokens),1000),
            Note="Both modes use the same data and full worker. The baseline reproduces 0.1.91; candidate enables generic restoration and experimental strict-lookup name/lexicon policies."
        };
    }
    using var metadata=JsonDocument.Parse(File.ReadAllText(Path.Combine(input,"metadata.json")));
    var result=new{Schema=1,ModelFingerprint=model.Source.Fingerprint,model.Source,BundleLoadMs=load.Elapsed.TotalMilliseconds,CandidateIndexAndFirstReviewMs=cold.Elapsed.TotalMilliseconds,
        Input=metadata.RootElement.Clone(),Reports=reports,
        ReleaseReadiness="NOT_QUALIFIED",Reasons=new[]{"No independent, double-reviewed real-error/correct-text gold set is available.","Synthetic target recovery and warnings on unannotated text do not establish production precision or recall.","Word runtime/visual placement and user-machine latency are not measured by this evaluator."}};
    Directory.CreateDirectory(Path.GetDirectoryName(output)!);File.WriteAllText(output,JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}),new UTF8Encoding(false));
    Console.WriteLine("Measured "+rows.Length+" rows with the full worker pipeline in both modes. Quality report: "+output);
    Console.WriteLine("Release readiness: NOT_QUALIFIED. CI correctness is not proofing quality certification.");
}
finally{Directory.Delete(root,true);}

static bool Overlap(FindingDto f,Edit e)=>f.Offset<(long)e.Start+e.Length && e.Start<(long)f.Offset+f.Length;
static bool Correct(FindingDto f,Edit target,Case row,IEnumerable<string> suggestions)
{
    if(target.Start<f.Offset || target.Start+target.Length>f.Offset+f.Length)return false;
    string wanted=row.Text.Substring(f.Offset,f.Length);
    foreach(var e in row.Edits.Where(e=>e.Start>=f.Offset && e.Start+e.Length<=f.Offset+f.Length).OrderByDescending(e=>e.Start))
        wanted=wanted.Remove(e.Start-f.Offset,e.Length).Insert(e.Start-f.Offset,e.Target);
    return suggestions.Any(s=>s.Normalize(NormalizationForm.FormC)==wanted.Normalize(NormalizationForm.FormC));
}
static double? Rate(int numerator,int denominator,double factor=1)=>denominator==0?null:factor*numerator/denominator;
static object Summary(string kind,string group,Score[] scores)
{
    var latency=scores.Select(s=>s.Ms).Order().ToArray();int targets=scores.Sum(s=>s.Targets);
    return new{Kind=kind,Group=group,Rows=scores.Length,Tokens=scores.Sum(s=>s.Tokens),Targets=targets,Hits=scores.Sum(s=>s.Hits),
        TargetDetectionRate=Rate(scores.Sum(s=>s.Hits),targets),Top1TargetCorrectionRate=Rate(scores.Sum(s=>s.Top1),targets),Top3TargetCorrectionRate=Rate(scores.Sum(s=>s.Top3),targets),
        RedWarnings=scores.Sum(s=>s.Warnings),WarningsOutsideAnnotatedTargets=scores.Sum(s=>s.Extra),
        RowsWithExtraWarnings=scores.Count(s=>s.Extra>0),P95WarmWorkerMs=latency.Length==0?0:latency[(int)Math.Ceiling(latency.Length*.95)-1]};
}
public sealed record Case(string Id,string Group,string Kind,string Text,Edit[] Edits,string[] Terms);
public sealed record Edit(int Start,int Length,string Original,string Target);
public sealed record Score(string Id,string Group,string Kind,int Tokens,int Targets,int Hits,int Top1,int Top3,int Warnings,int Extra,double Ms);
