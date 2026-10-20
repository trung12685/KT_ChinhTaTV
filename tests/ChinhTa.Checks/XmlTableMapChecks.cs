using System.Xml.Linq;
using ChinhTa.Core;

public static class XmlTableMapChecks
{
    public static void Run(Action<bool, string> assert)
    {
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        const string xml = """
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>
            <w:tbl><w:tblPr/><w:tr>
            <w:tc><w:p><w:r><w:t>CƠ QUAN BAN HÀNH</w:t></w:r></w:p><w:p/></w:tc>
            <w:tc><w:p><w:r><w:t>Độc lập – Tự do – Hạnh phúc</w:t></w:r></w:p><w:p/></w:tc>
            </w:tr></w:tbl><w:p><w:r><w:t>BÁO CÁO</w:t></w:r></w:p>
            <w:p><w:r><w:t>Trích yếu thử nghiệm</w:t></w:r></w:p><w:p/>
            </w:body></w:document>
            """;
        WordParagraphSnapshot[] live =
        [new(700,716,"CƠ QUAN BAN HÀNH\r",1,true,true), new(716,718,"\r\a",1,true,true),
         new(718,744,"Độc lập – Tự do – Hạnh phúc\r",1,true,true), new(744,746,"\r\a",1,true,true),
         new(746,754,"BÁO CÁO\r",1,false,true), new(754,774,"Trích yếu thử nghiệm\r",1,false,true),
         new(774,775,"\r",1,false,true)];
        var map = WordXmlTableMap.Read(xml,live);
        assert(map[0].CellStart == 700 && map[0].CellEnd == 718 && map[2].CellStart == 718 &&
            map[2].CellEnd == 746 && map[0].TableStart == map[2].TableStart,
            "XML map dùng Start/End Word thật được cung cấp, phân biệt hai ô cùng bảng");
        var targets = ShapeLineRules.Identify(map,DocumentProfile.State);
        assert(targets.Count == 3 && targets.All(t=>t.Centered), "XML map nhận diện đủ cơ quan, tiêu ngữ và trích yếu");
        assert(targets.All(t => ShapeLineRules.Evaluate(t,new(100,100,210),null,null,LinePolicy.Decree30).Fix.ShapeId is null),
            "Ba vùng thiếu Line có thể đề xuất tạo Line, không yêu cầu Line có sẵn");
        bool Reject(string document, WordParagraphSnapshot[] snapshots)
        {
            try { WordXmlTableMap.Read(document,snapshots); return false; }
            catch (InvalidOperationException) { return true; }
        }
        assert(Reject(xml,live.Select((p,i)=>i==0?p with {Text="Nội dung khác\\r"}:p).ToArray()),
            "Không ghép XML cũ/sai nội dung với range Word hiện tại");
        assert(Reject(xml,live.Select((p,i)=>i==0?p with {InTable=false}:p).ToArray()),
            "Không ghép khi trạng thái trong bảng khác Word");
        assert(!WordXmlTableMap.Read(xml,live.Take(1).ToArray())[0].Centered, "Không đoán vùng ô khi chưa đối chiếu hết các đoạn trong ô");
        var doc = XDocument.Parse(xml);
        doc.Descendants(w+"tc").First().AddFirst(new XElement(w+"tcPr",new XElement(w+"tcMar",
            new XElement(w+"left",new XAttribute(w+"type","dxa"),new XAttribute(w+"w",0)),
            new XElement(w+"right",new XAttribute(w+"type","dxa"),new XAttribute(w+"w",100)))));
        assert(!WordXmlTableMap.Read(doc.ToString(),live)[0].Centered,
            "Ô có padding lệch không được tự coi là có tâm chữ trùng tâm ô");
        doc = XDocument.Parse(xml);
        doc.Descendants(w+"tblPr").First().Add(new XElement(w+"tblStyle",new XAttribute(w+"val","Custom")));
        var styles = new XElement(w+"styles",new XElement(w+"style",new XAttribute(w+"styleId","Custom"),
            new XElement(w+"tblPr",new XElement(w+"tblCellMar",
                new XElement(w+"left",new XAttribute(w+"type","dxa"),new XAttribute(w+"w",100)),
                new XElement(w+"right",new XAttribute(w+"type","dxa"),new XAttribute(w+"w",100))))));
        var flat = new XElement("package",doc.Root,styles);
        assert(WordXmlTableMap.Read(flat.ToString(),live)[0].Centered,
            "Đọc được Flat OPC kèm style bảng và padding cân bằng");
        styles.Descendants(w+"right").First().SetAttributeValue(w+"w",200);
        var changedFlat = new XElement("package",XDocument.Parse(xml.Replace("<w:tblPr/>","<w:tblPr><w:tblStyle w:val=\"Custom\"/></w:tblPr>")).Root,styles);
        assert(!WordXmlTableMap.Read(changedFlat.ToString(),live)[0].Centered,
            "Padding lệch thừa kế từ style cũng ngăn tự chèn lệch tâm");
        styles.Elements(w+"style").First().SetAttributeValue(w+"type","table");
        styles.Elements(w+"style").First().SetAttributeValue(w+"default","1");
        var defaultStyleDoc = new XElement("package",XDocument.Parse(xml).Root,styles);
        assert(!WordXmlTableMap.Read(defaultStyleDoc.ToString(),live)[0].Centered,
            "Không bỏ qua padding lệch ở style bảng mặc định khi tblStyle không ghi trực tiếp");
        doc = XDocument.Parse(xml);
        doc.Descendants(w+"tc").First().AddFirst(new XElement(w+"tcPr",new XElement(w+"vMerge")));
        assert(!WordXmlTableMap.Read(doc.ToString(),live)[0].Centered, "Bảng gộp phức tạp không bị đoán tọa độ để tự chèn");
    }
}
