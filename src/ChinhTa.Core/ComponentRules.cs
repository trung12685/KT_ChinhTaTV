using System.Text.RegularExpressions;

namespace ChinhTa.Core;

public sealed record ComponentTarget(Paragraph Row,ComponentRole Role,bool Last=false);
public static class ComponentRules
{
    public const string AgencyCitation="NĐ30, PL I, Phần I.I.4 và II.2.b (PDF 21–22).";
    public const string RecipientCitation="NĐ30, PL I, Phần I.I.4 và II.9.d, ô 9b (PDF 21, 28).";
    public static IReadOnlyList<ComponentTarget> Identify(IReadOnlyList<Paragraph> paragraphs,IReadOnlyList<LayoutParagraph> layout,DocumentProfile profile)
    {
        if(profile!=DocumentProfile.State)return [];
        var result=paragraphs.Where(p=>p.Outline?.Scope==OutlineScope.Recipient && p.Outline.RecipientRole is not null)
            .Select(p=>new ComponentTarget(p,p.Outline!.RecipientRole!.Value,p.Outline.LastRecipient)).ToList();
        var mottos=layout.Where(p=>p.Page==1 && p.InTable && HeadingRules.IsCandidate(p.Text,profile) && p.Text.TrimStart().StartsWith("Độc lập",StringComparison.OrdinalIgnoreCase)).ToArray();
        if(mottos.Length!=1)return result;
        var motto=mottos[0];var left=layout.Where(p=>p.Page==1 && p.InTable && p.TableStart==motto.TableStart && p.CellStart<motto.CellStart && p.CellEnd<=motto.CellStart+1).ToArray();
        if(left.Length==0)return result;
        int cell=left.Max(p=>p.CellStart);var rows=left.Where(p=>p.CellStart==cell && !string.IsNullOrWhiteSpace(p.Text)).OrderBy(p=>p.Start).ToArray();
        var numbers=rows.Where(p=>AdministrativeHeaderRules.IsNumber(p.Text)).ToArray();
        if(numbers.Length!=1)return result;
        var names=rows.Where(p=>p.Start<numbers[0].Start).ToArray();
        if(names.Length is <1 or >2 || names.Any(p=>p.Text.TrimEnd('\r','\a','\n').IndexOfAny(['\t','\v','\f'])>=0 || p.Text.Count(char.IsLetter)<3))return result;
        for(int i=0;i<names.Length;i++)
        {
            var matched=paragraphs.Where(p=>p.Start==names[i].Start && p.Text==names[i].Text).ToArray();
            if(matched.Length==1)result.Add(new(matched[0],i==names.Length-1?ComponentRole.Agency:ComponentRole.AgencyParent));
        }
        return result;
    }
    public static IReadOnlyList<Issue> Check(IReadOnlyList<Paragraph> paragraphs,IReadOnlyList<LayoutParagraph> layout,DocumentProfile profile)
    {
        var result=new List<Issue>();
        foreach(var target in Identify(paragraphs,layout,profile))
        {
            var p=target.Row;var role=target.Role;string text=p.Text.TrimEnd('\r','\a','\n');
            bool agency=role is ComponentRole.Agency or ComponentRole.AgencyParent;
            string label=role switch {ComponentRole.AgencyParent=>"cơ quan chủ quản",ComponentRole.Agency=>"cơ quan ban hành",ComponentRole.RecipientLabel=>"nhãn Nơi nhận",_=>"dòng nơi nhận"};
            string citation=agency?AgencyCitation:RecipientCitation;
            CharacterFormat style=role switch
            {
                ComponentRole.AgencyParent=>new(p.Size>=12 && p.Size<=13?p.Size:12,false,false,role),
                ComponentRole.Agency=>new(p.Size>=12 && p.Size<=13?p.Size:12,false,true,role),
                ComponentRole.RecipientLabel=>new(12,true,true,role),_=>new(11,false,null,role)
            };
            FixKind formatKind=agency?FixKind.AgencyFormat:FixKind.RecipientFormat,textKind=agency?FixKind.AgencyText:FixKind.RecipientText;
            if(!p.FormatObserved || p.FormatMixed || p.BoldMixed || !p.TextEditable)
                result.Add(new(p.Start,text.Length,text,text,"Chưa đủ bằng chứng chữ/định dạng đồng nhất để sửa "+label+"; không kết luận thành phần này sai thể thức.",false,formatKind,
                    CanFix:false,Citation:citation,Title:"Kiểm tra định dạng "+label));
            else if(!style.Matches(p.Font,p.Size,p.Italic?-1:0,p.Color,p.Bold?-1:0))
                result.Add(new(p.Start,text.Length,text,text,"Định dạng "+label+" chưa đúng thông số đã đối chiếu; giữ nguyên nội dung và căn đoạn.",false,formatKind,
                    Citation:citation,Title:"Định dạng "+label,CharacterFormat:style));
            void Edit(int offset,string original,string replacement,string title)
            {
                if(original==replacement)return;
                result.Add(new(p.Start+offset,original.Length,original,replacement,title,false,textKind,CanFix:p.TextEditable,Citation:citation,Title:title));
            }
            if(agency)
            {
                foreach(Match word in Regex.Matches(text,@"[\p{L}\p{M}]+"))
                    Edit(word.Index,word.Value,word.Value.ToUpperInvariant(),"Viết hoa tên "+label);
                continue;
            }
            if(text.IndexOfAny(['\t','\v','\f'])>=0)continue;
            if(role==ComponentRole.RecipientLabel)
            {
                var words=Regex.Matches(text,@"[\p{L}\p{M}]+");
                if(words.Count==2)
                {
                    Edit(words[0].Index,words[0].Value,"Nơi","Chuẩn hóa nhãn Nơi nhận:");
                    Edit(words[1].Index,words[1].Value,"nhận"+(text.TrimEnd().EndsWith(':')?"":":"),"Chuẩn hóa nhãn Nơi nhận:");
                }
                continue;
            }
            int lead=text.Length-text.TrimStart(' ').Length;
            string body=text[lead..].TrimEnd(' ');if(body.Length==0)continue;
            bool dash=body[0] is '-' or '–' or '—';char punctuation=target.Last?'.':';';
            if(!dash && body.Length==1)
            {Edit(lead,body,"- "+body+punctuation,"Dấu đầu/cuối dòng nơi nhận chưa đúng");continue;}
            if(!dash)Edit(lead,body[..1],"- "+body[..1],"Thiếu gạch đầu dòng nơi nhận");
            else if(body.Length>1 && body[1]!=' ')Edit(lead,body[..1],body[..1]+" ","Thiếu khoảng sau gạch đầu dòng nơi nhận");
            char last=body[^1];string desired=last is ';' or '.'?punctuation.ToString():last+punctuation.ToString();
            Edit(lead+body.Length-1,last.ToString(),desired,"Dấu cuối dòng nơi nhận chưa đúng");
        }
        return result;
    }
}
