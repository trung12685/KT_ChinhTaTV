using ChinhTa.Core;

namespace ChinhTa.Desktop;

public static class PageNumberDocumentReader
{
    // Document.WordOpenXML includes all stories and their relationships. A range
    // exports only the parts needed by that range; it cannot prove footer absence.
    public static string ReadXml(object documentObject)
    {
        dynamic document=documentObject;
        string xml=(string)document.WordOpenXML;
        if(string.IsNullOrWhiteSpace(xml))throw new InvalidOperationException("Thiếu gói XML đầy đủ của Word hiện tại.");
        return xml;
    }
    public static PageNumberPackage ReadCurrent(object documentObject)=>PageNumberPackage.FromFlat(ReadXml(documentObject));
}
