"""Aggregate-only report: never embed external source sentences or annotations."""
from pathlib import Path
import argparse,json,sys

def render(path):
    d=json.loads(path.read_text(encoding='utf-8-sig'));m=d['Input'];c=m['Counts']
    old=d['Reports']['baseline_0_1_91_compatible'];new=d['Reports']['candidate'];a=old['SourceLabelAgreement'];b=new['SourceLabelAgreement']
    percent=lambda n,total:f'{100*n/total:.2f}%' if total else '—'
    lines=['# Đo bộ kiểm trên lỗi thực VSEC — tập phát triển','',
        '**CHƯA ĐỦ ĐIỀU KIỆN PHÁT HÀNH.** Đây là đo nghiên cứu trên nhãn của nguồn ngoài, chưa được dự án rà soát độc lập hai lượt. Nguyên mẫu vẫn tắt trong ứng dụng.','',
        f"Nguồn: [VSEC2021/VSEC](https://github.com/VSEC2021/VSEC/tree/{m['Commit']}); SHA-256 nguồn: {m['SourceSHA256']}.",
        f"Đọc {c['SourceRows']:,} câu; loại {c.get('Rejected_original_is_an_accepted_answer',0)} câu có đáp án chấp nhận chính chữ gốc và {c.get('Rejected_unaligned_text',0)} câu không ghép được vị trí chính xác. Loại trùng huấn luyện: {c.get('ExcludedTrainingOverlap',0)}; trùng kiểm toán lịch sử: {c.get('ExcludedPriorAuditOverlap',0)}.",
        f"Đã đo {c['DevelopmentRows']:,} câu / {c['DevelopmentTargets']:,} đích lỗi. Giữ lại {c['ReservedRows']:,} câu / {c['ReservedTargets']:,} đích, chưa xuất và chưa chấm. Các câu gốc/câu sửa tương đương được gom cùng nhóm trước chia tập.",'',
        '| Chỉ số theo nhãn nguồn | Bản 0.1.91 tương thích | Nguyên mẫu |','|---|---:|---:|',
        f"| Đích có cảnh báo đỏ | {a['Hits']}/{a['Targets']} ({percent(a['Hits'],a['Targets'])}) | {b['Hits']}/{b['Targets']} ({percent(b['Hits'],b['Targets'])}) |",
        f"| Cảnh báo đỏ phủ đích lỗi / toàn bộ cảnh báo đỏ | {a['MatchedWarnings']}/{a['Warnings']} ({percent(a['MatchedWarnings'],a['Warnings'])}) | {b['MatchedWarnings']}/{b['Warnings']} ({percent(b['MatchedWarnings'],b['Warnings'])}) |",
        f"| Cảnh báo trên phần được nguồn gán đúng | {a['WarningsOnSourceLabeledCorrectSpans']} | {b['WarningsOnSourceLabeledCorrectSpans']} |",
        f"| Cảnh báo / 1.000 token được nguồn gán đúng | {a['WarningsPer1000SourceLabeledCorrectTokens']:.2f} | {b['WarningsPer1000SourceLabeledCorrectTokens']:.2f} |",'',
        '| Nhóm lỗi | Đích | Bản cũ: phát hiện | Nguyên mẫu: phát hiện | Nguyên mẫu: gợi ý đầu / ba gợi ý đúng |','|---|---:|---:|---:|---:|']
    old_groups={g['Group']:g for g in old['ErrorGroups']}
    for g in new['ErrorGroups']:
        before=old_groups[g['Group']];lines.append(f"| {g['Group']} | {g['Targets']} | {before['Hits']} | {g['Hits']} | {g['Top1']} / {g['Top3']} |")
    lines+=['','| Mục gốc có trong từ vựng của bộ kiểm | Đích | Bản cũ: phát hiện | Nguyên mẫu: phát hiện |','|---|---:|---:|---:|']
    old_words={g['OriginalInLexicon']:g for g in old['SourceWordGroups']}
    for g in new['SourceWordGroups']:
        lines.append(f"| {'Có' if g['OriginalInLexicon'] else 'Không'} | {g['Targets']} | {old_words[g['OriginalInLexicon']]['Hits']} | {g['Hits']} |")
    lines+=['',
        '“Phủ đích lỗi” được tính theo giao nhau giữa vùng cảnh báo và vùng lỗi gán nhãn. Gợi ý đúng phải khớp một đáp án được chấp nhận, kể cả đáp án thứ hai hoặc xóa từ; một gợi ý phủ nhiều đích phải sửa đúng mọi đích bên trong. Kiểm vị trí bằng chuỗi UTF-16 gốc.','',
        'Bộ dữ liệu chỉ gồm câu có lỗi. Các token được gán đúng nằm trong những câu đó; chúng không thay thế một kho văn bản hoàn toàn đúng được thẩm định. Tỷ lệ khớp nhãn nguồn không phải chứng nhận độ chính xác thực tế hoặc kết quả đạt cửa kiểm 98%. Không có ranh giới tài liệu gốc để chứng minh độc lập theo tài liệu.','',
        'Nguyên mẫu tăng phát hiện nhưng tăng cảnh báo trên phần được gán đúng. Cần giảm báo nhầm, cải thiện từ hợp lệ dùng sai/dấu đã gõ sai và xếp hạng đáp án trước khi bật cho người dùng. Nhóm chữ HOA/viết hoa chưa có đích riêng trong nguồn này; vẫn cần bộ đối chứng khác.','',
        'Dữ liệu/từ điển ứng dụng không được huấn luyện hoặc bổ sung từ nguồn này. Kho gốc chưa nêu giấy phép; không đưa câu nguồn, nhãn gốc, tập dự trữ hoặc tệp cases vào GitHub artifact/bộ cài. Báo cáo chỉ chứa số tổng hợp, hash và nguồn. Word thật chưa được đo trong bài này.','']
    return '\n'.join(lines)

if __name__=='__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    p=argparse.ArgumentParser();p.add_argument('report',type=Path);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
    text=render(a.report);a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(text,encoding='utf-8',newline='\n');print(text)
