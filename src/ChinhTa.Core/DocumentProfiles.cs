namespace ChinhTa.Core;

public enum DocumentProfile { State, Party, Personal }

public static class DocumentProfiles
{
    public static string Description(DocumentProfile profile) => profile switch
    {
        DocumentProfile.State => "Nhà nước — NĐ 30/2020/NĐ-CP, Phụ lục I: quốc hiệu, tiêu ngữ được nhận diện trên trang đầu; khổ giấy/lề; ba Shape Line khi nhận diện/đo được. " +
            "Kiểm mẫu số/ký hiệu, địa danh/ngày tháng ở phần đầu nhận diện được; giữ trường trống. " +
            "Chưa xác minh mã được cấp, địa danh chính thức, toàn bộ nội dung, chữ ký, số trang và mẫu riêng.",
        DocumentProfile.Party => "Đảng — 05-HD/VPTW ngày 27/5/2026: tiêu đề Đảng trang đầu, khổ giấy/lề và cảnh báo ngoại lệ. " +
            "Chưa kiểm đầy đủ các thành phần, thân bài, số trang, đường kẻ và mẫu riêng.",
        _ => "Cá nhân: kiểm tra ngôn ngữ minh họa, khoảng trắng; gợi ý kiểm tra tab liên tiếp/đoạn căn giữa. " +
            "Không áp quốc hiệu, tiêu ngữ, khổ giấy hoặc lề theo quy định Nhà nước/Đảng."
    };
}

