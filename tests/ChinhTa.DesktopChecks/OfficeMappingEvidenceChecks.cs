using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class OfficeMappingEvidenceChecks
{
    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-mapping-evidence-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            var writer=new OfficeMappingEvidence();int calls=0;
            assert(writer.Capture(null,()=>{calls++;throw new InvalidOperationException("Normal document text must not be captured");}) is null && calls==0,
                "Normal document use disables mapping evidence before accessing the snapshot factory");
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            string xml=new XDocument(new XElement(w+"document",new XElement(w+"body",
                new XElement(w+"p",new XElement(w+"r",new XElement(w+"t","Mốc đầu 😀"))),
                new XElement(w+"p",new XElement(w+"r",new XElement(w+"t","Nội dung XML")))))).ToString();
            WordParagraphSnapshot[] live=[new(91,102,"Mốc đầu 😀\r",1,false,true),new(102,107,"Khác\r",1,false,false)];
            string? warning=null;var format=new List<int>();
            var editable=WordTextProof.Read(xml,live,e=>warning=e.DiagnosticCode,onFormatEditable:(s,can)=>{if(can)format.Add(s);});
            assert(warning is not null,"The diagnostic fixture contains a real XML/live paragraph mismatch");
            OfficeMappingSnapshot Snapshot(string sample="synthetic.docx",bool experimental=false)=>new(sample,experimental,xml,live,live.Take(1).ToArray(),
                editable.Order().ToArray(),format.Order().ToArray(),warning,null,false,false);
            var clean=Snapshot() with{TextDiagnostic=null};
            assert(writer.Capture(folder,()=>clean) is null && Directory.GetFiles(folder).Length==0,
                "A fully proven read creates no mismatch evidence files");
            var snapshot=Snapshot();var captured=writer.Capture(folder,()=>snapshot)!;
            string saved=File.ReadAllText(captured.Path);using var json=JsonDocument.Parse(saved);
            var restored=JsonSerializer.Deserialize<OfficeMappingSnapshot>(json.RootElement.GetProperty("Snapshot").GetRawText())!;
            assert(captured.Written && json.RootElement.GetProperty("EvidenceVersion").GetInt32()==1 && restored.CurrentWordOpenXml==xml,
                "One versioned evidence file retains exact current in-memory XML, including Unicode");
            assert(restored.LiveParagraphs.SequenceEqual(live) && restored.HeaderParagraphs.SequenceEqual(live.Take(1)),
                "The whole-body and bounded header observations retain their distinct real Word CP ranges");
            assert(restored.TextEditableStarts.SequenceEqual(editable.Order()) && restored.FormatEditableStarts.SequenceEqual(format.Order()) && restored.TextDiagnostic==warning,
                "Evidence preserves granted text/format starts and fixed numeric mismatch codes without broadening proof");
            string? replayWarning=null;var replayFormats=new List<int>();
            var replay=WordTextProof.Read(restored.CurrentWordOpenXml!,restored.LiveParagraphs,e=>replayWarning=e.DiagnosticCode,
                onFormatEditable:(s,can)=>{if(can)replayFormats.Add(s);});
            assert(replay.SetEquals(editable) && replayWarning==warning && replayFormats.SequenceEqual(format),
                "Replaying the captured XML and live observations reproduces the same permissions and exact failure boundary");
            var stamp=File.GetLastWriteTimeUtc(captured.Path);var same=writer.Capture(folder,()=>snapshot)!;
            assert(!same.Written && same.Path==captured.Path && File.GetLastWriteTimeUtc(captured.Path)==stamp && File.ReadAllText(captured.Path)==saved,
                "Repeated unchanged mismatch evidence reuses the complete snapshot without rewriting it");
            File.Delete(captured.Path);
            assert(writer.Capture(folder,()=>snapshot)!.Written,"A missing previously captured evidence file is recreated rather than reported as retained");
            foreach(bool experimental in new[]{false,true})
            {
                for(int i=0;i<OfficeMappingEvidence.SlotsPerReader+3;i++)
                {
                    var result=writer.Capture(folder,()=>Snapshot($"synthetic-{i}.docx",experimental))!;
                    using var item=JsonDocument.Parse(File.ReadAllText(result.Path));
                    var current=item.RootElement.GetProperty("Snapshot");
                    assert(result.Written && current.GetProperty("Sample").GetString()==$"synthetic-{i}.docx" &&
                        current.GetProperty("Experimental").GetBoolean()==experimental,
                        "A ring slot contains a complete matching XML/range snapshot from its own selected reader");
                }
                assert(Directory.GetFiles(folder,$"mapping-evidence-{(experimental?"experimental":"legacy")}-*.json").Length==OfficeMappingEvidence.SlotsPerReader,
                    "Mapping evidence has bounded retention separately for each reader");
            }
            assert(Directory.GetFiles(folder).Length==OfficeMappingEvidence.SlotsPerReader*2 && Directory.GetFiles(folder,"*.tmp").Length==0,
                "Reader rings cannot grow unbounded or leave temporary XML/range pairs");
            var unavailable=Snapshot() with{CurrentWordOpenXml=null,TextDiagnostic=null,TextUnavailable=true,TableUnavailable=true};
            var unavailableCapture=writer.Capture(folder,()=>unavailable)!;
            using var missing=JsonDocument.Parse(File.ReadAllText(unavailableCapture.Path));
            assert(missing.RootElement.GetProperty("Snapshot").GetProperty("CurrentWordOpenXml").ValueKind==JsonValueKind.Null &&
                missing.RootElement.GetProperty("Snapshot").GetProperty("TextUnavailable").GetBoolean(),
                "An unavailable current XML read is explicit and cannot be replaced by a saved-file XML guess");
            var tableOnly=Snapshot() with{TextDiagnostic=null,TableDiagnostic="XML-COUNT.P2.XML1.Word2"};
            assert(writer.Capture(folder,()=>tableOnly)!.Written,"A table-prefix mismatch is retained even when whole-body text has no diagnostic");
            bool blocked=false;string absent=Path.Combine(folder,"missing-folder");
            try{writer.Capture(absent,()=>Snapshot("failed-write.docx"));}catch(IOException){blocked=true;}
            assert(blocked && !Directory.Exists(absent),"A failed capture reports its I/O failure without inventing evidence or changing the source folder");
            assert(writer.Capture(folder,()=>Snapshot("failed-write.docx"))!.Written && Directory.GetFiles(folder,"*.tmp").Length==0,
                "A failed I/O attempt does not poison duplicate detection or a later successful capture");
            assert(restored.CurrentWordOpenXml==xml && live.SequenceEqual(snapshot.LiveParagraphs) &&
                editable.SetEquals(snapshot.TextEditableStarts),"Capturing and rotating diagnostic files leave input XML, ranges and proof permissions unchanged");
        }
        finally{Directory.Delete(folder,true);}
    }
}
