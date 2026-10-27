using System.IO;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public sealed partial class WordSession
{
    private readonly OfficeMappingEvidence mappingEvidence=new();
    private void CaptureMappingEvidence(string? currentXml,IReadOnlyList<WordParagraphSnapshot> live,
        IReadOnlyList<WordParagraphSnapshot> header,IReadOnlySet<int> text,IReadOnlySet<int> format,
        WordXmlMappingException? textWarning,WordXmlMappingException? tableWarning,bool textUnavailable,bool tableUnavailable)
    {
        try
        {
            var capture=mappingEvidence.Capture(LineCheckArtifactFolder,()=>new(Path.GetFileName(Source),UseOptimizedReading,currentXml,
                live.ToArray(),header.ToArray(),text.Order().ToArray(),format.Order().ToArray(),
                textWarning?.DiagnosticCode,tableWarning?.DiagnosticCode,textUnavailable,tableUnavailable));
            if(capture?.Written==true)File.AppendAllText(Path.Combine(LineCheckArtifactFolder!,"office-check.txt"),
                "MAPPING_EVIDENCE "+Path.GetFileName(capture.Path)+Environment.NewLine);
        }
        catch(Exception error) when(error is IOException or UnauthorizedAccessException)
        {
            // A failed diagnostic write never changes the outcome or grants
            // permissions. The original mismatch/unavailable notice remains.
            Diagnostics.Write("OfficeCheck.MappingEvidence.Capture",error);
        }
    }
}
