using System.Text.Json;

namespace ChinhTa.Desktop;

internal static class OfficeReadComparisonEvidence
{
    public static void RequireEqual(string legacy,string experimental,string label,string legacyPath,string experimentalPath)
    {
        if(legacy==experimental)return;
        using var a=JsonDocument.Parse(legacy);using var b=JsonDocument.Parse(experimental);
        string detail=Difference(a.RootElement,b.RootElement,"$")??"$ — khác thứ tự thuộc tính hoặc biểu diễn JSON";
        var error=new InvalidOperationException("Hai cách đọc khác kết quả: "+label+". "+detail);
        error.Data["ChinhTa.ReadComparison"]="READ_COMPARISON_MISMATCH "+detail+Environment.NewLine+
            "LegacySnapshot="+legacyPath+Environment.NewLine+"ExperimentalSnapshot="+experimentalPath;
        throw error;
    }
    private static string? Difference(JsonElement a,JsonElement b,string path)
    {
        static string Preview(JsonElement v){string s=v.GetRawText();return s.Length<=120?s:s[..120]+"…";}
        string Changed()=>path+": cũ="+Preview(a)+"; thử nghiệm="+Preview(b);
        if(a.ValueKind!=b.ValueKind)return Changed();
        if(a.ValueKind==JsonValueKind.Object)
        {
            foreach(var p in a.EnumerateObject())
            {
                if(!b.TryGetProperty(p.Name,out var next))return path+"."+p.Name+": thiếu ở bộ đọc thử nghiệm";
                string? difference=Difference(p.Value,next,path+"."+p.Name);if(difference is not null)return difference;
            }
            foreach(var p in b.EnumerateObject())if(!a.TryGetProperty(p.Name,out _))return path+"."+p.Name+": chỉ có ở bộ đọc thử nghiệm";
            return null;
        }
        if(a.ValueKind==JsonValueKind.Array)
        {
            if(a.GetArrayLength()!=b.GetArrayLength())return path+".Length: cũ="+a.GetArrayLength()+"; thử nghiệm="+b.GetArrayLength();
            for(int i=0;i<a.GetArrayLength();i++)
            {string? difference=Difference(a[i],b[i],path+"["+i+"]");if(difference is not null)return difference;}
            return null;
        }
        return a.GetRawText()==b.GetRawText()?null:Changed();
    }
}
