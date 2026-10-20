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
Console.WriteLine($"{checks} checks passed.");
