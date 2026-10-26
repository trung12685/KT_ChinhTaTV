namespace ChinhTa.Desktop;

// The ordinary Office validation follows the mode selected by the user. The
// experimental comparison remains strict, explicit, and cannot earn a PASS by
// being skipped. Functional XPS/edit/rollback checks run in either mode.
internal static class OfficeReaderCheckPolicy
{
    public const string NotRun="NOT_RUN Bộ đọc thử nghiệm/so sánh hai cách đọc: đang tắt; kết quả chỉ áp dụng bộ đọc cũ.";
    public static void CompareIfSelected(bool experimentalSelected,Action compare,Action<string> note)
    {
        if(experimentalSelected)compare();else note(NotRun);
    }
}
