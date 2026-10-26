using System.Globalization;

namespace ChinhTa.Core;

// A single approval covers the defective page properties; legal margins remain
// exact. The source eligibility is re-established before writing the candidate.
public sealed record PageLayoutFix(SectionLayout Before,SectionLayout After)
{
    private static bool In(double n,double lo,double hi)=>n>=lo-.25 && n<=hi+.25;
    private static double Clamp(double n,double lo,double hi)=>In(n,lo,hi)?n:Math.Clamp(n,lo,hi);
    public static bool Known(SectionLayout s)=>new[]{s.WidthMm,s.HeightMm,s.TopMm,s.BottomMm,s.LeftMm,s.RightMm}.All(double.IsFinite) &&
        s.WidthMm is >=100 and <=600 && s.HeightMm is >=100 and <=600 && s.TopMm>=0 && s.BottomMm>=0 && s.LeftMm>=0 && s.RightMm>=0 &&
        s.WidthMm-s.LeftMm-s.RightMm>=40 && s.HeightMm-s.TopMm-s.BottomMm>=40;
    public static PageLayoutFix? Propose(SectionLayout s)
    {
        if(!s.RepairEligible || s.Number!=1 || s.Start<0 || s.Anchor.Length!=1 || char.IsControl(s.Anchor[0]) ||
            char.IsWhiteSpace(s.Anchor[0]) || !Known(s) || s.MirrorMargins || s.WidthMm>s.HeightMm)return null;
        bool paper=!In(s.WidthMm,210,210) || !In(s.HeightMm,297,297);
        var after=s with {WidthMm=paper?210:s.WidthMm,HeightMm=paper?297:s.HeightMm,
            TopMm=Clamp(s.TopMm,20,25),BottomMm=Clamp(s.BottomMm,20,25),LeftMm=Clamp(s.LeftMm,30,35),RightMm=Clamp(s.RightMm,15,20)};
        return after!=s?new(s,after):null;
    }
    public bool Valid=>Known(Before) && Known(After) && Propose(Before)?.After==After;
    public static bool Matches(SectionLayout a,SectionLayout b)=>a.Number==b.Number && a.MirrorMargins==b.MirrorMargins &&
        new[]{a.WidthMm-b.WidthMm,a.HeightMm-b.HeightMm,a.TopMm-b.TopMm,a.BottomMm-b.BottomMm,a.LeftMm-b.LeftMm,a.RightMm-b.RightMm}.All(d=>Math.Abs(d)<=.25);
    private static string N(double n)=>n.ToString("0.##",CultureInfo.InvariantCulture);
    public string Suggestion=>"Đặt "+N(After.WidthMm)+" × "+N(After.HeightMm)+" mm; lề trên/dưới/trái/phải "+
        string.Join("/",new[]{After.TopMm,After.BottomMm,After.LeftMm,After.RightMm}.Select(N))+" mm. Giữ lề đang đúng, chữ và bảng; phân trang/đo Line lại.";
}
