using System.Globalization;
using ChinhTa.Core;

public static class IssueTooltipChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var space=new Issue(7,3,"   "," ",new string('x',2000),false);
        var missing=new Issue(0,3,"ABC","ABC","Số đo dài chỉ xuất hiện trong chi tiết.",false,FixKind.ShapeLine,
            Citation:ShapeLineRules.Citation(LineRole.Agency),Title:"Thiếu đường kẻ dưới cơ quan ban hành",LineFix:new(null,100,106,95.2,LineRole.Agency));
        var tooltip=IssueTooltipCatalog.Describe(missing);
        assert(tooltip.Error==missing.Title && tooltip.Basis.Contains("II.2, điểm b") && tooltip.Basis.Contains("1/3–1/2") &&
            tooltip.Basis.Contains("nét liền") && tooltip.Action.Contains("95.2 pt") && tooltip.Action.Contains("3/7"),
            "Agency tooltip includes the precise appendix provision, its content and actual proposed length");
        assert(!tooltip.Basis.Contains("3/7") && !tooltip.Basis.Contains("6 pt") && !tooltip.Text.Contains("Thông tư") &&
            tooltip.Action.Contains("cấu hình"),"User target and measurement policy are separated from the decree provision");
        foreach(var role in Enum.GetValues<LineRole>())
        {
            var line=missing with {Citation=ShapeLineRules.Citation(role),LineFix=missing.LineFix! with {Role=role,ShapeId=5}};
            var info=IssueTooltipCatalog.Describe(line);
            assert(info.Action.StartsWith("Đặt lại") && (role==LineRole.Motto?
                info.Basis.Contains("II.1, điểm b") && info.Basis.Contains("dài bằng") && !info.Action.Contains("3/7"):
                info.Basis.Contains(role==LineRole.Agency?"II.2, điểm b":"II.5, điểm b")),
                "Every Line role uses its own provision and existing-Line action");
        }
        var blocked=missing with {CanFix=false,LineFix=null,Message="Sửa nội dung/định dạng trong vùng trước rồi quét lại."};
        assert(IssueTooltipCatalog.Describe(blocked).Basis==tooltip.Basis && IssueTooltipCatalog.Describe(blocked).Action.Contains("rồi quét lại"),
            "Blocked Line retains its role basis without offering to create a duplicate");
        assert(IssueTooltipCatalog.Describe(blocked with {Message="Chưa đo được"}).Action.Contains("thủ công"),
            "Unmeasured or manual-only Line stays manual in the concise tooltip");
        var national=HeadingRules.Check(new(0,Checker.Heading,Bold:false,Size:11,InTable:true),DocumentProfile.State).Single();
        var ni=IssueTooltipCatalog.Describe(national);
        assert(ni.Basis.Contains("II.1, điểm a") && ni.Basis.Contains("12–13") && ni.Basis.Contains("theo yêu cầu người dùng") &&
            ni.Action.Contains("cỡ 12") && ni.Action.Contains("căn giữa trong ô"),"Heading basis distinguishes local user alignment and actual target size");
        var motto=HeadingRules.Check(new(0,HeadingRules.Motto,Bold:false,Size:14),DocumentProfile.State).Single();
        assert(IssueTooltipCatalog.Describe(motto).Basis.Contains("II.1, điểm b") && IssueTooltipCatalog.Describe(motto).Action.Contains("cỡ 14"),
            "Motto tooltip keeps a valid existing size rather than promising the lower bound");
        var party=HeadingRules.Check(new(0,HeadingRules.PartyTitle,Bold:false),DocumentProfile.Party).Single();
        assert(IssueTooltipCatalog.Describe(party).Basis.Contains("05-HD/VPTW (27/5/2026)") &&
            IssueTooltipCatalog.Describe(party).Basis.Contains("Phụ lục 2, mục 1") && !IssueTooltipCatalog.Describe(party).Basis.Contains("Nghị định"),
            "Party heading keeps its own source, provision and content");
        foreach(bool date in new[]{false,true})
        {
            var font=space with {Kind=FixKind.AdministrativeFormat,Citation="NĐ 30/2020/NĐ-CP, Phụ lục I.",CharacterFormat=new(date?14:13,date)};
            var info=IssueTooltipCatalog.Describe(font);
            assert(info.Basis.Contains(date?"II.4, điểm c":"II.3, điểm c") && info.Action.Contains(date?"cỡ 14, chữ nghiêng":"cỡ 13, chữ đứng"),
                "Number/date typography names the correct provision and actual character-format target");
        }
        foreach(var kind in new[]{FixKind.AdministrativeNumber,FixKind.AdministrativeDate,FixKind.DocumentHeadingFormat,FixKind.AdministrativePosition,FixKind.PageLayout})
        {
            var info=IssueTooltipCatalog.Describe(space with {Kind=kind,Citation="NĐ30, PL I.",CanFix=false});
            assert(info.Basis.Contains("Nghị định 30/2020/NĐ-CP") && info.Basis.Contains(": ") && info.Action.Contains("thủ công"),
                "Supported State format warnings include provision content while retaining manual eligibility");
        }
        foreach(var kind in new[]{FixKind.Text,FixKind.RepeatedText,FixKind.PunctuationSpace,FixKind.ParenthesisSpace,FixKind.SentenceEndSpace,
            FixKind.ColonSpace,FixKind.SentenceInnerSpace,FixKind.SentenceAfterSpace,FixKind.OutlineOrder,FixKind.OutlineMarker})
        {
            var info=IssueTooltipCatalog.Describe(space with {Kind=kind});
            assert(!info.Basis.Contains("Nghị định") && !info.Basis.Contains("Thông tư") &&
                info.Basis.Contains(kind is FixKind.OutlineOrder or FixKind.OutlineMarker?"yêu cầu người dùng":"Quy tắc ứng dụng"),
                "Language and user-requested outline rules do not acquire invented legal citations");
        }
        var unknown=IssueTooltipCatalog.Describe(space with {Kind=FixKind.Advisory,CanFix=false,Citation="Nguồn khác, mục 7.",Title="Kiểm tra cấu trúc"});
        assert(unknown.Basis.Contains("Nguồn khác, mục 7") && unknown.Basis.Contains("đối chiếu") && !unknown.Basis.Contains("Nghị định"),
            "Unknown references are retained without inventing the provision's content");
        var previous=CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture=new CultureInfo("vi-VN");
            foreach(var issue in new[]{space,missing,blocked,national,motto,party,space with {Kind=FixKind.Advisory,CanFix=false}})
            {
                string text=IssueTooltipCatalog.Describe(issue).Text;
                assert(text.Split('\n') is [var error,var basis,var action] && error.StartsWith("Lỗi: ") && basis.StartsWith("Căn cứ: ") &&
                    action.StartsWith("Cách xử lý: ") && text.Length<700 && !text.Contains(issue.Message),
                    "Tooltips have exactly three concise blocks independent of long diagnostics and culture");
            }
        }
        finally{CultureInfo.CurrentCulture=previous;}
        assert(missing.CanFix && missing.LineFix!.Width==95.2 && !blocked.CanFix && space.Message.Length==2000,
            "Generating presentation summaries leaves original payload, diagnostics and repair eligibility unchanged");
    }
}
