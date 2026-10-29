namespace ChinhTa.Core;

public enum SpacingDiagnosticFailureCode
{
    ReferenceMissing, ReferenceStale, UnsupportedEvidence, OperationFailed,
    IntegrityFailed, CleanupFailed, SourceChanged
}

// Safe, actionable failure metadata. Never copy document XML, selected text or
// an arbitrary exception message into the user-facing measurement report.
public sealed class SpacingDiagnosticException:InvalidOperationException
{
    public SpacingDiagnosticFailureCode Code {get;}
    public string? RecoveryPath {get;}
    public SpacingDiagnosticException(SpacingDiagnosticFailureCode code,Exception? innerException=null,string? recoveryPath=null)
        :base(SafeMessage(code),innerException)
    {
        Code=Enum.IsDefined(code)?code:SpacingDiagnosticFailureCode.OperationFailed;
        RecoveryPath=SafeRecoveryPath(recoveryPath);
    }
    private static string SafeMessage(SpacingDiagnosticFailureCode code)=>code switch
    {
        SpacingDiagnosticFailureCode.ReferenceMissing=>"Chưa có bản xem trước hiện tại để đo. Hãy mở và quét tài liệu trước.",
        SpacingDiagnosticFailureCode.ReferenceStale=>"Tài liệu đã đổi sau bản xem trước. Hãy quét lại trước khi đo tiếp.",
        SpacingDiagnosticFailureCode.UnsupportedEvidence=>"Đoạn hoặc bản thử chưa đủ bằng chứng trong phạm vi đo được hỗ trợ. Giữ kiểm tra thủ công hoặc chọn đoạn khác.",
        SpacingDiagnosticFailureCode.IntegrityFailed=>"Bản thử không giữ được toàn vẹn dữ liệu đã kiểm. Không dùng bản thử hoặc kết quả của lượt đo này.",
        SpacingDiagnosticFailureCode.CleanupFailed=>"Chưa xác nhận đóng và khôi phục đầy đủ phiên Word đo thử. Kết quả lượt này không được dùng; cần kiểm tra phiên Word riêng và bản phục hồi còn giữ.",
        SpacingDiagnosticFailureCode.SourceChanged=>"Nguồn Word đã thay đổi trong lúc đo. Kết quả lượt này bị hủy; hãy kiểm tra tài liệu hiện tại rồi quét lại.",
        _=>"Phiên đo Word chưa hoàn tất do lỗi thao tác. Kiểm tra Word rồi thử lại; quét lại không thay thế việc xử lý lỗi phiên đo."
    };
    private static string? SafeRecoveryPath(string? path)
    {
        if(string.IsNullOrWhiteSpace(path) || path.Length>4096 || path.Any(char.IsControl))return null;
        try
        {
            if(!Path.IsPathFullyQualified(path))return null;
            string full=Path.GetFullPath(path),name=Path.GetFileName(full);
            const string prefix="ChinhTa-Isolated-";
            var comparison=OperatingSystem.IsWindows()?StringComparison.OrdinalIgnoreCase:StringComparison.Ordinal;
            string temporary=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            if(!name.StartsWith(prefix,StringComparison.Ordinal) || !Guid.TryParseExact(name[prefix.Length..],"N",out _) ||
                !string.Equals(Path.GetDirectoryName(full)?.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar),temporary,comparison))return null;
            return full;
        }
        catch(Exception error) when(error is ArgumentException or IOException or NotSupportedException or System.Security.SecurityException)
        {return null;}
    }
}
