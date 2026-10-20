using ChinhTa.Core;

var checks = 0;
void Assert(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name); checks++;
}
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
var pair = Checker.Check([new(0, Checker.Heading, true, 12), new(100, HeadingRules.Motto, true, 14)], DocumentProfile.State);
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
Console.WriteLine($"{checks} checks passed.");
