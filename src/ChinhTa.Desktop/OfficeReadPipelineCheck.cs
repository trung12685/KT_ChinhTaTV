using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

// Real Office comparison, executed ONLY by the user's explicit synthetic Office
// check. Compiling this fixture in CI is not evidence that Office has passed it.
internal static class OfficeReadPipelineCheck
{
    public static void Run(WordSession test,string modern,string folder,Action<string> pass)
    {
        bool previous=test.UseOptimizedReading;
        string isolated=Path.Combine(folder,"read-pipeline");Directory.CreateDirectory(isolated);
        try
        {
            void Compare(string label)
            {
                string story=test.ReadCheckText();bool changed=test.Changed;int undo=test.CheckUndoCount;
                byte[] checkpoint=SHA256.HashData(File.ReadAllBytes(test.RecoveryPath));
                string Read(bool optimized)
                {
                    test.UseOptimizedReading=optimized;
                    var issues=test.Check();var targets=test.LineTargets.ToArray();var shapes=test.LineShapes.ToArray();
                    var lines=OfficeComponentCheck.ReadLines(test);
                    if(story!=test.ReadCheckText() || test.Changed!=changed || test.CheckUndoCount!=undo ||
                        !checkpoint.SequenceEqual(SHA256.HashData(File.ReadAllBytes(test.RecoveryPath))))
                        throw new InvalidOperationException("Đọc/quét so sánh đã thay đổi tài liệu hoặc trạng thái hoàn tác.");
                    return JsonSerializer.Serialize(new {Issues=issues,Targets=targets,Shapes=shapes,MeasuredLineIssues=lines});
                }
                string legacy=Read(false),optimized=Read(true);
                if(legacy!=optimized)throw new InvalidOperationException("Hai cách đọc khác kết quả kiểm tra, quyền sửa hoặc số đo nét XPS: "+label);
                pass("Đọc cũ/mới: cùng danh sách lỗi, payload, mốc bảng và số đo XPS; giữ chữ, checkpoint, Changed/Undo — "+label);
            }
            foreach(bool bad in new[]{false,true})
            {
                string source=OfficeComponentCheck.Create(modern,isolated,bad,true);
                byte[] sourceHash=SHA256.HashData(File.ReadAllBytes(source));test.Open(source);
                Compare(bad?"thành phần sai, trích yếu có ngắt dòng":"thành phần đúng trong bảng, trích yếu nhiều đoạn và nơi nhận");
                if(!bad)
                {
                    test.UseOptimizedReading=true;
                    var lines=OfficeComponentCheck.ReadLines(test);
                    if(lines.Length!=3)throw new InvalidOperationException("Mẫu so sánh chưa nhận đủ ba Line để kiểm định dạng sau chèn.");
                    test.FixSelection(lines);Compare("ba Line đã chèn cùng lượt");
                    test.Undo();Compare("sau một Undo");
                }
                if(!sourceHash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))
                    throw new InvalidOperationException("Mẫu nguồn so sánh cách đọc đã thay đổi.");
            }
            test.UseOptimizedReading=true;test.Open(modern);object? owned=test.CheckApplicationIdentity;
            string before=test.ReadCheckText();test.Open(modern);
            if(owned is null || !ReferenceEquals(owned,test.CheckApplicationIdentity) || before!=test.ReadCheckText() || test.Changed || test.CheckUndoCount!=0)
                throw new InvalidOperationException("Mở lại mẫu chưa dùng đúng phiên Word riêng hoặc thay đổi dữ liệu/trạng thái.");
            pass("Mở liên tiếp: giữ đúng phiên Word riêng, nguồn/chữ/Changed/Undo nguyên vẹn; không dùng Word đang mở của người dùng");
        }
        finally{test.UseOptimizedReading=previous;}
    }
}
