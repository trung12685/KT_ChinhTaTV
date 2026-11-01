"""Render measured counts without calling a development audit 'accuracy'."""
from pathlib import Path
import argparse,json

def render(path):
    d=json.loads(path.read_text(encoding='utf-8-sig'))
    lines=['# Báo cáo chất lượng chính tả — kiểm toán phát triển','',
        'Trạng thái phát hành: **CHƯA ĐỦ ĐIỀU KIỆN**. Không tạo bộ cài từ quy trình này.','',
        'Cùng dữ liệu và toàn bộ luồng worker: bản so sánh tái hiện 0.1.91; nguyên mẫu bật phục hồi dấu tổng quát và chính sách bảo vệ chuỗi tên/tra cứu từ vựng rộng hơn. Không huấn luyện lại dữ liệu trong lần đo.','',
        '| Tập / nhóm | Bản so sánh: phát hiện/đích | Nguyên mẫu: phát hiện/đích | Gợi ý đầu đúng đích: so sánh → nguyên mẫu |',
        '|---|---:|---:|---:|']
    old=d['Reports']['baseline_0_1_91_compatible'];new=d['Reports']['candidate']
    for a,b in zip(old['Groups'],new['Groups']):
        if a['Targets']==0:continue
        lines.append(f"| {a['Kind']} / {a['Group']} | {a['Hits']}/{a['Targets']} | {b['Hits']}/{b['Targets']} | {round((a['Top1TargetCorrectionRate'] or 0)*100,1)}% → {round((b['Top1TargetCorrectionRate'] or 0)*100,1)}% |")
    lines+=['',f"Mức cảnh báo đỏ trên 659 câu tin tức chưa gán nhãn: {old['AuditWarningRatePer1000Tokens']:.2f} → {new['AuditWarningRatePer1000Tokens']:.2f} cảnh báo/1.000 token. Đây **không phải tỷ lệ báo nhầm**; các cảnh báo chưa được thẩm định.",'',
        '61 câu do dự án biên soạn là kiểm thử phát triển, không phải tập đánh giá độc lập. 659 lỗi bỏ dấu là nhiễu tạo có chủ đích; sửa lại bản gốc không chứng minh câu nhiễu luôn sai trong ngữ cảnh.','',
        'Chưa có bộ lỗi thực/văn bản đúng được hai người rà soát độc lập. Chưa đo vị trí gạch và độ trễ trên Word thật. CI xanh chỉ xác nhận quy trình chạy đúng và tái lập, không chứng nhận bộ kiểm đạt chất lượng sử dụng rộng rãi.','']
    return '\n'.join(lines)

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('report',type=Path);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
    text=render(a.report);a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(text,encoding='utf-8',newline='\n');print(text)
