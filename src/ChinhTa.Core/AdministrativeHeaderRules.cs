using System.Text.RegularExpressions;

namespace ChinhTa.Core;

// Narrow text checks for the identified first-page State header. Never infer assigned identifiers/dates.
public static class AdministrativeHeaderRules
{
    public const string TypeMismatchTitle="Tên loại không khớp ký hiệu";
    private const RegexOptions Options=RegexOptions.IgnoreCase|RegexOptions.CultureInvariant;
    private const RegexOptions Linear=Options|RegexOptions.NonBacktracking;
    private static readonly Regex NumberHint=new(@"^[ \t]*Số(?:[ \t]*:|[ \t]*(?=[0-9/._…]))",Options);
    private static readonly Regex NumberPrefix=new(@"^[ \t]*(?<label>Số)(?<before>[ \t]*)(?<colon>:?)(?<gap>[ \t]*)(?<value>.*)$",Linear);
    private static readonly Regex Identifier=new(@"^(?<number>[0-9]+|[._…]*)(?<slash>[ \t]*/[ \t]*)(?<code>[\p{L}\p{M}\p{N}]+(?:\.[\p{L}\p{M}\p{N}]+)*(?:[ \t]*[-–—][ \t]*[\p{L}\p{M}\p{N}]+(?:\.[\p{L}\p{M}\p{N}]+)*)+)$");
    private static readonly Regex DateHint=new(@"^[ \t]*(?:[\p{L}\p{M}\p{N}][\p{L}\p{M}\p{N} .’'\-]*?(?:[ \t]+|,[ \t]*))?ngày[^\r\a]*tháng[^\r\a]*năm",Linear);
    private static readonly Regex DateRow=new(@"^[ \t]*(?<place>[\p{L}\p{M}\p{N}][\p{L}\p{M}\p{N} .’'\-]*?)(?<separator>[ \t]*,[ \t]*|[ \t]+)(?<dayLabel>ngày)(?<day>[0-9._… \t]*)(?<monthLabel>tháng)(?<month>[0-9._… \t]*)(?<yearLabel>năm)(?<year>[0-9._… \t]*)$",Linear);
    private static readonly Regex StandaloneDate=new(@"^[ \t]*ngày(?<day>[0-9._… \t]*)tháng(?<month>[0-9._… \t]*)năm(?<year>[0-9._… \t]*)$",Linear);
    // Read blank zones separately from the strict edit parser. A trailing annotation must
    // not turn an intentional blank into a generic spacing edit; it remains manual.
    private static readonly Regex DateBlankZones=new(@"^[ \t]*(?:[\p{L}\p{M}\p{N}][\p{L}\p{M}\p{N} .’'\-]*?(?:[ \t]*,[ \t]*|[ \t]+))?ngày(?<day>[0-9._… \t]*)tháng(?<month>[0-9._… \t]*)năm(?<year>[0-9._… \t]*)",Linear);
    private static readonly Regex Hyphen=new(@"[ \t]*[-–—][ \t]*");
    private static string Clean(string text)=>text.TrimEnd('\r','\a','\n');
    private static bool Blank(string value)=>value.Length==0 || value.All(c=>c is '.' or '_' or '…');
    private static string Citation(bool date)=>date
        ? "NĐ 30/2020/NĐ-CP, Phụ lục I, Phần I.II.4.b–c; PDF trang 23. Chưa xác minh địa danh chính thức hoặc ngoại lệ riêng của đơn vị."
        : "NĐ 30/2020/NĐ-CP, Phụ lục I, Phần I.II.3.a–c; PDF trang 22–23. Chữ viết tắt tên loại theo PL III.I; mã cơ quan do cơ quan quy định, chưa xác minh mã được cấp.";

    public static bool IsCandidate(string text)=>NumberHint.IsMatch(Clean(text)) || DateHint.IsMatch(Clean(text));
    public static bool IsNumber(string text)=>NumberHint.IsMatch(Clean(text));
    public static IReadOnlyList<Paragraph> Identify(IReadOnlyList<Paragraph> paragraphs,DocumentProfile profile)
    {
        if(profile!=DocumentProfile.State)return [];
        var first=paragraphs.Take(40).Where(p=>p.Page==1).ToArray();
        var headings=first.Select((p,i)=>(p,i)).Where(x=>HeadingRules.IsCandidate(x.p.Text,profile)).ToArray();
        if(headings.Length!=2 || headings.Any(x=>x.i>=16) ||
            headings.Count(x=>Clean(x.p.Text).TrimStart().StartsWith("Độc",StringComparison.OrdinalIgnoreCase))!=1)return [];
        int last=headings.Max(x=>x.i),end=first.Length;
        for(int i=0;i<first.Length;i++)
        {
            string text=Clean(first[i].Text);
            if(ShapeLineRules.IsDocumentType(text) || i>last && !first[i].InTable &&
                !string.IsNullOrWhiteSpace(text) && !IsCandidate(text)) {end=i;break;}
        }
        return first.Take(end).Where(p=>IsCandidate(p.Text)).ToArray();
    }
    public static IReadOnlyList<Issue> Check(IReadOnlyList<Paragraph> paragraphs,DocumentProfile profile)
    {
        var header=Identify(paragraphs,profile);
        var front=paragraphs.Take(40).Where(p=>p.Page==1).ToArray();
        var titles=front.Select((p,i)=>(p,i)).Where(x=>!x.p.InTable && DocumentTypeCatalog.FindHeading(x.p.Text) is not null).ToArray();
        int lastHeading=Array.FindLastIndex(front,p=>HeadingRules.IsCandidate(p.Text,profile));
        var namedType=titles.Length==1 && lastHeading>=0 && titles[0].i>lastHeading && titles[0].p.TextEditable &&
            !front.Skip(lastHeading+1).Take(titles[0].i-lastHeading-1).Any(p=>!p.InTable &&
                !string.IsNullOrWhiteSpace(Clean(p.Text)) && !IsCandidate(p.Text))
                ?DocumentTypeCatalog.FindHeading(titles[0].p.Text):null;
        var result=new List<Issue>();
        foreach(bool date in new[]{false,true})
        {
            var rows=header.Where(p=>(date?DateHint:NumberHint).IsMatch(Clean(p.Text))).ToArray();
            if(rows.Length>1)
            {
                foreach(var row in rows)result.Add(Notice(row,date,"Có nhiều dòng ứng viên trong phần đầu; cần xác định đúng dòng trước khi sửa."));
                continue;
            }
            if(rows.Length!=1)continue;
            var p=rows[0];
            if(!p.TextEditable || p.Text.Contains('\t'))
            {
                result.Add(Notice(p,date,"Chưa xác minh được dòng chữ thuần để sửa an toàn (có thể có trường tự động, control, liên kết hoặc tab bố cục). Chỉnh thủ công; giữ nguyên nội dung và cấu trúc."));
                continue;
            }
            bool known=date?CheckDate(p,result):CheckNumber(p,result,namedType);
            if(p.FormatEvidence is not null)
            {
                var assessment=new FormatPolicy(13,date?14:13,date,null).Assess(p,allowSelectiveSizeRepair:true);
                if(known && assessment.State==FormatState.Repairable)
                {
                    string text=Clean(p.Text);
                    result.Add(new(p.Start,text.Length,text,text,"Times New Roman, màu đen, đúng cỡ/kiểu của dòng; giữ cỡ hợp lệ, đậm/gạch chân, ô trống và căn đoạn.",false,
                        FixKind.AdministrativeFormat,Citation:Citation(date),Title:date?"Định dạng địa danh/ngày tháng":"Định dạng số/ký hiệu",
                        CharacterFormat:new(assessment.Size,date,PreserveSize:assessment.PreserveSize,RepairInvalidSizes:assessment.RepairInvalidSizes)));
                }
                else if(assessment.State is FormatState.Unknown or FormatState.NeedsReview || !known && assessment.State==FormatState.Repairable)
                    result.Add(Notice(p,date,"Chưa thể sửa định dạng cả dòng an toàn; giữ nguyên cỡ hợp lệ khác và kiểm tra thủ công."));
            }
            else if(p.FormatObserved && (p.FormatMixed || p.Font!="Times New Roman" ||
                p.Color is not (0 or -16777216) || !float.IsFinite(p.Size) ||
                (date ? p.Size<12.99f || p.Size>14.01f || !p.Italic : Math.Abs(p.Size-13)>.01f || p.Italic)))
            {
                if(known && !p.FormatMixed && !string.IsNullOrWhiteSpace(p.Font) && float.IsFinite(p.Size) && p.Size>0 && p.Size<=1638)
                {
                    string text=Clean(p.Text);
                    float size=date && p.Size>=13 && p.Size<=14?p.Size:13;
                    result.Add(new(p.Start,text.Length,text,text,
                        "Sửa phông Times New Roman, màu đen, cỡ "+(date?"13–14, chữ nghiêng":"13, chữ đứng")+
                        "; giữ cỡ hợp lệ, chữ, ô trống, đậm/gạch chân và căn đoạn. Chưa kiểm vị trí tương đối.",false,
                        FixKind.AdministrativeFormat,Citation:Citation(date),
                        Title:date?"Định dạng địa danh/ngày tháng":"Định dạng số/ký hiệu",CharacterFormat:new(size,date)));
                }
                else result.Add(Notice(p,date,"Định dạng chưa đồng nhất hoặc mẫu dòng chưa rõ; kiểm Times New Roman, màu đen, cỡ "+
                    (date?"13–14, chữ nghiêng.":"13, chữ đứng.")+" Giữ thủ công để tránh thay định dạng chưa xác minh."));
            }
        }
        return result;
    }
    private static Issue Notice(Paragraph p,bool date,string message)
    {
        string text=Clean(p.Text);
        return new(p.Start,text.Length,text,text,message,false,date?FixKind.AdministrativeDate:FixKind.AdministrativeNumber,
            CanFix:false,Citation:Citation(date),Title:date?"Kiểm tra địa danh, ngày tháng":"Kiểm tra số, ký hiệu văn bản");
    }
    private static void Edit(Paragraph p,bool date,int offset,string original,string replacement,string title,List<Issue> result,string? citation=null)
    {
        if(original==replacement)return;
        if(original.Length==0)throw new InvalidOperationException("An administrative edit must retain a source anchor");
        result.Add(new(p.Start+offset,original.Length,original,replacement,title,false,
            date?FixKind.AdministrativeDate:FixKind.AdministrativeNumber,Citation:citation??Citation(date),Title:title));
    }
    private static bool CheckNumber(Paragraph p,List<Issue> result,DocumentTypeEntry? namedType)
    {
        string text=Clean(p.Text);
        var prefix=NumberPrefix.Match(text);
        string value=prefix.Groups["value"].Value.TrimEnd(' ','\t');
        var parsed=Identifier.Match(value);
        if(!parsed.Success)
        {
            result.Add(Notice(p,false,"Chưa phân tích chắc chắn số/ký hiệu. Kiểm tra dấu hai chấm, dấu / và nhóm viết tắt có dấu -. Mẫu có năm hoặc cấu trúc riêng cần duyệt thủ công."));
            return false;
        }
        var number=parsed.Groups["number"];
        if(!Blank(number.Value) && number.Value.All(c=>c=='0'))
        {
            result.Add(Notice(p,false,"Số thứ tự đang bằng 0; cần kiểm tra số được cấp, không tự chọn số khác."));return false;
        }
        string gap=prefix.Groups["colon"].Length>0?prefix.Groups["gap"].Value:prefix.Groups["before"].Value;
        string wanted="Số:"+(Blank(number.Value)&&gap.Length>0?gap:" ");
        int label=prefix.Groups["label"].Index,baseIndex=prefix.Groups["value"].Index;
        Edit(p,false,label,text[label..baseIndex],wanted,"Chuẩn hóa nhãn Số và dấu hai chấm",result);
        if(number.Length==1 && number.Value[0] is >= '1' and <= '9')
            Edit(p,false,baseIndex+number.Index,number.Value,"0"+number.Value,"Thêm 0 trước số văn bản nhỏ hơn 10",result);
        var slash=parsed.Groups["slash"];
        Edit(p,false,baseIndex+slash.Index,slash.Value,"/","Bỏ khoảng trắng quanh dấu / của số, ký hiệu",result);
        var code=parsed.Groups["code"];
        foreach(Match separator in Hyphen.Matches(code.Value))
            Edit(p,false,baseIndex+code.Index+separator.Index,separator.Value,"-","Chuẩn hóa dấu nối trong ký hiệu",result);
        var firstSeparator=Hyphen.Match(code.Value);
        var type=firstSeparator.Success?DocumentTypeCatalog.FindAbbreviation(code.Value[..firstSeparator.Index]):null;
        bool mismatch=false;
        if(namedType is not null && type is {NamedHeading:true} && type!=namedType)
        {
            mismatch=true;
            string actual=code.Value[..firstSeparator.Index];
            result.Add(new(p.Start+baseIndex+code.Index,actual.Length,actual,actual,
                "Tên loại “"+namedType.Heading+"” có viết tắt “"+namedType.Abbreviation+
                "”, nhưng ký hiệu đang dùng “"+actual+"”. Đối chiếu tên loại và ký hiệu đã đăng ký; sửa bên ghi sai trong Word rồi quét lại.",
                false,FixKind.AdministrativeNumber,CanFix:false,
                Citation:DocumentTypeCatalog.WithSource("NĐ 30/2020/NĐ-CP, Phụ lục I, Phần I.II.3.b: ký hiệu gồm chữ viết tắt tên loại và cơ quan/chức danh ban hành; Phụ lục III, phần I quy định chữ viết tắt tên loại.",
                    type.Source==DocumentTypeSource.UserSupplement || namedType.Source==DocumentTypeSource.UserSupplement),
                Title:TypeMismatchTitle));
        }
        if(type is not null && type==namedType)
            Edit(p,false,baseIndex+code.Index,code.Value[..firstSeparator.Index],type.Abbreviation,
                "Sai hoa/thường viết tắt tên loại",result,
                DocumentTypeCatalog.WithSource(Citation(false),type.Source==DocumentTypeSource.UserSupplement));
        // A catalog-shaped prefix may be an agency code in a Công văn. Without
        // the corresponding title, retain the old uppercase-code check only;
        // do not invent a type/casing warning for an otherwise uppercase code.
        string lettersToCheck=type is not null && (mismatch || type==namedType || code.Value[..firstSeparator.Index]==type.Abbreviation)
            ?code.Value[(firstSeparator.Index+firstSeparator.Length)..]:code.Value;
        if(lettersToCheck.Any(char.IsLower))
            result.Add(Notice(p,false,"Ký hiệu có chữ thường; đối chiếu quy định chữ in hoa và mã viết tắt của cơ quan. Chưa tự đổi mã."));
        return true;
    }
    private static bool CheckDate(Paragraph p,List<Issue> result)
    {
        string text=Clean(p.Text);
        var match=DateRow.Match(text);
        if(!match.Success)
        {
            result.Add(Notice(p,true,"Chưa phân tích chắc chắn các trường địa danh/ngày/tháng/năm; cần kiểm tra mẫu, không tự đoán giá trị."));return false;
        }
        string[] names=["day","month","year"];
        string[] values=names.Select(n=>match.Groups[n].Value.Trim(' ','\t')).ToArray();
        int?[] numbers=new int?[3];
        for(int i=0;i<3;i++)
        {
            if(Blank(values[i]))continue;
            if(!values[i].All(c=>c is >= '0' and <= '9') || values[i].Length>(i==2?4:2) ||
                i==2 && values[i].Length!=4 || !int.TryParse(values[i],out int number))
            {result.Add(Notice(p,true,"Trường ngày/tháng/năm chưa đủ rõ hoặc năm chưa có đủ bốn chữ số; không tự điền hoặc đổi giá trị."));return false;}
            numbers[i]=number;
        }
        if(numbers[0] is <1 or >31 || numbers[1] is <1 or >12 || numbers[2] is <1 or >9999 ||
            numbers[0] is int day && numbers[1] is int month &&
            day>(numbers[2] is int year?DateTime.DaysInMonth(year,month):month==2?29:DateTime.DaysInMonth(2000,month)))
        {result.Add(Notice(p,true,"Ngày/tháng/năm không hợp lệ theo lịch; cần sửa giá trị gốc, không tự chọn ngày thay thế."));return false;}
        var separator=match.Groups["separator"];
        Edit(p,true,separator.Index,separator.Value,", ","Chuẩn hóa dấu phẩy sau địa danh",result);
        for(int i=0;i<3;i++)
        {
            var label=match.Groups[names[i]+"Label"];
            Edit(p,true,label.Index,label.Value,new[]{"ngày","tháng","năm"}[i],"Viết thường từ ngày, tháng, năm",result);
            var zone=match.Groups[names[i]];
            if(Blank(values[i]))continue; // Preserve all intentional spaces and placeholders.
            string number=values[i];
            if(i==0 && numbers[i]<10 && number.Length==1 || i==1 && numbers[i] is 1 or 2 && number.Length==1)number="0"+number;
            string tail=i<2?" ":new string(zone.Value.Reverse().TakeWhile(c=>c is ' ' or '\t').Reverse().ToArray());
            Edit(p,true,zone.Index,zone.Value," "+number+tail,"Chuẩn hóa cách ghi và khoảng trắng ngày tháng",result);
        }
        if(char.IsLower(match.Groups["place"].Value[0]))
            result.Add(Notice(p,true,"Địa danh bắt đầu bằng chữ thường; cần đối chiếu tên chính thức. Chưa tự đổi tên hoặc chữ hoa của địa danh."));
        return true;
    }
    public static bool IsBlankTemplateSpacing(string text,int start,int length)
    {
        text=Clean(text);
        var date=DateRow.Match(text);
        if(!date.Success)date=StandaloneDate.Match(text);
        if(!date.Success)date=DateBlankZones.Match(text);
        if(date.Success)
            foreach(string name in new[]{"day","month","year"})
            {
                var zone=date.Groups[name];
                if(Blank(zone.Value.Trim(' ','\t')) && start>=zone.Index && (long)start+length<=zone.Index+zone.Length)return true;
            }
        if(NumberHint.IsMatch(text))
        {
            var prefix=NumberPrefix.Match(text);string value=prefix.Groups["value"].Value;
            int labelEnd=prefix.Groups["label"].Index+prefix.Groups["label"].Length;
            int slash=value.IndexOf('/');
            if(slash>=0 && Blank(value[..slash].Trim(' ','\t')) && start>=labelEnd &&
                (long)start+length<=prefix.Groups["value"].Index)return true;
        }
        return false;
    }
}
