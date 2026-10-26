using ChinhTa.Core;

namespace ChinhTa.Desktop;

public static class PageNumberDocumentReader
{
    // Document.WordOpenXML includes all stories and their relationships. A range
    // exports only the parts needed by that range; it cannot prove footer absence.
    public static PageNumberPackage ReadCurrent(object documentObject)
    {
        dynamic document=documentObject;
        return PageNumberPackage.FromFlat((string)document.WordOpenXML);
    }
}
