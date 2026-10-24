using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

internal static class OfficeCaseStyleCheck
{
    private const string Lower="  Báo cáo  ",Upper="  BÁO CÁO  ";
    public static void Run(WordSession test,string modern,string folder,Action<string> pass)
    {
        string isolated=Path.Combine(folder,"case-styles");Directory.CreateDirectory(isolated);
        string seed=OfficeComponentCheck.Create(modern,isolated,false);
        for(int mode=0;mode<3;mode++)RunCase(test,Create(seed,isolated,mode),isolated,mode,pass);
    }
    private static void RunCase(WordSession test,string source,string folder,int mode,Action<string> pass)
    {
        byte[] hash=SHA256.HashData(File.ReadAllBytes(source));test.Open(source);test.Profile=DocumentProfile.State;
        string original=test.ReadCheckText();var found=test.Check();var capital=found.Single(i=>i.CanFix && i.Kind==FixKind.DocumentTypeText);
        var probe=test.CaseTrackingRegions([capital]).Single();var before=test.ReadCheckCurrentStyles(Lower);
        var font=found.SingleOrDefault(i=>i.CanFix && i.Kind==FixKind.DocumentHeadingFormat && i.Start==probe.Start);
        if((font is null)!=(mode==0) || mode==2 && font?.CharacterFormat?.RepairInvalidSizes!=true)
            throw new InvalidOperationException("Mẫu viết hoa chưa phân biệt đúng cỡ hợp lệ/phông sai/cỡ sai hỗn hợp.");
        var expectedCase=CasePreservationPlan.Expected(probe,[capital],before,i=>"  ");
        bool failed=false;
        try{test.FixTextFailureProbe([capital],1);}catch(InvalidOperationException e) when(e.InnerException is OfficeTextProbeException){failed=true;}
        test.Check();
        if(!failed || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed || !before.Matches(test.ReadCheckCurrentStyles(Lower)))
            throw new InvalidOperationException("Lỗi sau viết hoa chưa phục hồi nguyên chữ/định dạng.");
        capital=test.Check().Single(i=>i.CanFix && i.Kind==FixKind.DocumentTypeText);
        probe=test.CaseTrackingRegions([capital]).Single();
        expectedCase=CasePreservationPlan.Expected(probe,[capital],before,i=>"  ");
        test.Fix(capital);test.Check();
        if(test.ReadCheckText()!=original.Replace(Lower,Upper,StringComparison.Ordinal) || !expectedCase.Matches(test.ReadCheckCurrentStyles(Upper)) ||
            test.Check().Any(i=>i.CanFix && i.Kind==FixKind.DocumentTypeText))
            throw new InvalidOperationException("Sửa riêng viết hoa chưa giữ từng cỡ/phông/màu/kiểu/căn đoạn.");
        if(mode>0 && !test.Check().Any(i=>i.CanFix && i.CharacterFormat is not null && i.Start==probe.Start))
            throw new InvalidOperationException("Viết hoa đã làm mất lỗi phông chưa chọn.");
        test.Undo();test.Check();
        if(test.ReadCheckText()!=original || !before.Matches(test.ReadCheckCurrentStyles(Lower)))
            throw new InvalidOperationException("Undo viết hoa chưa phục hồi định dạng.");
        if(font is not null)
        {
            var currentFont=test.Check().Single(i=>i.CanFix && i.Kind==FixKind.DocumentHeadingFormat && DocumentTypeCatalog.FindHeading(i.Original) is not null);
            test.Fix(currentFont);test.Check();
            if(test.ReadCheckText()!=original || !before.WithFormat(currentFont.CharacterFormat!).Matches(test.ReadCheckCurrentStyles(Lower)) ||
                !test.Check().Any(i=>i.CanFix && i.Kind==FixKind.DocumentTypeText))
                throw new InvalidOperationException("Sửa riêng phông làm đổi tên loại hoặc mất mục viết hoa chưa chọn.");
            test.Undo();test.Check();
        }
        Issue[] Select()
        {
            var current=test.Check();var lines=OfficeComponentCheck.ReadLines(test);var model=new IssueReviewModel(current.Concat(lines));
            model.Choices.Single(c=>c.Issue.LineFix?.Role==LineRole.Subject).IsChecked=true;
            var chosen=model.Choices.Where(c=>c.IsChecked).Select(c=>c.Issue).ToArray();
            if(chosen.Count(i=>i.LineFix is not null)!=1 || chosen.Count(i=>i.Kind==FixKind.DocumentTypeText)!=1 ||
                chosen.Count(i=>i.CharacterFormat is not null)!=(mode==0?0:1))
                throw new InvalidOperationException("Chọn Line trích yếu chưa tích kèm đúng viết hoa/phông cần duyệt.");
            return chosen;
        }
        var selected=Select();probe=test.CaseTrackingRegions(selected.Where(i=>StagedRepairBatch.Stage(i)==RepairStage.Text).ToArray()).Single();
        var expected=CasePreservationPlan.Expected(probe,selected,before,i=>"  ");
        bool restored=false;
        try{test.FixStagedFailureProbe(selected,3);}catch(InvalidOperationException e) when(e.InnerException is OfficeStagedProbeException){restored=true;}
        test.Check();
        if(!restored || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed || !before.Matches(test.ReadCheckCurrentStyles(Lower)))
            throw new InvalidOperationException("Lỗi sau Line chưa phục hồi toàn lượt viết hoa/phông.");
        test.FixSelection(Select());test.Check();
        if(test.ReadCheckText()!=original.Replace(Lower,Upper,StringComparison.Ordinal) || !expected.Matches(test.ReadCheckCurrentStyles(Upper)) ||
            test.CheckUndoCount!=1 || test.Check().Any(i=>i.Kind==FixKind.DocumentTypeText || i.Kind==FixKind.DocumentHeadingFormat))
            throw new InvalidOperationException("Viết hoa/phông/Line chưa giữ đúng định dạng đã duyệt sau đo lại.");
        var pending=OfficeComponentCheck.ReadLines(test).Select(i=>i.LineFix!.Role).OrderBy(r=>r).ToArray();
        if(!pending.SequenceEqual(new[]{LineRole.Agency,LineRole.Motto}))
            throw new InvalidOperationException("Sửa Line trích yếu làm mất hai Line chưa chọn hoặc chưa đạt nét đã chọn.");
        string saved=Path.Combine(folder,$"case-style-{mode}-fixed.docx");test.SaveCopy(saved);test.Undo();test.Check();
        if(test.ReadCheckText()!=original || !before.Matches(test.ReadCheckCurrentStyles(Lower)))
            throw new InvalidOperationException("Một Undo chưa phục hồi toàn lượt viết hoa/Line.");
        test.Open(saved);test.Check();
        if(test.ReadCheckText()!=original.Replace(Lower,Upper,StringComparison.Ordinal) || !expected.Matches(test.ReadCheckCurrentStyles(Upper)) ||
            !OfficeComponentCheck.ReadLines(test).Select(i=>i.LineFix!.Role).OrderBy(r=>r).SequenceEqual(new[]{LineRole.Agency,LineRole.Motto}) || !hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))
            throw new InvalidOperationException("Lưu/mở viết hoa chưa giữ định dạng hoặc nguồn thay đổi.");
        pass($"Viết hoa cỡ hỗn hợp mẫu{mode}: chọn riêng chữ/phông, giữ định dạng/căn đoạn, chọn Line kéo đúng phụ thuộc, hậu kiểm/rollback/một Undo/lưu-mở/băm nguồn");
    }
    private static string Create(string seed,string folder,int mode)
    {
        string path=Path.Combine(folder,$"case-style-{mode}.docx");File.Copy(seed,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;XDocument xml;
        using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var p=xml.Descendants(w+"p").Single(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value))=="BÁO CÁO");
        XElement Run(string text,int size,bool underline,bool wrong)=>new(w+"r",new XElement(w+"rPr",
            new XElement(w+"rFonts",new XAttribute(w+"ascii",wrong?"Arial":"Times New Roman"),new XAttribute(w+"hAnsi",wrong?"Arial":"Times New Roman")),
            new XElement(w+"sz",new XAttribute(w+"val",size*2)),new XElement(w+"b",new XAttribute(w+"val",wrong?0:1)),
            new XElement(w+"i",new XAttribute(w+"val",wrong?1:0)),new XElement(w+"color",new XAttribute(w+"val",wrong?"FF0000":"000000")),
            new XElement(w+"u",new XAttribute(w+"val",underline?"single":"none"))),new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text));
        p.Elements(w+"r").Remove();p.Add(Run("  ",8,false,false),Run("Báo",mode==2?12:13,true,mode>0),Run(" ",8,false,false),
            Run("cáo",14,false,mode>0),Run("  ",8,false,false));
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
}
