namespace ChinhTa.Core;

public enum DocumentTypeSource { DecreeAdministrative,DecreeCopy,UserSupplement }
public sealed record DocumentTypeEntry(string Name,string Heading,string Abbreviation,DocumentTypeSource Source)
{
    public bool NamedHeading=>Source!=DocumentTypeSource.DecreeCopy;
    public string SourceLabel=>Source switch
    {DocumentTypeSource.DecreeAdministrative=>"NĐ30, PL III.I — hành chính",DocumentTypeSource.DecreeCopy=>"NĐ30, PL III.I — bản sao",_=>"Người dùng bổ sung"};
}
// Reference data transcribed from the signed NĐ30 PDF p40. Preserve the exact
// mixed-case abbreviations; document-copy entries have separate format rules.
public static class DocumentTypeCatalog
{
    public const string Citation="Nghị định 30/2020/NĐ-CP, Phụ lục III, phần I (PDF trang 40).";
    public const string UserNote="Báo cáo đề xuất — BCĐX do người dùng bổ sung, không nằm trong bảng Phụ lục III.";
    public static IReadOnlyList<DocumentTypeEntry> Entries {get;}=Array.AsReadOnly(new DocumentTypeEntry[]
    {
        new("Nghị quyết (cá biệt)","NGHỊ QUYẾT","NQ",DocumentTypeSource.DecreeAdministrative),
        new("Quyết định (cá biệt)","QUYẾT ĐỊNH","QĐ",DocumentTypeSource.DecreeAdministrative),
        new("Chỉ thị","CHỈ THỊ","CT",DocumentTypeSource.DecreeAdministrative),
        new("Quy chế","QUY CHẾ","QC",DocumentTypeSource.DecreeAdministrative),
        new("Quy định","QUY ĐỊNH","QyĐ",DocumentTypeSource.DecreeAdministrative),
        new("Thông cáo","THÔNG CÁO","TC",DocumentTypeSource.DecreeAdministrative),
        new("Thông báo","THÔNG BÁO","TB",DocumentTypeSource.DecreeAdministrative),
        new("Hướng dẫn","HƯỚNG DẪN","HD",DocumentTypeSource.DecreeAdministrative),
        new("Chương trình","CHƯƠNG TRÌNH","CTr",DocumentTypeSource.DecreeAdministrative),
        new("Kế hoạch","KẾ HOẠCH","KH",DocumentTypeSource.DecreeAdministrative),
        new("Phương án","PHƯƠNG ÁN","PA",DocumentTypeSource.DecreeAdministrative),
        new("Đề án","ĐỀ ÁN","ĐA",DocumentTypeSource.DecreeAdministrative),
        new("Dự án","DỰ ÁN","DA",DocumentTypeSource.DecreeAdministrative),
        new("Báo cáo","BÁO CÁO","BC",DocumentTypeSource.DecreeAdministrative),
        new("Biên bản","BIÊN BẢN","BB",DocumentTypeSource.DecreeAdministrative),
        new("Tờ trình","TỜ TRÌNH","TTr",DocumentTypeSource.DecreeAdministrative),
        new("Hợp đồng","HỢP ĐỒNG","HĐ",DocumentTypeSource.DecreeAdministrative),
        new("Công điện","CÔNG ĐIỆN","CĐ",DocumentTypeSource.DecreeAdministrative),
        new("Bản ghi nhớ","BẢN GHI NHỚ","BGN",DocumentTypeSource.DecreeAdministrative),
        new("Bản thỏa thuận","BẢN THỎA THUẬN","BTT",DocumentTypeSource.DecreeAdministrative),
        new("Giấy uỷ quyền","GIẤY UỶ QUYỀN","GUQ",DocumentTypeSource.DecreeAdministrative),
        new("Giấy mời","GIẤY MỜI","GM",DocumentTypeSource.DecreeAdministrative),
        new("Giấy giới thiệu","GIẤY GIỚI THIỆU","GGT",DocumentTypeSource.DecreeAdministrative),
        new("Giấy nghỉ phép","GIẤY NGHỈ PHÉP","GNP",DocumentTypeSource.DecreeAdministrative),
        new("Phiếu gửi","PHIẾU GỬI","PG",DocumentTypeSource.DecreeAdministrative),
        new("Phiếu chuyển","PHIẾU CHUYỂN","PC",DocumentTypeSource.DecreeAdministrative),
        new("Phiếu báo","PHIẾU BÁO","PB",DocumentTypeSource.DecreeAdministrative),
        new("Bản sao y","BẢN SAO Y","SY",DocumentTypeSource.DecreeCopy),
        new("Bản trích sao","BẢN TRÍCH SAO","TrS",DocumentTypeSource.DecreeCopy),
        new("Bản sao lục","BẢN SAO LỤC","SL",DocumentTypeSource.DecreeCopy),
        new("Báo cáo đề xuất","BÁO CÁO ĐỀ XUẤT","BCĐX",DocumentTypeSource.UserSupplement)
    });
    private static string Clean(string text)=>text.TrimEnd('\r','\a','\n').Trim();
    public static DocumentTypeEntry? FindHeading(string text)
    {
        string name=Clean(text);
        // Both accepted tone-mark spellings keep their original letters; a case
        // repair never changes uỷ/ủy into the other spelling.
        if(name.Equals("GIẤY ỦY QUYỀN",StringComparison.OrdinalIgnoreCase))name="GIẤY UỶ QUYỀN";
        return Entries.SingleOrDefault(e=>e.NamedHeading && e.Heading.Equals(name,StringComparison.OrdinalIgnoreCase));
    }
    public static bool IsKnownAbbreviation(string text)=>Entries.Any(e=>e.Abbreviation.Equals(text,StringComparison.Ordinal));
    // Case-only lookup: never expand, remove accents or guess an unknown code.
    public static DocumentTypeEntry? FindAbbreviation(string text)=>Entries.SingleOrDefault(e=>e.Abbreviation.Equals(text,StringComparison.OrdinalIgnoreCase));
    public static bool IsUserHeading(string text)=>FindHeading(text)?.Source==DocumentTypeSource.UserSupplement;
    public static string WithSource(string citation,bool userSupplement)=>userSupplement?citation+" "+UserNote:citation;
}
