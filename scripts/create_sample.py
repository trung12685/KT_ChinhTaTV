"""Create a small non-sensitive DOCX fixture using only Python's standard library."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED

root = Path(__file__).resolve().parents[1]
with ZipFile(root / 'samples' / 'VanBanMau.docx', 'w', ZIP_DEFLATED) as z:
    z.writestr('[Content_Types].xml', '''<?xml version="1.0" encoding="UTF-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/></Types>''')
    z.writestr('_rels/.rels', '''<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/></Relationships>''')
    z.writestr('word/document.xml', '''<?xml version="1.0" encoding="UTF-8"?><w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>
<w:p><w:r><w:t>Cộng hòa xã hội chủ nghĩa Việt Nam</w:t></w:r></w:p>
<w:p><w:r><w:t>VĂN BẢN THỬ NGHIỆM — KHÔNG CÓ GIÁ TRỊ PHÁP LÝ</w:t></w:r></w:p>
<w:p><w:r><w:t xml:space="preserve">Tôi đi trên một con đo. Đây  là văn bản mẫu.</w:t></w:r></w:p>
<w:p><w:r><w:t>Tôi đo chiều dài. Tôi đi trên một con đò.</w:t></w:r></w:p>
<w:tbl><w:tblPr><w:tblW w:w="9000" w:type="dxa"/></w:tblPr><w:tblGrid><w:gridCol w:w="9000"/></w:tblGrid><w:tr><w:tc><w:tcPr><w:tcW w:w="9000" w:type="dxa"/></w:tcPr><w:p><w:r><w:t>Trong bảng: một con đo.</w:t></w:r></w:p></w:tc></w:tr></w:tbl>
<w:p><w:r><w:br w:type="page"/><w:t>Trang hai: một con đo.</w:t></w:r></w:p>
<w:sectPr><w:pgSz w:w="11906" w:h="16838"/><w:pgMar w:top="1134" w:right="850" w:bottom="1134" w:left="1701"/></w:sectPr>
</w:body></w:document>''')
print('Created samples/VanBanMau.docx')
