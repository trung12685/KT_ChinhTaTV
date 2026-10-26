using ChinhTa.Core;

public static class PreparedPreviewChecks
{
    public static void Run(Action<bool,string> assert)
    {
        string[] sources=["Tôi đi trên con đo. Đây là văn bản.","con đocon đo","Đây  là mẫu","Một dòng\rHai dòng\a",
            "Trước 😀 sau 😀","ngắt\u00adchữ và ghép e\u0301","    ","Một\tđoạn\vcuối",""];
        foreach(string source in sources)
        {
            var prepared=PreviewLocator.Prepare(source);
            string[] targets=[source,source.Replace(" ",""),"ĐẦU"+source+"CUỐI",source+source,"không có nội dung này"];
            bool same=true;
            foreach(string target in targets)
            {
                var rendered=PreviewLocator.Prepare(target);
                for(int start=-1;start<=source.Length+1;start++)
                    for(int length=0;length<=7;length++)
                    {
                        var anchor=new PreviewAnchor(1,source,start,length);
                        same&=PreviewLocator.Locate(anchor,target)==PreviewLocator.Locate(anchor,prepared,rendered);
                    }
            }
            assert(same,"Prepared preview preserves all offsets/lengths, whitespace and ambiguity decisions for a representative Unicode/control case");
        }
        const string repeated="Tôi đi con đo. Lại đi con đo.";
        int last=repeated.LastIndexOf("đo",StringComparison.Ordinal);
        assert(PreviewLocator.Locate(new(1,repeated,last,2),PreviewLocator.Prepare(repeated),PreviewLocator.Prepare(repeated))==new TextSpan(last,2),
            "Prepared source indexing still distinguishes the second repeated phrase");
        assert(PreviewLocator.Locate(new(1,"con đocon đo",4,2),PreviewLocator.Prepare("con đocon đo"),PreviewLocator.Prepare("con đo")) is null,
            "Prepared preview never chooses the first match of an ambiguous phrase");
        assert(PreviewLocator.Locate(new(1,"Đây  là mẫu",3,2),PreviewLocator.Prepare("Đây  là mẫu"),PreviewLocator.Prepare("Đây  là mẫu"))==new TextSpan(3,2) &&
            PreviewLocator.Locate(new(1,"Đây  là mẫu",3,2),PreviewLocator.Prepare("Đây  là mẫu"),PreviewLocator.Prepare("Đâylàmẫu")) is null,
            "Whitespace-only errors retain their actual gap and do not highlight an adjacent word");
        assert(PreviewLocator.Locate(new(1,"con đò",4,2),PreviewLocator.Prepare("con đo"),PreviewLocator.Prepare("con đò")) is null,
            "A prepared source from another document version cannot bind a changed anchor");
        var cache=new PreviewTextCache();string shared=new('a',1000);var first=cache.Get(shared);
        assert(ReferenceEquals(first,cache.Get(shared)) && cache.Preparations==1,
            "Repeated anchors sharing the same immutable story normalize it only once");
        string equalCopy=new(shared.ToCharArray());
        assert(!ReferenceEquals(first,cache.Get(equalCopy)) && cache.Preparations==2,
            "Equal text in a different string instance gets independently bound prepared data without hashing the full story");
        cache.Clear();assert(!ReferenceEquals(first,cache.Get(shared)) && cache.Preparations==1,
            "Clearing a preview discards all prepared source data before the next document");
    }
}
