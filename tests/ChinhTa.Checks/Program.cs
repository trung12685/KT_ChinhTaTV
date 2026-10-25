using ChinhTa.Core;

var checks = 0;
void Assert(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name); checks++;
}
DocxLineMetadataChecks.Run(Assert);
LinePlacementFeedbackChecks.Run(Assert);
BatchAndSpacingChecks.Run(Assert);
GlyphAdvanceChecks.Run(Assert);
LineReferenceBindingChecks.Run(Assert);
AdministrativeHeaderChecks.Run(Assert);
AtomicTextBatchChecks.Run(Assert);
AdministrativeTypographyChecks.Run(Assert);
AdministrativePositionChecks.Run(Assert);
AdministrativeCenteringChecks.Run(Assert);
HeaderReviewChecks.Run(Assert);
VerifiedSaveCopyChecks.Run(Assert);
DocumentHeadingChecks.Run(Assert);
StructuredTextChecks.Run(Assert);
WholeBodyTextProofChecks.Run(Assert);
SentenceInnerSpaceChecks.Run(Assert);
SentenceAfterSpaceChecks.Run(Assert);
ShortFinalSentenceChecks.Run(Assert);
IssueReviewCatalogChecks.Run(Assert);
IssueTooltipChecks.Run(Assert);
StagedRepairChecks.Run(Assert);
RepairDependencyChecks.Run(Assert);
PreviewAnchorBatchChecks.Run(Assert);
ComponentChecks.Run(Assert);
FormatAssessmentChecks.Run(Assert);
FontSizeEvidenceChecks.Run(Assert);
SelectiveSizeChecks.Run(Assert);
HeadingSelectiveSizeChecks.Run(Assert);
CasePreservationChecks.Run(Assert);
OwnedLineFormatChecks.Run(Assert);
RepairProgressChecks.Run(Assert);
DocumentTypeTextChecks.Run(Assert);
DocumentTypeCatalogChecks.Run(Assert);
DocumentTypeConsistencyChecks.Run(Assert);
OutlineChecks.Run(Assert);
HierarchicalOutlineChecks.Run(Assert);
HeadingPostSaveChecks.Run(Assert);
CheckpointCleanupChecks.Run(Assert);
RepeatedTextChecks.Run(Assert);
InitialRepeatChecks.Run(Assert);
PunctuationSpaceChecks.Run(Assert);
ParentheticalPunctuationChecks.Run(Assert);
ParenthesisSpaceChecks.Run(Assert);
ParenthesisAfterSpaceChecks.Run(Assert);
SentenceEndSpaceChecks.Run(Assert);
ParagraphEndSpaceChecks.Run(Assert);
SemicolonEndSpaceChecks.Run(Assert);
BodyFormatChecks.Run(Assert);
BodyParagraphChecks.Run(Assert);
BodySpacingChecks.Run(Assert);
BodyOpeningChecks.Run(Assert);
BodyEmphasisChecks.Run(Assert);
BodyEmphasisLayoutChecks.Run(Assert);
BodyRuleScanChecks.Run(Assert);
ColonSpaceChecks.Run(Assert);
ColonAfterSpaceChecks.Run(Assert);
DevelopmentReportChecks.Run(Assert);
var text = "Tôi đi trên một con đo. Đây  là văn bản.\r";
var issues = Checker.Check([new(37, text)]);
Assert(issues.Count == 2, "Hai lỗi minh họa");
var context = issues.Single(x => x.Replacement == "đò");
Assert(text.Substring(context.Start - 37, context.Length) == "đo", "Vị trí tiếng Việt và offset Word");
Assert(!context.BatchSafe, "Không tự sửa lỗi ngữ cảnh");
Assert(Checker.Check([new(0, "Tôi đo chiều dài. Con đường này đẹp.\r")]).Count == 0, "Không cảnh báo đo hoặc con đường");
Assert(Checker.Check([new(0, Checker.Heading + "\r\a", true)]).Count == 0, "Quốc hiệu trong ô bảng đúng mẫu");
var heading = Checker.Check([new(10, Checker.Heading.ToLowerInvariant() + "\r", false, 12, 0)]).Single();
Assert(heading.Kind == FixKind.NationalHeading && heading.Length == Checker.Heading.Length, "Lỗi quốc hiệu không gồm dấu kết thúc đoạn");
Assert(Checker.Check([new(0, "    Nội dung\r")]).Count == 0, "Không xóa thụt đầu dòng");
Assert(Checker.Check([new(0, "con đo; con đo")]).Count == 2, "Phân biệt nhiều vị trí giống nhau");
Assert(Checker.Check([new(0, "con đò")]).Count == 0, "Văn bản đã sửa không còn lỗi mẫu");
var body = "Đầu trang\rMột con đo.\rTrong bảng: con đo.\r";
int second = body.LastIndexOf("đo", StringComparison.Ordinal);
var anchor = new PreviewAnchor(1, body, second, 2);
var rendered = "TIÊU ĐỀĐầu trangMột con đo.Trong bảng: con đo.CHÂN TRANG";
var located = PreviewLocator.Locate(anchor, rendered);
Assert(located is not null && located.Start == rendered.LastIndexOf("đo", StringComparison.Ordinal),
    "Ngữ cảnh phân biệt từ giống nhau trong bảng");
var page = "Một con đo. Lại một con đo.";
int pos = page.LastIndexOf("đo", StringComparison.Ordinal);
var repeated = PreviewLocator.Locate(new(1, page, pos, 2), page);
Assert(repeated?.Start == pos, "Offset phân biệt hai lỗi trong cùng trang");
Assert(PreviewLocator.Locate(new(1, "con đo", 4, 2), "con đocon đo") is null,
    "Không chọn lần xuất hiện đầu khi ánh xạ mơ hồ");
var spaced = PreviewLocator.Locate(new(1, "Đây  là mẫu", 3, 2), "Đây  là mẫu");
Assert(spaced == new TextSpan(3, 2), "Khoảng trắng được đánh dấu riêng");
Assert(PreviewLocator.Locate(new(1, "Đây  là mẫu", 3, 2), "Đâylàmẫu") is null,
    "Không tô từ bên cạnh khi XPS không có ký tự khoảng trắng");
var split = PreviewLocator.Locate(new(1, "Tôi đi con đo", 11, 2), "Tôi đi\ncon đo");
Assert(split is not null && "Tôi đi\ncon đo".Substring(split.Start, split.Length) == "đo",
    "Xuống dòng không làm lệch vị trí tiếng Việt");
Assert(PreviewLocator.Locate(new(1, "con đo", 4, 2), "con đò") is null,
    "Nội dung cũ không được ánh xạ lên bản sửa");
Assert(PreviewLocator.Locate(new(1, "con đo", -1, 2), "con đo") is null,
    "Từ chối vị trí ngoài trang");
Assert(PreviewLocator.Locate(new(1, "con đo", 4, 8), "con đo") is null,
    "Từ chối lỗi vượt ranh giới trang");
var longBody = "Nội dung đầu khác" + new string('a', 60) + "con đo" + new string('b', 60) + "Cuối";
var changedOrder = "Đầu trang XPS" + new string('a', 60) + "con đo" + new string('b', 60) + "Chân trang";
var contextual = PreviewLocator.Locate(new(1, longBody, longBody.IndexOf("đo", StringComparison.Ordinal), 2), changedOrder);
Assert(contextual?.Start == changedOrder.IndexOf("đo", StringComparison.Ordinal),
    "Ngữ cảnh duy nhất khi thứ tự nội dung ngoài vùng lỗi khác nhau");
var rpc = new System.Runtime.InteropServices.COMException("RPC", unchecked((int)0x800706BA));
Assert(WordConnectionPolicy.IsDisconnected(rpc), "Nhận diện Word RPC server unavailable");
int attempts = 0, recoveries = 0;
int recovered = WordConnectionPolicy.ReadWithRecovery(() =>
{
    if (++attempts == 1) throw rpc;
    return 42;
}, () => recoveries++);
Assert(recovered == 42 && attempts == 2 && recoveries == 1, "Khôi phục đúng một lần rồi đọc lại");
attempts = recoveries = 0;
try { WordConnectionPolicy.ReadWithRecovery<int>(() => { attempts++; throw rpc; }, () => recoveries++); }
catch (System.Runtime.InteropServices.COMException) { }
Assert(attempts == 2 && recoveries == 1, "Không lặp vô hạn khi Word tiếp tục lỗi");
attempts = recoveries = 0;
try { WordConnectionPolicy.ReadWithRecovery<int>(() => { attempts++; throw new IOException("file"); }, () => recoveries++); }
catch (IOException) { }
Assert(attempts == 1 && recoveries == 0, "Không khởi động Word lại vì lỗi file thông thường");
attempts = 0;
try { WordConnectionPolicy.ReadWithRecovery<int>(() => { attempts++; throw rpc; }, () => throw new InvalidOperationException("unsaved")); }
catch (InvalidOperationException) { }
Assert(attempts == 1, "Không đọc lại khi khôi phục từ chối vì thay đổi chưa lưu");
var rpcCallFailed = new System.Runtime.InteropServices.COMException("RPC call failed", unchecked((int)0x800706BE));
Assert(WordConnectionPolicy.IsDisconnected(rpcCallFailed), "Nhận diện đúng mã 0x800706BE trong nhật ký Office 2021");
attempts = recoveries = 0;
WordConnectionPolicy.ReadWithRecovery(() => { if (++attempts == 1) throw rpcCallFailed; return true; }, () => recoveries++);
Assert(attempts == 2 && recoveries == 1, "Lỗi 0x800706BE được khôi phục một lần");
var story = "Đây là nội dung ở trang trước rất dài." + new string('x', 60) + "Từduynhất" + "Phần sau ở trang khác";
var edge = PreviewLocator.Locate(new(2, story, story.IndexOf("Từduynhất", StringComparison.Ordinal), 9), "Từduynhất");
Assert(edge is not null && edge.Start == 0, "Ánh xạ đầu trang không cần Word GoTo");
var ambiguousStory = "con đo" + new string('x', 100) + "con đo";
Assert(PreviewLocator.Locate(new(1, ambiguousStory, 4, 2), "con đo") is null,
    "Không thu ngắn ngữ cảnh rồi đánh dấu nhầm cụm lặp");
var batchText = "Đây  là con đo. Một  lần.";
var allBatchIssues = Checker.Check([new(0, batchText)]);
var selectedBatchIssues = allBatchIssues.Where(x => x.Replacement != "đò").ToArray();
var plan = BatchPlan.Create(selectedBatchIssues);
BatchPlan.Validate(plan, x => batchText.Substring(x.Start, x.Length));
var fixedBatch = batchText;
foreach (var issue in plan)
    fixedBatch = fixedBatch.Remove(issue.Start, issue.Length).Insert(issue.Start, issue.Replacement);
Assert(fixedBatch == "Đây là con đo. Một lần.", "Sửa nhiều khoảng trắng không lệch vị trí và giữ lỗi chưa chọn");
var allPlan = BatchPlan.Create(allBatchIssues);
fixedBatch = batchText;
BatchPlan.Validate(allPlan, x => fixedBatch.Substring(x.Start, x.Length));
foreach (var issue in allPlan)
    fixedBatch = fixedBatch.Remove(issue.Start, issue.Length).Insert(issue.Start, issue.Replacement);
Assert(fixedBatch == "Đây là con đò. Một lần.", "Sửa toàn bộ gợi ý tiếng Việt theo thứ tự từ cuối");
Assert(BatchPlan.Create([allBatchIssues[0], allBatchIssues[0]]).Count == 1, "Không sửa trùng một lỗi hai lần");
bool staleRejected = false;
try { BatchPlan.Validate(allPlan, _ => "đã thay đổi"); }
catch (InvalidOperationException) { staleRejected = true; }
Assert(staleRejected, "Kiểm tra nội dung cũ trước khi áp dụng lượt sửa");
bool overlapRejected = false;
try { BatchPlan.Create([new(0, 3, "abc", "x", "", false), new(2, 2, "cd", "y", "", false)]); }
catch (InvalidOperationException) { overlapRejected = true; }
Assert(overlapRejected, "Từ chối lỗi chồng lấn trước khi sửa");
Assert(BatchPlan.Create([]).Count == 0, "Danh sách rỗng không tạo thao tác sửa");
var badHeading = new Paragraph(0, Checker.Heading.ToLowerInvariant(), false, 12, 0);
Assert(Checker.Check([badHeading], DocumentProfile.Personal).Count == 0,
    "Cá nhân không bị bắt buộc quốc hiệu hành chính");
Assert(Checker.Check([badHeading], DocumentProfile.Party).Count == 0,
    "Văn bản Đảng không dùng quy tắc quốc hiệu Nhà nước");
Assert(Checker.Check([badHeading], DocumentProfile.State).Count == 1,
    "Nhà nước dùng quy tắc quốc hiệu riêng");
Assert(Enum.GetValues<DocumentProfile>().All(profile => Checker.Check([new(0, "con đo. Đây  là mẫu")], profile).Count == 2),
    "Các chế độ cùng kiểm tra ngữ cảnh minh họa và khoảng trắng");
Assert(Checker.Check([new(0, Checker.Heading, true, 12)], DocumentProfile.State).Count == 0,
    "NĐ30 chấp nhận quốc hiệu cỡ 12, không ép mọi văn bản về 13");
Assert(Checker.Check([new(0, Checker.Heading, true, 13)], DocumentProfile.State).Count == 0,
    "NĐ30 chấp nhận quốc hiệu cỡ 13");
var wrongFontHeading = Checker.Check([new(0, Checker.Heading, true, 13, Font: "Arial")], DocumentProfile.State).Single();
Assert(wrongFontHeading.Format?.Size == 13 && wrongFontHeading.Citation?.Contains("trang 21") == true,
    "Sửa font giữ cỡ hợp lệ và có căn cứ PDF");
Assert(Checker.Check([new(0, HeadingRules.Motto, true, 14)], DocumentProfile.State).Count == 0,
    "Tiêu ngữ cỡ 14 hợp lệ");
Assert(Checker.Check([new(0, HeadingRules.Motto, true, 12)], DocumentProfile.State).Single().Format?.Size == 13,
    "Tiêu ngữ cỡ 12 được gợi ý sửa về 13");
Assert(Checker.Check([new(0, HeadingRules.PartyTitle, true, 15)], DocumentProfile.Party).Count == 0,
    "HD05 tiêu đề Đảng cỡ 15 hợp lệ");
Assert(Checker.Check([new(0, HeadingRules.PartyTitle, true, 14)], DocumentProfile.Party).Single().Format?.Size == 15,
    "Tiêu đề Đảng cỡ 14 phải sửa theo Phụ lục 2");
Assert(Checker.Check([new(0, HeadingRules.PartyTitle, true, 14)], DocumentProfile.State).Count == 0,
    "Không áp tiêu đề Đảng trong chế độ Nhà nước");
Assert(Checker.Check([new(0, Checker.Heading, true, 12, Italic: true)], DocumentProfile.State).Count == 1,
    "Quốc hiệu nghiêng bị cảnh báo");
Assert(Checker.Check([new(0, Checker.Heading, true, 12, Color: 255)], DocumentProfile.State).Count == 1,
    "Quốc hiệu màu đỏ bị cảnh báo");
Assert(Checker.Check([new(0, Checker.Heading, true, 12, Color: -16777216)], DocumentProfile.State).Count == 0,
    "Màu Automatic không bị gán nhầm là màu đỏ");
Assert(Checker.Check([new(0, HeadingRules.Motto, true, 13, 0, InTable: true)], DocumentProfile.State).Single().Format?.Alignment == 1,
    "Tiêu ngữ trong ô cần căn giữa");
Assert(Checker.Check([new(0, Checker.Heading.ToLowerInvariant(), Page: 3)], DocumentProfile.State).Count == 0,
    "Không coi dòng trích dẫn ở trang sau là quốc hiệu trang đầu");
var standardLayout = new SectionLayout(1, 0, "A", 210, 297, 20, 20, 30, 15);
Assert(!PageRules.Check(standardLayout, DocumentProfile.State).Any(), "Lề Nhà nước ở cận dưới hợp lệ");
Assert(!PageRules.Check(standardLayout with { TopMm=25, BottomMm=25, LeftMm=35, RightMm=20 }, DocumentProfile.State).Any(),
    "Lề Nhà nước ở cận trên hợp lệ");
Assert(!PageRules.Check(standardLayout, DocumentProfile.Party).Any(), "Lề Đảng 20/20/30/15 hợp lệ");
var partyMarginWarning = PageRules.Check(standardLayout with { TopMm=25 }, DocumentProfile.Party).Single();
Assert(!partyMarginWarning.CanFix && partyMarginWarning.Citation?.Contains("28") == true,
    "Lề Đảng sai là cảnh báo thủ công có căn cứ");
Assert(!PageRules.Check(standardLayout with { WidthMm=148, HeightMm=210, TopMm=50 }, DocumentProfile.Personal).Any(),
    "Văn bản cá nhân không bị ép khổ giấy/lề hành chính");
Assert(PageRules.Check(standardLayout with { WidthMm=297, HeightMm=210 }, DocumentProfile.State).All(x => !x.CanFix),
    "Trang ngang được yêu cầu kiểm tra ngoại lệ, không tự sửa");
Assert(PageRules.Check(standardLayout with { WidthMm=148, HeightMm=210 }, DocumentProfile.Party).Single().Message.Contains("A5"),
    "Có nhắc ngoại lệ giấy tờ hành chính A5 của Đảng");
Assert(!PageRules.Check(standardLayout with { MirrorMargins=true }, DocumentProfile.Party).Any(),
    "Lề trong/ngoài đúng của Đảng không bị cảnh báo nhầm");
Assert(Checker.Check([new(0, "\t\tNội dung", Alignment: 0)], DocumentProfile.Personal).Single().CanFix == false,
    "Tab liên tiếp cá nhân chỉ gợi ý, không tự xóa");
bool manualRejected = false;
try { BatchPlan.Create([partyMarginWarning]); } catch (InvalidOperationException) { manualRejected=true; }
Assert(manualRejected, "Cảnh báo thủ công không lọt vào động cơ sửa tất cả");
Assert(Checker.Check([new(0, Checker.Heading.Replace("HÒA", "HOÀ"), true, 12)], DocumentProfile.State).Count == 0,
    "Chấp nhận biến thể HOÀ trong mẫu phụ lục, không tự đổi vị trí dấu");
var headingSpaces = Checker.Check([new(0, Checker.Heading.Replace("XÃ HỘI", "XÃ  HỘI"), true, 12)], DocumentProfile.State);
Assert(headingSpaces.Count == 1 && headingSpaces[0].Kind == FixKind.NationalHeading,
    "Lỗi khoảng trắng trong quốc hiệu gộp vào sửa thành phần, không chồng lấn");
var pair = Checker.Check([new(0, Checker.Heading, true, 12,FormatObserved:true), new(100, HeadingRules.Motto, true, 14,FormatObserved:true)], DocumentProfile.State);
Assert(pair.Count == 1 && !pair[0].CanFix, "Cỡ quốc hiệu/tiêu ngữ cần thống nhất theo chú thích phụ lục");
Assert(Checker.Check([new(0, Checker.Heading, true, 13), new(100, HeadingRules.Motto, true, 14)], DocumentProfile.State).Count == 0,
    "Cặp quốc hiệu 13 và tiêu ngữ 14 hợp lệ");
var enDashMotto = HeadingRules.Motto.Replace("-", "–");
Assert(Checker.Check([new(0, enDashMotto, true, 13)], DocumentProfile.State).Count == 0,
    "Tiêu ngữ dùng en dash do Word tự đổi không bị báo lỗi");
var mixedDashMotto = "Độc lập - Tự do – Hạnh phúc";
Assert(Checker.Check([new(0, mixedDashMotto, true, 14)], DocumentProfile.State).Count == 0,
    "Tiêu ngữ trộn hyphen và en dash vẫn hợp lệ");
var enDashFontFix = Checker.Check([new(0, enDashMotto, true, 13, Font: "Arial")], DocumentProfile.State).Single();
Assert(enDashFontFix.Kind == FixKind.StateMotto && enDashFontFix.Replacement == enDashMotto,
    "Sửa font tiêu ngữ giữ nguyên en dash, không thay nội dung");
var enDashSizeFix = Checker.Check([new(0, enDashMotto, true, 12)], DocumentProfile.State).Single();
Assert(enDashSizeFix.Replacement == enDashMotto && enDashSizeFix.Format?.Size == 13,
    "Sửa cỡ chữ vẫn giữ nguyên dấu Word đang dùng");
var mixedCaseFix = Checker.Check([new(0, mixedDashMotto.ToLowerInvariant(), true, 13)], DocumentProfile.State).Single();
Assert(mixedCaseFix.Replacement == mixedDashMotto,
    "Sửa hoa thường giữ đúng từng dấu phân cách ban đầu");
Assert(Checker.Check([new(0, Checker.Heading, true, 13), new(100, enDashMotto, true, 14)], DocumentProfile.State).Count == 0,
    "Cặp quốc hiệu/tiêu ngữ không báo sai do en dash");
var lineParagraphs = new LayoutParagraph[]
{
    new(0,"CƠ QUAN CHỦ QUẢN\r",1,true,0,100,true),
    new(25,"CƠ QUAN BAN HÀNH\r",1,true,0,100,true),
    new(50,"Số: 01/BC\r",1,true,0,100,true),
    new(100, Checker.Heading + "\r",1,true,100,200,true),
    new(140,HeadingRules.Motto + "\r",1,true,100,200,true),
    new(200,"BÁO CÁO\r",1,false,0,0,true),
    new(215,"Trích yếu dòng đầu\r",1,false,0,0,true),
    new(240,"Dòng cuối trích yếu\r",1,false,0,0,true),
    new(265,"\r",1,false,0,0,false),
    new(266,"Kính gửi: cơ quan\r",1,false,0,0,false)
};
var lineTargets = ShapeLineRules.Identify(lineParagraphs,DocumentProfile.State);
Assert(lineTargets.Count == 3 && lineTargets.Single(t=>t.Role==LineRole.Agency).Start==25,
    "Nhận diện ba đường kẻ và cơ quan dưới cùng, không lấy cơ quan chủ quản");
var subjectTarget = lineTargets.Single(t=>t.Role==LineRole.Subject);
Assert(subjectTarget.Start==240 && subjectTarget.Text=="Dòng cuối trích yếu",
    "Chọn đoạn cuối của trích yếu, không lấy Kính gửi");
Assert(!ShapeLineRules.Identify(lineParagraphs,DocumentProfile.Party).Any() &&
       !ShapeLineRules.Identify(lineParagraphs,DocumentProfile.Personal).Any(),
    "Bộ ba Shape Line chỉ áp dụng Nhà nước");
var agencyTarget = lineTargets.Single(t=>t.Role==LineRole.Agency);
var referenceLine = new LineReference(100,100,210);
var missingLine = ShapeLineRules.Evaluate(agencyTarget, referenceLine,null,null,LinePolicy.Decree30);
Assert(missingLine.Errors.Length==1 && Math.Abs(missingLine.Fix.Width-90)<.01 && missingLine.Fix.Top==106,
    "Thiếu line đề xuất đích 3/7 và khoảng cách 6pt theo cấu hình");
var correctGeometry = new ChinhTa.Core.LineGeometry(missingLine.Fix.Left,109,missingLine.Fix.Width);
Assert(ShapeLineRules.Evaluate(agencyTarget,referenceLine,correctGeometry,42,LinePolicy.Decree30).Errors.Length==0,
    "Line đúng độ dài/tâm/khoảng cách không bị cảnh báo");
Assert(ShapeLineRules.Evaluate(agencyTarget,referenceLine,correctGeometry with {Left=190},42,LinePolicy.Decree30).Errors.Any(e=>e.Contains("lệch")),
    "Báo lệch tâm khi dịch line quá dung sai");
Assert(ShapeLineRules.Evaluate(agencyTarget,referenceLine,correctGeometry with {Y=120},42,LinePolicy.Decree30).Errors.Any(e=>e.Contains("Khoảng cách")),
    "Báo line cách dưới quá xa");
Assert(ShapeLineRules.Evaluate(agencyTarget,referenceLine,new(145,109,120),42,LinePolicy.Decree30).Errors.Any(e=>e.Contains("Độ dài")),
    "Độ dài vượt 1/2 phải cảnh báo");
Assert(ShapeLineRules.Evaluate(agencyTarget,referenceLine,new(170,109,70),42,LinePolicy.Decree30).Errors.Length==0,
    "Độ dài đúng cận 1/3 được chấp nhận");
Assert(ShapeLineRules.Evaluate(agencyTarget,referenceLine,new(152.5,109,105),42,LinePolicy.Decree30).Errors.Length==0,
    "Độ dài đúng cận 1/2 được chấp nhận");
var mottoTarget = lineTargets.Single(t=>t.Role==LineRole.Motto);
var mottoLine = ShapeLineRules.Evaluate(mottoTarget,referenceLine,null,null,LinePolicy.Decree30);
Assert(mottoLine.Fix.Width==210 && mottoLine.Fix.Top==106,
    "Line dưới tiêu ngữ bằng dòng chữ, mặc định cách 6pt");
Assert(ShapeLineRules.Evaluate(mottoTarget,referenceLine,new(100,103,210),42,LinePolicy.Decree30).Errors.Length==0 &&
       ShapeLineRules.Evaluate(mottoTarget,referenceLine,new(100,113,210),42,LinePolicy.Decree30).Errors.Length==0,
    "Tiêu ngữ chấp nhận hai cận khoảng cách 3 và 13pt");
var unrelatedTable = lineParagraphs.Select(p => p with { TableStart = p.CellStart==100 ? 100 : 0 }).ToArray();
Assert(!ShapeLineRules.Identify(unrelatedTable,DocumentProfile.State).Any(t=>t.Role==LineRole.Agency),
    "Không lấy cơ quan từ bảng khác làm vùng bên trái tiêu ngữ");
XmlTableMapChecks.Run(Assert);
DocxLineEditorChecks.Run(Assert, args.Length >= 1 ? args[0] : null);
if (args.Length >= 1)
{
    var fixtureTargets = ShapeLineRules.Identify(DocxLineFixture.Read(args[0]), DocumentProfile.State);
    var mappedTargets = ShapeLineRules.Identify(DocxLineFixture.ReadThroughXmlMap(args[0]), DocumentProfile.State);
    Assert(mappedTargets.Count == 3 && mappedTargets.All(t => t.Centered),
        "File người dùng: XML map nhận diện ba vùng căn giữa, không gọi COM Tables/Cells");
    Assert(fixtureTargets.Select(t=>t.Role).Distinct().Count()==3,
        "File DOCX được cung cấp nhận diện đủ ba vùng qua XML (chưa kiểm Word/XPS)");
}
if (args.Length >= 2) DocxLineEditorChecks.CheckSavedRepair(Assert,args[1]);
var trailingSubtitle = lineParagraphs.Select(p => p.Start==215 ? p with {Text="Trích yếu dòng đầu \r",Centered=false,ParagraphCentered=true} : p).ToArray();
Assert(ShapeLineRules.Identify(trailingSubtitle,DocumentProfile.State).Single(t=>t.Role==LineRole.Subject).Start==240,
    "Dấu cách cuối đoạn đầu trích yếu không làm mất nhận diện đoạn cuối");
var unsafeLastSubtitle = trailingSubtitle.Select(p=>p.Start==240 ? p with {Centered=false,ParagraphCentered=true}:p).ToArray();
Assert(!ShapeLineRules.Identify(unsafeLastSubtitle,DocumentProfile.State).Single(t=>t.Role==LineRole.Subject).Centered,
    "Khớp cấu trúc căn giữa không tự xác nhận tâm dòng cuối khi chưa đủ điều kiện đo");
var expectedRendered = new ChinhTa.Core.LineGeometry(100,120,80);
Assert(RenderedLineVerifier.Find(expectedRendered,[new(100,220,80)]) is null,
    "Line có cùng độ dài nhưng nằm cạnh Báo cáo không được coi là đã đặt đúng");
Assert(RenderedLineVerifier.Find(expectedRendered,[]) is null,
    "Không tìm thấy nét trên trang không được coi là sửa Line thành công");
Assert(RenderedLineVerifier.Find(expectedRendered,[new(100.25,120.25,79.75)]) is not null,
    "Nét đúng vị trí được chấp nhận trong dung sai xuất trang");
Assert(RenderedLineVerifier.Find(expectedRendered,[new(100,120,160)]) is null,
    "Đầu trái đúng nhưng độ dài sai không được coi là đúng");
Assert(ShapeLineRules.UnidentifiedRoles(lineTargets).Count==0 &&
    ShapeLineRules.UnidentifiedRoles(lineTargets.Where(t=>t.Role!=LineRole.Subject).ToArray()).SequenceEqual([LineRole.Subject]),
    "Trích yếu chưa nhận diện phải có cảnh báo chưa kiểm tra, không im lặng bỏ qua");
Assert(ShapeLineRules.UnidentifiedRoles([]).Count==3,
    "Không nhận diện được mốc nào phải nêu rõ ba vùng chưa được kiểm tra");
Console.WriteLine($"{checks} checks passed.");

