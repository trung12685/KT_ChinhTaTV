"""Create fictional number/date fixtures from the public synthetic two-cell header.

Do not use private uploaded documents. Existing samples are left byte-for-byte intact.
"""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
ET.register_namespace("w", W)


def build(filename, number, date):
    with ZipFile(ROOT / "samples/BaDuongKeThieu.docx") as source:
        document = ET.fromstring(source.read("word/document.xml"))
        paragraphs = document.findall(".//{" + W + "}p")
        for index, text in ((3, number), (7, date)):
            paragraph = paragraphs[index]
            run = paragraph.find("{" + W + "}r")
            run.find("{" + W + "}t").text = text
            run.find("{" + W + "}t").set("{http://www.w3.org/XML/1998/namespace}space", "preserve")
            if index == 7:
                ET.SubElement(run.find("{" + W + "}rPr"), "{" + W + "}i")
        with ZipFile(ROOT / "samples" / filename, "w", ZIP_DEFLATED) as output:
            for entry in source.infolist():
                output.writestr(entry.filename, ET.tostring(document, encoding="utf-8", xml_declaration=True)
                                if entry.filename == "word/document.xml" else source.read(entry.filename))


build("SoKyHieuNgayThang.docx", "số  :  3 / BC – ABC", "Địa danh  Ngày3  Tháng1  Năm2026")
build("TruongSoNgayDeTrong.docx", "Số:        /BC-ABC", "Địa danh, ngày        tháng 10 năm 2026")
print("Created two fictional header samples; existing samples unchanged")
