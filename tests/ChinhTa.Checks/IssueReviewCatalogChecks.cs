using ChinhTa.Core;

public static class IssueReviewCatalogChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var space=new Issue(10,3,"   "," ","detail",false);
        var info=IssueReviewCatalog.Describe(space);
        assert(info.Label=="Khoảng trắng thừa" && info.Section==IssueReviewSection.ReviewSuggestion,"Spacing remains a reviewable suggestion with a concise label");
        foreach(var kind in new[]{FixKind.AdministrativeNumber,FixKind.AdministrativeDate,FixKind.AdministrativeFormat,FixKind.DocumentHeadingFormat})
        {
            var issue=space with {Kind=kind,Citation="Căn cứ thể thức"};
            assert(IssueReviewCatalog.Describe(issue).Section==IssueReviewSection.RequiredFormat,"Cited supported formatting requirements join the required-format section");
            assert(IssueReviewCatalog.Describe(issue with {CanFix=false}).Section==IssueReviewSection.ManualWarning,"Manual eligibility always overrides required-format classification");
        }
        var heading=new Issue(0,3,"abc","ABC","",false,FixKind.NationalHeading,Citation:"NĐ30",Format:new(13));
        assert(IssueReviewCatalog.Describe(heading).Section==IssueReviewSection.RequiredFormat,"A cited heading correction has a required-format section");
        assert(IssueReviewCatalog.Describe(heading with {Original="ABC",Format=new(13,1)}).Section==IssueReviewSection.ReviewSuggestion,
            "A heading with a local alignment request is kept reviewable when presentation cannot establish a separate character defect");
        var missing=new Issue(0,3,"ABC","ABC","",false,FixKind.ShapeLine,Citation:"NĐ30",Title:"Thiếu đường kẻ dưới cơ quan ban hành",LineFix:new(null,100,106,90,LineRole.Agency));
        assert(IssueReviewCatalog.Describe(missing).Label==missing.Title && IssueReviewCatalog.Describe(missing).Section==IssueReviewSection.RequiredFormat,
            "A verified missing cited Line is named as missing and classified as a format requirement");
        assert(IssueReviewCatalog.Describe(missing with {LineFix=missing.LineFix! with {ShapeId=1},Title="Đường kẻ dưới cơ quan ban hành chưa đúng"}).Section==IssueReviewSection.ReviewSuggestion,
            "Existing-Line geometry based on configured tolerances stays reviewable rather than being called missing or mandatory");
        assert(IssueReviewCatalog.Describe(missing with {CanFix=false,LineFix=null}).Section==IssueReviewSection.ManualWarning,
            "An uncertain or blocked Line cannot be made repairable by its display category");
        assert(IssueReviewCatalog.Describe(space with {Kind=FixKind.AdministrativePosition,Citation="NĐ30"}).Section==IssueReviewSection.ReviewSuggestion,
            "Optional centering remains a reviewable presentation action");
        assert(IssueReviewCatalog.Describe(heading with {Citation=null}).Section==IssueReviewSection.ReviewSuggestion,"Missing regulatory evidence cannot produce a required-format label");
        assert(IssueReviewCatalog.Describe(space with {Original="đo",Replacement="đò"}).FamilyKey!="spacing","The demo spelling proposal is separate from generic spacing");
        foreach(var kind in new[]{FixKind.RepeatedText,FixKind.PunctuationSpace,FixKind.ParenthesisSpace,FixKind.SentenceEndSpace,FixKind.ColonSpace,FixKind.SentenceInnerSpace,FixKind.SentenceAfterSpace})
            assert(IssueReviewCatalog.Describe(space with {Kind=kind}).Section==IssueReviewSection.ReviewSuggestion,"Language and punctuation proposals always retain review status");
        foreach(var kind in new[]{FixKind.OutlineOrder,FixKind.OutlineMarker})
        {
            var item=space with {Kind=kind};
            assert(IssueReviewCatalog.Describe(item).Section==IssueReviewSection.RequiredFormat,
                "Repairable outline order and singleton markers follow the user's required-section preference without a regulatory citation");
            assert(IssueReviewCatalog.Describe(item with {CanFix=false}).Section==IssueReviewSection.ManualWarning,
                "An ambiguous outline remains manual even when the repairable family is required by user preference");
            assert(item.Citation is null && !item.BatchSafe,"The outline display preference grants neither legal evidence nor new repair eligibility");
        }
        string longTitle="  "+new string('a',150)+"\r\n\t cuối";
        var brief=IssueReviewCatalog.Describe(space with {Kind=FixKind.Advisory,CanFix=false,Title=longTitle});
        assert(brief.Label.Length<=88 && !brief.Label.Any(char.IsControl) && brief.Label.EndsWith("…"),"Long or multiline titles are bounded single-line labels without altering the original detail");
        assert(IssueReviewCatalog.SectionDetail(IssueReviewSection.RequiredFormat).Contains("duyệt") &&
            IssueReviewCatalog.SectionDetail(IssueReviewSection.ManualWarning).Contains("Không tích"),"Section explanations preserve review and manual handling");
        assert(space.Original=="   " && !space.BatchSafe && space.CanFix && space.Kind==FixKind.Text,"Review metadata never changes repair eligibility, payload or transaction flags");
    }
}
