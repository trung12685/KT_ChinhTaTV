namespace ChinhTa.Core;

public static class BatchPlan
{
    public static IReadOnlyList<Issue> Create(IEnumerable<Issue> issues)
    {
        var ordered = issues.Distinct().OrderBy(x => x.Start).ToArray();
        var active=new List<Issue>();
        for (int i = 0; i < ordered.Length; i++)
        {
            var current = ordered[i];
            if (!current.CanFix)
                throw new InvalidOperationException("Có cảnh báo chỉ kiểm tra thủ công, không thể sửa tự động.");
            if (current.Start < 0 || current.Length <= 0 || current.Original.Length != current.Length)
                throw new InvalidOperationException("Vị trí lỗi không hợp lệ. Hãy quét lại.");
            if(current.SpacingFix is { } spacing && (!spacing.Valid ||
                current.Kind!=(spacing.IsGap?FixKind.BodyGap:FixKind.BodyLineSpacing) || current.Original!=current.Replacement ||
                current.CharacterFormat is not null || current.ParagraphFix is not null || current.Format is not null || current.CenterFix is not null || current.LineFix is not null || current.Original.Any(char.IsControl) ||
                spacing.Next is { } next && next.Start<(long)current.Start+current.Length) ||
                current.Kind is FixKind.BodyGap or FixKind.BodyLineSpacing && current.SpacingFix is null)
                throw new InvalidOperationException("Gợi ý khoảng đoạn/giãn dòng chưa hợp lệ; hãy quét lại.");
            if(current.ParagraphFix is { } paragraph && (current.Kind!=FixKind.BodyParagraph || !paragraph.Valid ||
                current.Original!=current.Replacement || current.CharacterFormat is not null || current.Format is not null ||
                current.CenterFix is not null || current.LineFix is not null || current.Original.Any(char.IsControl)) ||
                current.Kind==FixKind.BodyParagraph && current.ParagraphFix is null)
                throw new InvalidOperationException("Gợi ý căn/thụt đầu dòng chưa hợp lệ; hãy quét lại.");
            if(current.OutlineContext is not null && current.Kind!=FixKind.OutlineOrder)
                throw new InvalidOperationException("Ngữ cảnh mục không đúng loại sửa. Hãy quét lại.");
            if(current.Format?.RepairInvalidSizes==true && current.Format.MaximumSize is null)
                throw new InvalidOperationException("Gợi ý sửa riêng cỡ tiêu đề thiếu khoảng cỡ hợp lệ. Hãy quét lại.");
            if(current.Format?.MaximumSize is float maximum && (current.Original!=current.Replacement ||
                !(current.Kind==FixKind.NationalHeading && current.Format.Size==12 && maximum==13 ||
                  current.Kind==FixKind.StateMotto && current.Format.Size==13 && maximum==14)))
                throw new InvalidOperationException("Gợi ý giữ cỡ tiêu đề chưa đúng vai trò hoặc đang thay chữ. Hãy quét lại.");
            if(current.Kind==FixKind.DocumentTypeText && (!ShapeLineRules.IsDocumentType(current.Original) ||
                current.Original!=current.Original.Trim() || current.Original.Any(char.IsControl) ||
                current.Replacement!=current.Original.ToUpperInvariant() || current.Original==current.Replacement ||
                current.Original.Length!=current.Replacement.Length || current.CharacterFormat is not null ||
                current.Format is not null || current.LineFix is not null || current.CenterFix is not null))
                throw new InvalidOperationException("Gợi ý viết hoa tên loại chưa hợp lệ. Hãy quét lại.");
            if(current.Kind is FixKind.ParagraphEndSpace or FixKind.QuoteSpace or FixKind.ParenthesisSpace &&
                (!(current.Replacement.Length==0 && current.Original.All(c=>c==' ') || current.AddsSpaceAfterClosingQuote || current.AddsSpaceBeforeOpeningQuote || current.AddsSpaceBeforeOpeningParenthesis || current.AddsSpaceAfterClosingParenthesis) ||
                current.CharacterFormat is not null || current.Format is not null || current.LineFix is not null || current.CenterFix is not null))
                throw new InvalidOperationException("Gợi ý xóa khoảng trắng chưa hợp lệ. Hãy quét lại.");
            if(current.CharacterFormat is CharacterFormat style && (!style.Valid ||
                !(current.Kind==FixKind.BodyFormat && style.Role==ComponentRole.Body && (style.PreserveEmphasis?style.Bold is null:style.Bold==false) && !style.Italic ||
                  current.Kind==FixKind.AdministrativeFormat && style.Bold is null && style.Role is null ||
                  current.Kind==FixKind.DocumentHeadingFormat && style.Bold==true && !style.Italic && style.Role is null ||
                  current.Kind==FixKind.AgencyFormat && style.Role is ComponentRole.Agency or ComponentRole.AgencyParent ||
                  current.Kind==FixKind.RecipientFormat && style.Role is ComponentRole.RecipientItem or ComponentRole.RecipientLabel) ||
                current.Original!=current.Replacement || current.Format is not null || current.LineFix is not null ||
                current.Original.IndexOfAny(['\r','\n','\a','\t'])>=0))
                throw new InvalidOperationException("Gợi ý định dạng ký tự không hợp lệ. Hãy quét lại.");
            if(current.CenterFix is not null && (current.Kind!=FixKind.AdministrativePosition ||
                !Enum.IsDefined(current.CenterFix.Role) || current.Original!=current.Replacement ||
                current.Original!=current.Original.Trim() || current.Original.IndexOfAny(['\r','\n','\a','\t'])>=0 ||
                current.CharacterFormat is not null || current.Format is not null || current.LineFix is not null) ||
                current.Kind==FixKind.AdministrativePosition && current.CenterFix is null)
                throw new InvalidOperationException("Gợi ý căn giữa không hợp lệ. Hãy quét lại.");
            if(current.Kind is FixKind.DocumentHeadingFormat or FixKind.AgencyFormat or FixKind.RecipientFormat or FixKind.BodyFormat && current.CharacterFormat is null)
                throw new InvalidOperationException("Gợi ý tên loại/trích yếu thiếu định dạng đã xác minh. Hãy quét lại.");
            active.RemoveAll(previous=>(long)previous.Start+previous.Length<=current.Start);
            foreach(var previous in active)
                if(!SharesFormat(previous,current) && !SharesFormat(current,previous) && !BodyStylePatchPlan.Disjoint(previous,current))
                    throw new InvalidOperationException("Các lỗi chồng lấn vị trí. Hãy sửa riêng từng lỗi rồi quét lại.");
            active.Add(current);
        }
        // Character-only formatting does not change CPs. Apply it before descending text edits.
        return ordered.OrderByDescending(i=>i.IsStyleOnly).ThenByDescending(i=>i.Start).ToArray();
    }
    private static bool SharesFormat(Issue format,Issue text)=>format.IsStyleOnly &&
        !text.IsStyleOnly && text.Format is null && text.LineFix is null &&
        text.Kind is FixKind.QuoteSpace or FixKind.Text or FixKind.AdministrativeNumber or FixKind.AdministrativeDate or FixKind.RepeatedText or FixKind.PunctuationSpace or FixKind.ParenthesisSpace or FixKind.SentenceEndSpace or FixKind.ParagraphEndSpace or FixKind.ColonSpace or FixKind.SentenceInnerSpace or FixKind.SentenceAfterSpace or FixKind.OutlineOrder or FixKind.OutlineMarker or FixKind.AgencyText or FixKind.RecipientText or FixKind.DocumentTypeText &&
        (text.Kind!=FixKind.DocumentTypeText || format.Kind==FixKind.DocumentHeadingFormat) &&
        text.Start>=format.Start && (long)text.Start+text.Length<=(long)format.Start+format.Length;
    public static void Validate(IReadOnlyList<Issue> plan, Func<Issue, string> read)
    {
        foreach (var issue in plan)
            if (read(issue) != issue.Original)
                throw new InvalidOperationException("Nội dung đã thay đổi; chưa áp dụng lượt sửa này. Hãy quét lại.");
    }
}

