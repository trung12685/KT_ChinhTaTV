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

# Additional fictional examples for the profile selector.
import xml.etree.ElementTree as ET
W = 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'
ET.register_namespace('w', W)
def tag(name): return '{' + W + '}' + name
def profile_sample(filename, paragraphs, *, width=11906, height=16838):
    doc = ET.Element(tag('document')); body = ET.SubElement(doc, tag('body'))
    for text, size, bold, center in paragraphs:
        p = ET.SubElement(body, tag('p'))
        if center:
            pr = ET.SubElement(p, tag('pPr')); ET.SubElement(pr, tag('jc'), {tag('val'): 'center'})
        r = ET.SubElement(p, tag('r')); pr = ET.SubElement(r, tag('rPr'))
        ET.SubElement(pr, tag('rFonts'), {tag('ascii'): 'Times New Roman', tag('hAnsi'): 'Times New Roman'})
        ET.SubElement(pr, tag('sz'), {tag('val'): str(size * 2)})
        if bold: ET.SubElement(pr, tag('b'))
        parts = text.split('\t')
        for i, part in enumerate(parts):
            if i: ET.SubElement(r, tag('tab'))
            t = ET.SubElement(r, tag('t'), {'{http://www.w3.org/XML/1998/namespace}space': 'preserve'}); t.text = part
    section = ET.SubElement(body, tag('sectPr'))
    ET.SubElement(section, tag('pgSz'), {tag('w'): str(width), tag('h'): str(height)})
    ET.SubElement(section, tag('pgMar'), {tag('top'): '1134', tag('bottom'): '1134', tag('left'): '1701', tag('right'): '850'})
    with ZipFile(root / 'samples' / filename, 'w', ZIP_DEFLATED) as z, ZipFile(root / 'samples' / 'VanBanMau.docx') as template:
        for name in ['[Content_Types].xml', '_rels/.rels']: z.writestr(name, template.read(name))
        z.writestr('word/document.xml', ET.tostring(doc, encoding='utf-8', xml_declaration=True))
profile_sample('VanBanDangMau.docx', [
    ('Đảng cộng sản Việt Nam', 14, False, True),
    ('TÀI LIỆU GIẢ ĐỂ THỬ — KHÔNG CÓ GIÁ TRỊ PHÁP LÝ', 14, True, True),
    ('Tôi đi trên một con đo. Đây  là ví dụ.', 14, False, False)])
profile_sample('VanBanCaNhanMau.docx', [
    ('\t\tTiêu đề cá nhân', 16, True, True),
    ('Tôi đi trên một con đo. Đây  là ví dụ.', 12, False, False)], width=8391, height=11906)
