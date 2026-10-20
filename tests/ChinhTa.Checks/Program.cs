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
Console.WriteLine($"{checks} checks passed.");
