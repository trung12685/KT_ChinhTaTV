using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

// Diagnostic data from ONE CheckCore read. The XML is current WordOpenXML,
// not a saved checkpoint; both paragraph lists retain their actual Word CPs.
internal sealed record OfficeMappingSnapshot(string Sample,bool Experimental,string? CurrentWordOpenXml,
    IReadOnlyList<WordParagraphSnapshot> LiveParagraphs,IReadOnlyList<WordParagraphSnapshot> HeaderParagraphs,
    IReadOnlyList<int> TextEditableStarts,IReadOnlyList<int> FormatEditableStarts,
    string? TextDiagnostic,string? TableDiagnostic,bool TextUnavailable,bool TableUnavailable);
internal sealed record OfficeMappingCapture(string Path,bool Written);

// Enabled ONLY in the isolated synthetic Office check. Bounded separate rings
// keep the four most recent distinct failures for each reader. No Word calls,
// Save/close, file-source reads or edits to proof/permissions occur here.
internal sealed class OfficeMappingEvidence
{
    internal const int SlotsPerReader=4;
    private sealed class ReaderState
    {
        public string? Fingerprint;
        public string? Path;
        public int NextSlot;
    }
    private readonly ReaderState legacy=new(),experimental=new();
    public OfficeMappingCapture? Capture(string? folder,Func<OfficeMappingSnapshot> read)
    {
        // Do not even materialize diagnostic text during normal document use.
        if(folder is null)return null;
        var snapshot=read();
        if(snapshot.TextDiagnostic is null && snapshot.TableDiagnostic is null &&
            !snapshot.TextUnavailable && !snapshot.TableUnavailable)return null;
        string json=JsonSerializer.Serialize(new {EvidenceVersion=1,Snapshot=snapshot},new JsonSerializerOptions{WriteIndented=true});
        string fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        var state=snapshot.Experimental?experimental:legacy;
        if(state.Fingerprint==fingerprint && state.Path is not null && File.Exists(state.Path))return new(state.Path,false);
        string mode=snapshot.Experimental?"experimental":"legacy";
        string destination=System.IO.Path.Combine(folder,$"mapping-evidence-{mode}-{state.NextSlot+1:00}.json");
        // One file contains XML + live observations, so evidence cannot pair
        // an old XML file with a new range list after an interrupted write.
        string temporary=destination+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            File.WriteAllText(temporary,json,new UTF8Encoding(false));
            File.Move(temporary,destination,true);
        }
        finally{if(File.Exists(temporary))File.Delete(temporary);}
        state.Fingerprint=fingerprint;state.Path=destination;state.NextSlot=(state.NextSlot+1)%SlotsPerReader;
        return new(destination,true);
    }
}
