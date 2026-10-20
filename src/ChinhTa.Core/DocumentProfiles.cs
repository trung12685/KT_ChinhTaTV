namespace ChinhTa.Core;

public enum DocumentProfile { State, Party, Personal }

public static class DocumentProfiles
{
    public static string Description(DocumentProfile profile) => profile switch
    {
        DocumentProfile.State => "Văn bản Nhà nước: đang chờ đối chiếu bản gốc Nghị định 30/2020/NĐ-CP. " +
            "Hiện chỉ có kiểm tra ngôn ngữ và mẫu quốc hiệu thử nghiệm; chưa kiểm tra đầy đủ thể thức.",
        DocumentProfile.Party => "Văn bản Đảng: đang chờ xác minh Hướng dẫn 05-HD/VPTW ngày 27/5/2026. " +
            "Hiện chỉ kiểm tra ngôn ngữ, không áp quy tắc quốc hiệu của văn bản Nhà nước.",
        _ => "Văn bản cá nhân: kiểm tra ngôn ngữ và khoảng trắng cơ bản; không bắt buộc quốc hiệu, tiêu ngữ hoặc bố cục hành chính. " +
            "Chưa có quy tắc tab/căn giữa tự động."
    };
}
