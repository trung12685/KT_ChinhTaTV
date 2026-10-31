using System.Security.Cryptography;
using System.Text;
namespace ChinhTa.WordProtocol;
// Plain DTOs can cross the .NET Framework Word host / .NET 8 worker boundary.
public sealed class ParagraphDto
{
    public int Index {get;set;}
    public int Start {get;set;}
    public int End {get;set;}
    public string Text {get;set;}="";
    public bool InTable {get;set;}
}
public sealed class FindingDto
{
    public int ParagraphIndex {get;set;}
    // UTF-16 string offset in ParagraphDto.Text. Never a native Word CP.
    public int Offset {get;set;}
    public int Length {get;set;}
    public bool InTable {get;set;}
    public string Original {get;set;}="";
    public string Message {get;set;}="";
    public string Category {get;set;}="";
    public string[] Suggestions {get;set;}=Array.Empty<string>();
    public override string ToString()=>"Đoạn "+ParagraphIndex+(InTable?" (bảng)":"")+": "+Original+" — "+Message;
}
public sealed class BridgeRequest
{
    public int Version {get;set;}=BridgeContract.Version;
    public string Id {get;set;}="";
    public string Operation {get;set;}="";
    public string DocumentId {get;set;}="";
    public string SnapshotHash {get;set;}="";
    public List<ParagraphDto> Paragraphs {get;set;}=new List<ParagraphDto>();
    public string Term {get;set;}="";
}
public sealed class BridgeResponse
{
    public int Version {get;set;}=BridgeContract.Version;
    public string Id {get;set;}="";
    public string DocumentId {get;set;}="";
    public string SnapshotHash {get;set;}="";
    public bool Success {get;set;}
    public string Error {get;set;}="";
    public string Source {get;set;}="";
    public long DictionaryRevision {get;set;}
    public List<FindingDto> Findings {get;set;}=new List<FindingDto>();
    public string[] Terms {get;set;}=Array.Empty<string>();
    public string MirrorPath {get;set;}="";
    public string Notice {get;set;}="";
    public int SkippedParagraphs {get;set;}
    public int TotalParagraphs {get;set;}
    public int ScannedParagraphs {get;set;}
    public int ScannedTableParagraphs {get;set;}
    public int ScannedSegments {get;set;}
    public bool Truncated {get;set;}
}
public static class BridgeContract
{
    // v2 separates string offsets from native Word positions. Reject v1 peers.
    public const int Version=2;
    public const int MaxFrame=4*1024*1024;
    public static string Hash(string text)
    {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","");}
    public static bool HasBounds(ParagraphDto p)=>p.Index>0 && p.Start>=0 && p.End>=p.Start && p.Text is not null;
    public static bool CanSelect(ParagraphDto before,ParagraphDto now,FindingDto finding,string originalHash,string currentHash)
    {
        int offset=finding.Offset;
        return originalHash==currentHash && HasBounds(before) && HasBounds(now) && before.Start==now.Start && before.End==now.End && before.Text==now.Text &&
            before.Index==now.Index && before.InTable==now.InTable && finding.ParagraphIndex==before.Index && finding.InTable==before.InTable &&
            offset>=0 && finding.Length>0 && offset<=before.Text.Length-finding.Length &&
            before.Text.Substring(offset,finding.Length)==finding.Original && finding.Length==finding.Original.Length;
    }
}
