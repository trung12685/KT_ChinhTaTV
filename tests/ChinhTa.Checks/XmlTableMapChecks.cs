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
        var boundaryFlags = WordXmlTableMap.Read(xml,live.Select(p=>p with {InTable=false}).ToArray());
        assert(boundaryFlags[0].InTable && boundaryFlags[2].InTable && !boundaryFlags[4].InTable &&
            ShapeLineRules.Identify(boundaryFlags,DocumentProfile.State).Count == 3,
            "Lấy cấu trúc ô từ XML đã khớp text, không chặn vì cờ COM ở ranh giới bảng");
        assert(!WordXmlTableMap.Read(xml,live.Take(1).ToArray())[0].Centered, "Không đoán vùng ô khi chưa đối chiếu hết các đoạn trong ô");
        var extraBodyBlank = live.Take(5).Concat(new[] { new WordParagraphSnapshot(754,755,"\r",1,false,true) })
            .Concat(live.Skip(5).Select(p=>p with {Start=p.Start+1,End=p.End+1})).ToArray();
        var alignedBlank = WordXmlTableMap.Read(xml,extraBodyBlank);
        assert(alignedBlank[5].Start==755 && ShapeLineRules.Identify(alignedBlank,DocumentProfile.State).Count==3,
            "Bỏ qua đoạn Word rỗng bổ sung ngoài bảng, giữ CP thật của trích yếu");
        var extraCellBlank = live.Take(2).Concat(new[] {new WordParagraphSnapshot(718,719,"\r",1,true,true)})
            .Concat(live.Skip(2).Select(p=>p with {Start=p.Start+1,End=p.End+1})).ToArray();
        assert(Reject(xml,extraCellBlank), "Không bỏ qua đoạn rỗng khác biệt bên trong ô bảng");
        var tabStopXml = XDocument.Parse(xml);
        tabStopXml.Descendants(w+"p").First().AddFirst(new XElement(w+"pPr",new XElement(w+"tabs",
            new XElement(w+"tab",new XAttribute(w+"val","center"),new XAttribute(w+"pos","1000")))));
        assert(WordXmlTableMap.Read(tabStopXml.ToString(),live).Count == live.Length,
            "Tab stop trong pPr là định dạng, không bị cộng nhầm thành ký tự tab nội dung");
        var realTabXml = XDocument.Parse(xml);
        realTabXml.Descendants(w+"r").First().AddFirst(new XElement(w+"tab"));
        assert(Reject(realTabXml.ToString(),live), "Tab thật trong run vẫn phải khớp nội dung Word");
        var breakXml = XDocument.Parse(xml);
        breakXml.Descendants(w+"r").First().Add(new XElement(w+"br",new XAttribute(w+"type","page")),
            new XElement(w+"br",new XAttribute(w+"type","column")),new XElement(w+"br"),
            new XElement(w+"noBreakHyphen"),new XElement(w+"softHyphen"));
        var breaksLive = live.Select((p,i)=>i==0?p with {Text="CƠ QUAN BAN HÀNH\f\u000e\v\u001e\u001f\r"}:p).ToArray();
        assert(WordXmlTableMap.Read(breakXml.ToString(),breaksLive).Count == live.Length,
            "Đối chiếu đúng mã Word cho ngắt trang/cột/dòng và gạch nối đặc biệt");
        var laterXml = XDocument.Parse(xml);
        laterXml.Descendants(w+"body").Single().Add(new XElement(w+"p",new XElement(w+"r",new XElement(w+"t","Đoạn sau"))));
        WordXmlMappingException? warning = null;
        var partial = WordXmlTableMap.Read(laterXml.ToString(),[..live,new(775,790,"Khác nội dung\r",1,false,false)],e=>warning=e);
        assert(warning is not null && warning.DiagnosticCode.StartsWith("XML-TEXT.P8.") &&
            ShapeLineRules.Identify(partial,DocumentProfile.State).Count==3,
            "Đoạn sau không khớp không vô hiệu hóa ba vùng đầu đã khớp");
        assert(warning is not null && !warning.DiagnosticCode.Contains("Khác") && !warning.DiagnosticCode.Contains("Đoạn"),
            "Mã chẩn đoán chỉ có mã/số, không có nội dung tài liệu");
        var truncatedTitle = WordXmlTableMap.Read(xml,live.Select((p,i)=>i==6?p with {Text="Không khớp\r"}:p).ToArray(),_=>{});
        assert(!ShapeLineRules.Identify(truncatedTitle,DocumentProfile.State).Any(t=>t.Role==LineRole.Subject),
            "Không tạo Line trích yếu khi phần đối chiếu dừng trước khi biết khối trích yếu đã kết thúc");
        var partialCell = WordXmlTableMap.Read(xml,live.Select((p,i)=>i==1?p with {Text="Khác\r"}:p).ToArray(),_=>{});
        assert(partialCell.Count==1 && !partialCell[0].Centered && partialCell[0].CellEnd==0,
            "Lỗi đối chiếu giữa ô không cho phép tạo Line trên vùng ô chưa khớp đủ");
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
