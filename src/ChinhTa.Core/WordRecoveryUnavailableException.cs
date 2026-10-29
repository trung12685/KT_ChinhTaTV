namespace ChinhTa.Core;

// The owned document reference may still exist after an RPC disconnect. Its
// presence alone cannot promise that retrying a scan can reopen this checkpoint.
public sealed class WordRecoveryUnavailableException:InvalidOperationException
{
    public WordRecoveryUnavailableException(string recoveryPath,Exception cause):base(
        "Word mất kết nối trước khi lưu được thay đổi cuối. " +
        "Không tự mở lại để tránh bỏ thay đổi âm thầm. Bản lưu gần nhất: " + recoveryPath,cause){}
}
