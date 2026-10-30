using System.IO;
using System.Xml.Linq;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class OfficialLetterDesktopChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var style=new CharacterFormat(12,false,false,ComponentRole.LetterSubject,PreserveSize:true,RepairInvalidSizes:true);
        var children=new[]{new SelectiveSizeWriterChecks.Range(0,1,"A",new(12)),new SelectiveSizeWriterChecks.Range(1,2,"B",new(13)),new SelectiveSizeWriterChecks.Range(2,3,"C",new(14))};
        var range=new SelectiveSizeWriterChecks.Range(0,3,"ABC",new(9999999),children);
        VisibleFormatReader.RepairSizes(range,"ABC",style);
        assert(children.Select(c=>c.Font.Size).SequenceEqual(new[]{12f,13,12}) && children.Select(c=>c.Font.Writes).SequenceEqual(new[]{0,0,1}),
            "The real selective-size writer uses the official-letter range, retains 12/13pt and changes only 14pt");
        var suite=OfficeFocusedCheck.Cases.Single(s=>s.Id=="document-subject");
        assert(OfficeFocusedCheck.Scenario(suite.Id).Method.DeclaringType==typeof(OfficeDocumentSubjectCheck) && WordCheckCommand.TryParse(["--word-check",suite.Id],out var selected) && selected.Single()==suite,
            "The document-subject Word scenario is independently selectable and routed to its actual fixture");
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-letter-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            foreach(string? title in new string?[]{null,"QUYẾT ĐỊNH","BÁO CÁO","KẾ HOẠCH"})
            {
                string path=OfficeDocumentSubjectCheck.Create(folder,title);
                using var package=DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(path,false);
                var errors=new DocumentFormat.OpenXml.Validation.OpenXmlValidator().Validate(package).ToArray();
                assert(errors.Length==0,"The independent "+(title??"V/v letter")+" Word fixture is schema-valid: "+string.Join("; ",errors.Take(3).Select(e=>e.Description)));
                var xml=XDocument.Parse(package.MainDocumentPart!.Document!.OuterXml);XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
                var rows=xml.Descendants(w+"p").ToArray();
                string Text(XElement row)=>string.Concat(row.Descendants(w+"t").Select(t=>t.Value));
                if(title is null)
                {
                    var subject=rows.Single(row=>Text(row).StartsWith("V/v",StringComparison.Ordinal));var number=rows.Single(row=>Text(row).StartsWith("Số:",StringComparison.Ordinal));
                    assert(subject.Ancestors(w+"tc").Single()==number.Ancestors(w+"tc").Single() && subject.ElementsBeforeSelf().Contains(number) &&
                        subject.Descendants(w+"sz").Single().Attribute(w+"val")!.Value=="26" && subject.Descendants(w+"b").Single().Attribute(w+"val")!.Value=="0" && number.Descendants(w+"spacing").Single().Attribute(w+"after")!.Value=="120",
                        "The actual letter fixture has a regular 13pt V/v below its own left-cell number with 6pt after-spacing");
                }
                else
                {
                    var heading=rows.Single(row=>Text(row)==title);var subject=rows.Single(row=>Text(row)=="Tổng hợp kết quả công tác");
                    assert(!heading.Ancestors(w+"tbl").Any() && !subject.Ancestors(w+"tbl").Any() && heading.ElementsAfterSelf().Contains(subject) &&
                        new[]{heading,subject}.All(row=>row.Descendants(w+"sz").Single().Attribute(w+"val")!.Value=="28" && row.Descendants(w+"b").Single().Attribute(w+"val")!.Value=="1"),
                        "The actual "+title+" fixture keeps its uppercase heading and bold 14pt subject below the header table");
                }
            }
        }
        finally{Directory.Delete(folder,true);}
    }
}
