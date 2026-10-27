namespace ChinhTa.Core;

public static class SignatureRules
{
    public const string Citation="NĐ30, PL I, Phần I.I.4 và II.7.đ, ô 7a–7b (PDF 21, 26–27): Times New Roman, đen, cỡ 13–14, đứng, đậm.";
    public static IReadOnlyList<Paragraph> ObservationTargets(IReadOnlyList<Paragraph> paragraphs,DocumentProfile profile)
    {
        if(profile!=DocumentProfile.State || paragraphs.Select(p=>p.Start).Distinct().Count()!=paragraphs.Count)return [];
        // Signature scope comes from the terminal-table XML proof, matched to
        // current Word ranges. Native InTable is only observed in the bounded
        // front-page scan; its false placeholder after ordinal40 cannot veto
        // this independently proven role. Do not infer table/page geometry.
        var rows=paragraphs.Where(p=>p.Outline is {Scope:OutlineScope.Signature,SignatureRole:ComponentRole.SignatureAuthority or ComponentRole.SignatureName}).ToArray();
        // All semantic anchors must also have current live text/format proof. A
        // prefix that mapped only the title must not grant writes on its own.
        return rows.Count(p=>p.Outline!.SignatureRole==ComponentRole.SignatureName)==1 &&
            rows.Count(p=>p.Outline!.SignatureRole==ComponentRole.SignatureAuthority) is >=1 and <=3 &&
            rows.All(p=>p.TextEditable || p.FormatEditable)?rows:[];
    }
    public static IReadOnlyList<Issue> Check(IReadOnlyList<Paragraph> paragraphs,DocumentProfile profile)
    {
        var result=new List<Issue>();
        foreach(var row in ObservationTargets(paragraphs,profile))
        {
            var assessment=new FormatPolicy(13,14,false,true).Assess(row,allowSelectiveSizeRepair:true);
            // Unknown observation is not evidence of a formatting defect.
            if(assessment.State!=FormatState.Repairable)continue;
            string text=row.Text.TrimEnd('\r','\a','\n');
            var role=row.Outline!.SignatureRole!.Value;
            result.Add(new(row.Start,text.Length,text,text,
                "Khối ký đã nhận diện: chỉ sửa phông/màu/cỡ/đứng/đậm; giữ nguyên tên, cấp bậc, chữ hoa/thường, căn đoạn, ảnh chữ ký và con dấu.",
                false,FixKind.SignatureFormat,Citation:Citation,
                Title:role==ComponentRole.SignatureAuthority?"Định dạng quyền hạn/chức vụ chưa đúng":"Định dạng họ tên người ký chưa đúng",
                CharacterFormat:new(assessment.Size,false,true,role,assessment.PreserveSize,assessment.RepairInvalidSizes)));
        }
        return result;
    }
}
