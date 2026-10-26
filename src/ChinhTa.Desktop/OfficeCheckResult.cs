namespace ChinhTa.Desktop;

public record OfficeCheckResult(bool Passed,string Stage,string Folder,bool ExperimentalReading=false)
{
    public string ReaderScope=>ExperimentalReading?"Chế độ đã chọn: bộ đọc thử nghiệm.":"Chế độ: bộ đọc cũ. Chưa chạy bộ đọc thử nghiệm/so sánh hai cách đọc.";
}
