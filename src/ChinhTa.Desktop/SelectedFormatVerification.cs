using ChinhTa.Core;

namespace ChinhTa.Desktop;

internal sealed record SelectedFormatProof(Issue Issue,string Text,string Prefix,FontSizeEvidence? Sizes,CharacterStyleEvidence? Styles=null);

// Rebind after save/reopen through actual text and prefix, then read Word formats.
internal static class SelectedFormatVerification
{
    public static void Verify(IReadOnlyList<SelectedFormatProof> formats,IReadOnlyList<WordParagraphSnapshot> current,
        Func<Issue,string> readPrefix,Func<int,int,object> readRange,Action<object?> release)
    {
        foreach(var proof in formats)
        {
            var matches=current.Where(p=>p.Text.TrimEnd('\r','\a','\n')==proof.Text &&
                readPrefix(proof.Issue with {Start=p.Start})==proof.Prefix).ToArray();
            if(matches.Length!=1)throw new InvalidOperationException("Chưa đối chiếu lại được vùng phông sau các nhóm sửa.");
            dynamic range=readRange(matches[0].Start,matches[0].Start+proof.Text.Length);
            dynamic? paragraph=null;
            Exception? primary=null;
            try
            {
                int? alignment=null;
                if(proof.Issue.Format?.Alignment is not null){paragraph=range.ParagraphFormat;alignment=(int)paragraph.Alignment;}
                bool correct=proof.Styles is not null || VisibleFormatReader.Matches((object)range,proof.Text,proof.Issue.CharacterFormat,proof.Issue.Format,alignment);
                if((string)range.Text!=proof.Text || !correct)
                    throw new InvalidOperationException("Phông sau các nhóm sửa không còn khớp đề xuất; phải khôi phục cả lượt.");
                if(proof.Styles is not null && !proof.Styles.Matches(VisibleFormatReader.ReadStyles((object)range,proof.Text,includeSpacing:proof.Styles.Paragraph?.Spacing is not null)))
                    throw new InvalidOperationException("Định dạng ký tự sau các nhóm sửa chưa khớp phần cần giữ/phần đã duyệt; phải khôi phục cả lượt.");
                if(proof.Sizes is not null && !proof.Sizes.Matches(VisibleFormatReader.ReadSizes((object)range,proof.Text)))
                    throw new InvalidOperationException("Cỡ chữ đã đổi sau các nhóm sửa; phải khôi phục cả lượt.");
            }
            catch(Exception failure){primary=failure;}
            finally
            {
                var failure=IndependentCleanup.Complete(primary,
                    [()=>release((object?)paragraph),()=>release((object)range)]);
                if(failure is not null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }
    }
}
