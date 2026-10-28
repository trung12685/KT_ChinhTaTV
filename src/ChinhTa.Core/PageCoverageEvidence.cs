using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace ChinhTa.Core;

public sealed record PageCoverageEvidence(IReadOnlyDictionary<string,CoverageObservation> Observations,SectionLayout? NumberPreviewLayout)
{
    public static PageCoverageEvidence Read(DocumentProfile profile,IReadOnlyList<SectionLayout> sections,PageNumberPackage? package,
        bool completeTextProof,int? pages)
    {
        var observations=new Dictionary<string,CoverageObservation>();SectionLayout? preview=null;
        if(profile!=DocumentProfile.State)return new(new ReadOnlyDictionary<string,CoverageObservation>(observations),null);
        int known=sections.Count(PageLayoutFix.Known);
        // Include settings as well as the body: book-fold and mirror options
        // must not disappear merely because coverage uses a package snapshot.
        var xml=package is null?null:new XDocument(new XElement("snapshot",package.Parts.Values
            .Where(p=>p.Root is not null).Select(p=>new XElement(p.Root!))));
        bool ordinary=completeTextProof && sections.Count==1 && known==1 && xml is not null && PageLayoutGuard.Eligible(xml,sections[0]);
        observations["C32"]=new(ordinary?CoverageState.Observed:known>0?CoverageState.Partial:CoverageState.Unavailable,
            sections.Count,known,ordinary?"Đã đọc khổ/lề của một section dọc thông thường trong phạm vi hỗ trợ; không chứng nhận toàn bộ bố cục.":
            "Chỉ kiểm được phần thông số trang đã đọc; nhiều section, header/footer, hình, cột hoặc bố cục chưa rõ còn cần đối chiếu thủ công.");
        if(package is null || pages is null or <1 or >10000)
            observations["C33"]=new(CoverageState.Unavailable,0,0,"Chưa đọc đủ phần đầu/cuối trang hoặc số trang hiện tại; không kết luận thiếu số trang.");
        else
        {
            try
            {
                var number=PageNumberRules.Analyze(package,pages.Value);
                bool read=completeTextProof && number.Covered && number.Repairable;
                if(read && number.Errors.Count==0 && number.Layout is not null)
                {
                    preview=number.Layout;
                    observations["C33"]=new(CoverageState.PendingGeometry,1,0,"Đã đọc thiết lập số trang; cần xác minh số thực sự hiển thị trên bản xem trước.");
                }
                else observations["C33"]=new(CoverageState.Partial,1,read?1:0,read?
                    "Đã đọc thiết lập và có cảnh báo số trang; chưa xác minh trọn phần hiển thị hiện tại.":
                    "Đầu/cuối trang hoặc cấu trúc số trang còn ngoài phạm vi; không suy từ không có cảnh báo rằng số trang đã đúng.");
            }
            catch(Exception error) when(error is InvalidOperationException or System.Xml.XmlException)
            {observations["C33"]=new(CoverageState.Unavailable,1,0,"Chưa đối chiếu được cấu trúc số trang hiện tại; cần kiểm tra thủ công.");}
        }
        return new(new ReadOnlyDictionary<string,CoverageObservation>(observations),preview);
    }
}
