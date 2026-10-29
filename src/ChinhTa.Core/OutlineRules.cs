using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ChinhTa.Core;

public sealed record OutlineFinding(int ParagraphStart,Issue Issue);

// Structural context for typed paragraph-start labels. Does not edit Word numPr,
// move paragraphs, infer omitted content, or rewrite references to other items.
public static class OutlineRules
{
    private enum Family { Roman, Number, Letter, Dash, Plus }
    private sealed record Marker(Paragraph Row,Family Family,string Label,string Delimiter,int Value,int Lead,int Prefix,int Indent,string Leading,bool GapTab,string[]? NumberPath=null);
    private sealed class Node(Marker? marker)
    {
        public Marker? Marker { get; }=marker;
        public List<Node> Children { get; }=[];
        public bool Complete { get; set; }=true;
        public bool NumberedDependents {get;set;}
    }
    private static readonly Regex Prefix=new(@"^(?<lead>[ \t]{0,32})(?:(?<number>[0-9]{1,3}(?:\.[0-9]{1,3}){0,3})(?<nd>[.)])|(?<roman>[IVXLCDM]{1,12})(?<rd>[.)])|(?<letter>[a-zđ])(?<ld>[.)])|(?<bullet>[-–—+]))(?<gap>[ \t]+)(?<body>.+)$",RegexOptions.CultureInvariant|RegexOptions.NonBacktracking);
    private static readonly Regex Hint=new(@"^[ \t]*(?:(?:[0-9]+\.)+[0-9]+[.)]|[0-9]{1,3}[.)]|[IVXLCDM]{1,12}[.)]|[a-zđ][.)]|[-–—+•●○*])(?:\s|$)",RegexOptions.CultureInvariant|RegexOptions.NonBacktracking);
    private const string ReferenceLabel=@"(?:[0-9]+(?:\.[0-9]+)*|[IVXLCDM]{1,12}|[a-zđ])";
    private const string ReferenceSpace=@"[\p{Zs}\t]";
    private const string ReferenceLabelToken=@"(?:\("+ReferenceSpace+@"*(?<label>"+ReferenceLabel+@")"+ReferenceSpace+@"*\)|(?<label>"+ReferenceLabel+@")(?:[.)])?)";
    private sealed record NumericReferenceRange(string[] Prefix,string Lower,string Upper);
    // Expand only fixed cue words, never normalize the paragraph or reuse
    // normalized offsets as Word CPs. References grant no edit permission;
    // they conservatively freeze numbering that would leave dangling labels.
    private static string ReferenceWord(string word)
    {
        string decomposed=word.Normalize(NormalizationForm.FormD);
        return word==decomposed?Regex.Escape(word):"(?:"+Regex.Escape(word)+"|"+Regex.Escape(decomposed)+")";
    }
    private static string ReferenceKey(string label)
    {
        if(label.Length==0 || !char.IsAsciiDigit(label[0]))return label;
        // Numeric references may omit label padding. Compare each component's
        // value as text, without changing the source label/CP or guessing that
        // Roman/letter references belong to an Arabic numbering family.
        return string.Join('.',label.Split('.').Select(component=>
        {string digits=component.TrimStart('0');return digits.Length==0?"0":digits;}));
    }
    private static readonly Regex References=new(@"\b(?:"+string.Join('|',new[]{"mục","điểm","khoản","phần","chương"}.Select(ReferenceWord))+@")"+
        ReferenceSpace+@"+(?:"+ReferenceWord("số")+ReferenceSpace+@"+)?"+ReferenceLabelToken+
        @"(?:(?<joiner>"+ReferenceSpace+@"*[-–—,]"+ReferenceSpace+@"*|"+
        ReferenceSpace+@"+(?:"+string.Join('|',new[]{"và","đến","tới"}.Select(ReferenceWord))+@")"+ReferenceSpace+@"+)"+
        ReferenceLabelToken+@")*(?![0-9]|\.[0-9])(?=\s|[,;:.!?]|$)",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    private static readonly Regex NumberPathHint=new(@"^[ \t]*(?<label>[0-9]+(?:\.[0-9]+)+)",RegexOptions.CultureInvariant|RegexOptions.NonBacktracking);
    private static readonly string[] Alphabets=["abcdefghijklmnopqrstuvwxyz","abcdđeghiklmnopqrstuvxy"];
    public static IReadOnlyList<OutlineFinding> Check(IReadOnlyList<Paragraph> rows)
    {
        var result=new List<OutlineFinding>();var roots=new List<Node>();var root=new Node(null);var path=new List<Node>();
        var references=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var referenceRanges=new List<NumericReferenceRange>();
        var uncertainRangePrefixes=new List<string[]>();
        foreach(var match in rows.SelectMany(p=>References.Matches(p.Text).Cast<Match>()))
        {
            string[] labels=match.Groups["label"].Captures.Cast<Capture>().Select(c=>ReferenceKey(c.Value)).ToArray();
            foreach(string key in labels)
            {
                references.Add(key);
                if(key.Length>0 && char.IsAsciiDigit(key[0]))
                    // Edited paths have at most four components. Deeper references
                    // still guard those prefixes without quadratic prefix allocation.
                    for(int dot=key.IndexOf('.'),depth=0;dot>=0 && depth<4;dot=key.IndexOf('.',dot+1),depth++)references.Add(key[..dot]);
            }
            var joiners=match.Groups["joiner"].Captures;
            for(int i=0;i<joiners.Count;i++)
            {
                string joiner=joiners[i].Value.Trim().Normalize(NormalizationForm.FormC);
                if(joiner is not ("-" or "–" or "—") && !joiner.Equals("đến",StringComparison.OrdinalIgnoreCase) && !joiner.Equals("tới",StringComparison.OrdinalIgnoreCase))continue;
                if(!char.IsAsciiDigit(labels[i][0]) || !char.IsAsciiDigit(labels[i+1][0]))continue;
                string[] left=labels[i].Split('.'),right=labels[i+1].Split('.');
                int common=0;while(common<Math.Min(left.Length,right.Length) && left[common]==right[common])common++;
                if(left.Length==right.Length && common>=left.Length-1)
                {
                    string lower=left[^1],upper=right[^1];
                    if(CompareReferenceNumber(lower,upper)>0)(lower,upper)=(upper,lower);
                    referenceRanges.Add(new(left[..^1],lower,upper));
                }
                else
                    // An explicit range across different hierarchy prefixes/depths
                    // cannot be resolved as one sibling interval. Freeze the shared
                    // numeric subtree instead of guessing its interior labels.
                    uncertainRangePrefixes.Add(left[..common]);
            }
        }
        bool Referenced(string label)
        {
            string key=ReferenceKey(label);
            if(references.Contains(key))return true;
            if(key.Length==0 || !char.IsAsciiDigit(key[0]))return false;
            string[] parts=key.Split('.');
            bool StartsWith(string[] prefix)=>parts.Length>=prefix.Length && parts.Take(prefix.Length).SequenceEqual(prefix,StringComparer.Ordinal);
            return uncertainRangePrefixes.Any(StartsWith) || referenceRanges.Any(range=>parts.Length>range.Prefix.Length && StartsWith(range.Prefix) &&
                CompareReferenceNumber(parts[range.Prefix.Length],range.Lower)>=0 && CompareReferenceNumber(parts[range.Prefix.Length],range.Upper)<=0);
        }
        void Notice(Paragraph row,FixKind kind,string title,string message)
        {
            string text=row.Text.TrimEnd('\r','\n','\a');
            result.Add(new(row.Start,new(row.Start,text.Length,text,text,message,false,kind,CanFix:false,Title:title)));
        }
        void End(bool uncertain)
        {
            if(uncertain){root.Complete=false;foreach(var open in path)open.Complete=false;}
            if(root.Children.Count>0)roots.Add(root);
            root=new(null);path.Clear();
        }
        bool automaticRun=false;
        foreach(var row in rows)
        {
            string text=row.Text.TrimEnd('\r','\n','\a');var info=row.Outline;
            var numberHint=NumberPathHint.Match(text);
            if(numberHint.Success && info?.Scope!=OutlineScope.Recipient)
                foreach(var open in path.Where(n=>n.Marker!.NumberPath is not null &&
                    numberHint.Groups["label"].Value.StartsWith(n.Marker.Label+".",StringComparison.Ordinal)))open.NumberedDependents=true;
            if(info is null){End(true);automaticRun=false;continue;}
            if(info.Scope==OutlineScope.Recipient){End(false);automaticRun=false;continue;}
            if(info.Scope!=OutlineScope.PlainBody || !row.TextEditable && !info.LabelThroughTabs)
            {
                End(true);
                if(info.Scope==OutlineScope.AutomaticList && !automaticRun)
                    Notice(row,FixKind.OutlineOrder,"Kiểm tra danh sách tự động","Ký hiệu do Word sinh tự động, không phải chữ gõ tay. Chưa kiểm/sửa thứ tự hoặc bỏ dấu đầu dòng qua văn bản; kiểm tra thiết lập danh sách trong Word.");
                else if(info.Scope!=OutlineScope.AutomaticList && Hint.IsMatch(text))
                    Notice(row,FixKind.OutlineOrder,"Cấu trúc mục chưa rõ","Có ký hiệu mục trong bảng/vùng cấu trúc hoặc chưa xác minh được kiểu/thụt lề. Kiểm tra thủ công, không đổi nhãn hoặc bỏ dấu đầu dòng.");
                automaticRun=info.Scope==OutlineScope.AutomaticList;continue;
            }
            automaticRun=false;
            var marker=Parse(row,text);
            if(marker is null)
            {
                if(numberHint.Success && !Hint.IsMatch(text))
                {
                    root.Complete=false;foreach(var open in path)open.Complete=false;
                    continue; // A decimal value is not itself an outline error.
                }
                if(Hint.IsMatch(text))
                {End(true);Notice(row,FixKind.OutlineOrder,"Ký hiệu mục cần kiểm tra","Ký hiệu/nội dung đầu mục chưa thuộc mẫu rõ để xác định cấp. Chưa tự sửa thứ tự hoặc bỏ dấu.");}
                else if(info.HeadingLevel is not null || text.Count(char.IsLetter)>=4 && text.Length<160 && text.Any(c=>c==' ') && text.Where(char.IsLetter).All(char.IsUpper))End(false);
                continue;
            }
            if(marker.NumberPath is {Length:>1} && path.Count(n=>DirectNumberChild(marker,n.Marker!))>1)
            {End(true);Notice(row,FixKind.OutlineOrder,"Mục cha nhiều cấp chưa rõ","Có nhiều mục cha đang mở cùng tiền tố số; chưa tự chọn cấp theo thụt lề hoặc đổi nhãn.");continue;}
            while(path.Count>0 && !ChildOf(marker,path[^1].Marker!))path.RemoveAt(path.Count-1);
            var parent=path.Count>0?path[^1]:root;var node=new Node(marker);parent.Children.Add(node);path.Add(node);
        }
        End(false);
        var pending=new Stack<Node>(roots);
        while(pending.Count>0)
        {
            var parent=pending.Pop();foreach(var child in parent.Children)pending.Push(child);
            foreach(var group in parent.Children.GroupBy(n=>n.Marker!.Family))
            {
                var nodes=group.ToArray();var members=nodes.Select(n=>n.Marker!).ToArray();var first=members[0];
                if(first.Family is Family.Dash or Family.Plus)
                {
                    if(members.Length!=1)continue;
                    if(parent.Marker is null || !parent.Complete || first.GapTab || first.Row.Outline!.HasReferences)
                    {Notice(first.Row,FixKind.OutlineMarker,"Dấu đầu dòng cần kiểm tra","Chưa xác định trọn vẹn mục cha hoặc tài liệu có dẫn chiếu cấu trúc. Chưa đủ bằng chứng dấu này đứng một mình; không tự bỏ.");continue;}
                    string symbol=first.Label;
                    result.Add(new(first.Row.Start,new(first.Row.Start+first.Lead,first.Prefix,first.Row.Text.Substring(first.Lead,first.Prefix),"",
                        $"Chỉ có một dấu {symbol} cùng cấp trong mục cha {parent.Marker.Label}{parent.Marker.Delimiter}. Theo lựa chọn người dùng, đề xuất bỏ dấu và khoảng cách phân cách; giữ nội dung/thụt đầu dòng. Cần duyệt mục cha và các mục con.",
                        false,FixKind.OutlineMarker,Title:$"Dấu {symbol} đứng một mình trong mục")));
                    continue;
                }
                bool compound=members.Any(m=>m.NumberPath is {Length:>1});
                if(compound && (parent.Marker is null || members.Any(m=>!DirectNumberChild(m,parent.Marker) || !CanonicalNumberPath(m)) || !CanonicalNumberPath(parent.Marker)))
                {Notice(members.First(m=>m.NumberPath is {Length:>1}).Row,FixKind.OutlineOrder,"Mục nhiều cấp cần kiểm tra","Chưa có mục cha số trực tiếp hoặc chuỗi trộn cấp/cách ghi số 0 đầu. Chưa tự đổi nhãn mục nhiều cấp.");continue;}
                if(members.Length<2)continue;
                if(members.Select(m=>(m.Indent,m.Leading)).Distinct().Count()!=1 || members.Select(m=>m.Delimiter).Distinct().Count()!=1 || members.Any(m=>m.Value<=0) || first.Value!=1 || members.Skip(1).Any(m=>m.Value==1))
                {Notice(first.Row,FixKind.OutlineOrder,"Thứ tự mục cần kiểm tra","Chuỗi cùng cấp thiếu mốc đầu 1/a/I, bắt đầu lại, ký hiệu không chuẩn hoặc trộn cách ghi. Chưa tự đánh lại số; cần đối chiếu mục cha.");continue;}
                bool padded=first.Family==Family.Number && !compound && first.Label.StartsWith('0');
                if(first.Family==Family.Number && !compound && members.Any(m=>padded?m.Label.Length!=first.Label.Length:m.Label.StartsWith('0')))
                {Notice(first.Row,FixKind.OutlineOrder,"Cách ghi số mục chưa thống nhất","Chuỗi trộn số có/không có số 0 đầu hoặc độ rộng chưa rõ. Chưa tự chọn cách đánh số mới.");continue;}
                var alphabets=Alphabets.Where(a=>members.All(m=>m.Label.Length==1 && a.Contains(m.Label[0]))).ToArray();
                bool dependentPrefix=first.Family==Family.Number && nodes.Select((node,index)=>
                    node.Marker!.Value!=index+1 && node.NumberedDependents).Any(x=>x);
                string NumberLabel(int value)=>compound?string.Join('.',first.NumberPath![..^1])+"."+value.ToString(CultureInfo.InvariantCulture):
                    value.ToString(padded?new string('0',first.Label.Length):"0",CultureInfo.InvariantCulture);
                string[] Expected(int index)=>first.Family switch
                {
                    Family.Number when index<999=>[NumberLabel(index+1)],
                    Family.Roman when index<3999=>[Roman(index+1)],
                    Family.Letter=>alphabets.Where(a=>index<a.Length).Select(a=>a[index].ToString()).Distinct().ToArray(),
                    _=>[]
                };
                bool referenced=members.Any(m=>m.Row.Outline!.HasReferences || Referenced(m.Label)) ||
                    Enumerable.Range(0,members.Length).SelectMany(Expected).Any(Referenced);
                for(int i=1;i<members.Length;i++)
                {
                    var item=members[i];var expected=Expected(i);
                    if(expected.Contains(item.Label,StringComparer.Ordinal))continue;
                    if(dependentPrefix)
                    {Notice(item.Row,FixKind.OutlineOrder,"Đổi mục cha cần kiểm tra mục con","Đánh lại chuỗi này cần đổi cả tiền tố số của mục con. Chưa tự sửa từng nhãn để tránh để lại mục con sai; đối chiếu cả nhóm trong Word.");continue;}
                    if(expected.Length!=1 || referenced || compound && !parent.Complete)
                    {Notice(item.Row,FixKind.OutlineOrder,"Thứ tự mục cần đối chiếu","Chuỗi có ký hiệu cần xem lại nhưng bảng chữ cái, giới hạn hoặc dẫn chiếu mục chưa đủ chắc. Không tự chọn nhãn thay thế.");continue;}
                    int tail=compound?item.Label.LastIndexOf('.')+1:0;
                    string original=item.Label[tail..],replacement=compound?expected[0][(expected[0].LastIndexOf('.')+1)..]:expected[0];
                    result.Add(new(item.Row.Start,new(item.Row.Start+item.Lead+tail,original.Length,original,replacement,
                        $"Trong cùng mục cha, vị trí thứ {i+1} dự kiến là {expected[0]}{item.Delimiter}, hiện ghi {item.Label}{item.Delimiter}. Chỉ đổi nhãn, giữ nguyên tiêu đề/nội dung; đối chiếu xem có thiếu nội dung hoặc dẫn chiếu trước khi duyệt.",
                        false,FixKind.OutlineOrder,Title:$"Thứ tự mục: {item.Label} → {expected[0]}",
                        OutlineContext:new(parent.Marker?.Row.Start,first.Row.Start,item.Label,item.Delimiter))));
                }
            }
        }
        return result.OrderBy(f=>f.Issue.Start).ToArray();
    }
    // Components are canonical digit strings, so length then ordinal comparison
    // has arbitrary precision and never overflows on an external reference.
    private static int CompareReferenceNumber(string left,string right)=>left.Length!=right.Length?left.Length.CompareTo(right.Length):string.CompareOrdinal(left,right);
    private static bool CanonicalNumberPath(Marker marker)=>marker.NumberPath is { } path && path.All(p=>p.Length<=3 && p[0] is >='1' and <='9');
    private static bool DirectNumberChild(Marker child,Marker parent)=>child.NumberPath is {Length:>1} c &&
        parent.NumberPath is { } p && c.Length==p.Length+1 && c.Take(p.Length).SequenceEqual(p,StringComparer.Ordinal);
    private static bool ChildOf(Marker child,Marker parent)
    {
        if(child.NumberPath is {Length:>1})return DirectNumberChild(child,parent);
        if(parent.NumberPath is {Length:>1} && child.Family==Family.Number)return false;
        return (int)child.Family>(int)parent.Family || child.Family==parent.Family &&
        (child.Leading==parent.Leading && child.Indent>parent.Indent || child.Indent==parent.Indent &&
            child.Leading.Length>parent.Leading.Length &&
            (child.Leading.All(c=>c==' ') && parent.Leading.All(c=>c==' ') || child.Leading.All(c=>c=='\t') && parent.Leading.All(c=>c=='\t')));
    }
    private static Marker? Parse(Paragraph row,string text)
    {
        var m=Prefix.Match(text);if(!m.Success)return null;
        string body=m.Groups["body"].Value;
        int letters=0;foreach(char c in body){if(c==' ')break;if(char.IsLetter(c))letters++;else if(char.GetUnicodeCategory(c) is not (UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark))return null;}
        if(letters<2 || body.IndexOfAny(['\t','\v','\f','=','`','<','>','^','|','{','}','[',']'])>=0)return null;
        Family family;string label,delimiter;int value;
        if(m.Groups["number"].Success){family=Family.Number;label=m.Groups["number"].Value;delimiter=m.Groups["nd"].Value;value=int.Parse(label.Split('.').Last(),CultureInfo.InvariantCulture);}
        else if(m.Groups["roman"].Success){family=Family.Roman;label=m.Groups["roman"].Value;delimiter=m.Groups["rd"].Value;value=RomanValue(label);}
        else if(m.Groups["letter"].Success){family=Family.Letter;label=m.Groups["letter"].Value;delimiter=m.Groups["ld"].Value;value=label=="a"?1:2;}
        else{label=m.Groups["bullet"].Value;family=label=="+"?Family.Plus:Family.Dash;delimiter="";value=0;}
        string leading=m.Groups["lead"].Value;
        if(leading.Contains(' ' ) && leading.Contains('\t'))return null;
        int lead=leading.Length;
        return new(row,family,label,delimiter,value,lead,label.Length+delimiter.Length+m.Groups["gap"].Length,row.Outline!.Indent,leading,m.Groups["gap"].Value.Contains('\t'),family==Family.Number?label.Split('.'):null);
    }
    private static int RomanValue(string text)
    {
        int Value(char c)=>c switch {'I'=>1,'V'=>5,'X'=>10,'L'=>50,'C'=>100,'D'=>500,'M'=>1000,_=>0};
        int total=0;for(int i=0;i<text.Length;i++)total+=i+1<text.Length && Value(text[i])<Value(text[i+1])?-Value(text[i]):Value(text[i]);
        return total is >0 and <4000 && Roman(total)==text?total:0;
    }
    private static string Roman(int number)
    {
        string text="";foreach(var (value,mark) in new[]{(1000,"M"),(900,"CM"),(500,"D"),(400,"CD"),(100,"C"),(90,"XC"),(50,"L"),(40,"XL"),(10,"X"),(9,"IX"),(5,"V"),(4,"IV"),(1,"I")})
            while(number>=value){text+=mark;number-=value;}return text;
    }
}
