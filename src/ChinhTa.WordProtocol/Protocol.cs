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
    public FormatDto? Format {get;set;}
}
public sealed class FormatDto
{
    public bool Observed {get;set;}
    public bool LayoutKnown {get;set;}
    public string Font {get;set;}="";
    public float Size {get;set;}
    public int Bold {get;set;}
    public int Italic {get;set;}
    public int Color {get;set;}
    public int Alignment {get;set;}=-1;
    public int Page {get;set;}
    public bool UnindentedCenter {get;set;}
    public int CellStart {get;set;}=-1;
    public int CellEnd {get;set;}=-1;
    public int TableStart {get;set;}=-1;
}
public sealed class SectionDto
{
    public int Number {get;set;}
    public int Start {get;set;}
    public int End {get;set;}
    public double WidthMm {get;set;}
    public double HeightMm {get;set;}
    public double TopMm {get;set;}
    public double BottomMm {get;set;}
    public double LeftMm {get;set;}
    public double RightMm {get;set;}
    public bool MirrorMargins {get;set;}
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
    public override string ToString()=>(Category=="Spelling"?"[Chính tả] ":Category=="Format"?"[Thể thức] ":"[Gợi ý] ")+"Đoạn "+ParagraphIndex+(InTable?" (bảng)":"")+": "+Original+" — "+Message;
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
    public string ReviewMode {get;set;}="text";
    public string Profile {get;set;}="State";
    public bool StrictSpelling {get;set;}
    public List<SectionDto> Sections {get;set;}=new List<SectionDto>();
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
    public string Scope {get;set;}="";
    public int ObservedFormatParagraphs {get;set;}
    public int ObservedSections {get;set;}
}
public static class BridgeContract
{
    // v3 adds explicit spelling/deep/format modes and observed metadata.
    // String offsets remain separate from native Word positions.
    public const int Version=3;
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
