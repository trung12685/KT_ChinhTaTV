using System.IO;
using System.Runtime.InteropServices;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml.Linq;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class SignatureSpaceDesktopChecks
{
    public sealed class Layout
    {
        private float before=6;public bool Drift {get;set;}public bool Fail {get;set;}public int Writes {get;private set;}
        public int Alignment=>1;public float LeftIndent=>0;public float RightIndent=>0;public float FirstLineIndent=>0;
        public float SpaceBefore{get=>before;set{Writes++;if(Fail)throw new COMException("synthetic");before=value;if(Drift)SpaceAfter=9;}}
        public float SpaceAfter{get;set;}=6;public int LineSpacingRule=>0;public float LineSpacing=>14;
        public int SpaceBeforeAuto{get;set;}=0;public int SpaceAfterAuto=>0;
    }
    public sealed class Range
    {
        public string Text {get;set;}="Nguyễn Văn An";
        public Layout ParagraphFormat {get;}=new();
        public object Font=>throw new InvalidOperationException("A space-only writer cannot access Font");
    }
    public static void Run(Action<bool,string> assert)
    {
        var range=new Range();var before=VisibleFormatReader.ReadParagraph(range);int releases=0;
        var expected=SignatureSpaceWriter.SetBefore(range,range.Text,before,80,_=>releases++);
        assert(expected.Spacing!.Before==80 && expected.Matches(VisibleFormatReader.ReadParagraph(range)) && range.ParagraphFormat.Writes==1 && releases==1,
            "The handwriting-space writer changes only before-spacing without accessing Font or changing after/line/alignment/indent");
        bool stale=false;try{SignatureSpaceWriter.SetBefore(range,range.Text,before,90,_=>{});}catch(InvalidOperationException){stale=true;}
        assert(stale && range.ParagraphFormat.Writes==1,"A stale paragraph snapshot is rejected before another before-spacing setter");
        var automatic=new Range();automatic.ParagraphFormat.SpaceBeforeAuto=-1;
        var autoStyle=VisibleFormatReader.ReadParagraph(automatic);SignatureSpaceWriter.SetBefore(automatic,automatic.Text,autoStyle,80,_=>{});
        assert(automatic.ParagraphFormat.SpaceBeforeAuto==0 && automatic.ParagraphFormat.SpaceAfterAuto==0,"Only the selected before-auto spacing is disabled for explicit handwriting space");
        var drift=new Range();drift.ParagraphFormat.Drift=true;bool rejected=false;
        try{SignatureSpaceWriter.SetBefore(drift,drift.Text,VisibleFormatReader.ReadParagraph(drift),80,_=>{});}catch(InvalidOperationException){rejected=true;}
        assert(rejected,"An unintended after-spacing drift fails immediate verification");
        var failing=new Range();failing.ParagraphFormat.Fail=true;int released=0;bool com=false;
        try{SignatureSpaceWriter.SetBefore(failing,failing.Text,VisibleFormatReader.ReadParagraph(failing),80,_=>released++);}catch(COMException){com=true;}
        assert(com && released==1,"A Word spacing failure preserves the real COM error and releases the acquired paragraph format");
        var partial=new Range();partial.ParagraphFormat.SpaceBeforeAuto=-1;partial.ParagraphFormat.Fail=true;
        var partialStyle=VisibleFormatReader.ReadParagraph(partial);int restored=0,committed=0;bool partialFailure=false;
        try{SignatureWritingSpace.Apply(6,new(1,1,1,20),value=>{
            SignatureSpaceWriter.SetBefore(partial,partial.Text,partialStyle,value,_=>{});return new(1,1,1,80);
        },_=>{},()=>committed++,()=>{restored++;partial.ParagraphFormat.SpaceBeforeAuto=-1;});}catch(COMException){partialFailure=true;}
        assert(partialFailure && restored==1 && committed==0 && partial.ParagraphFormat.SpaceBeforeAuto==-1,
            "A failure after disabling before-auto spacing reaches whole-action restoration and publishes no Undo");

        const string authority="TRƯỞNG PHÒNG",name="Nguyễn Văn An";
        var target=new SignatureWritingSpaceTarget([new(0,authority.Length+1,authority+"\r",1,true,true)],new(authority.Length+3,authority.Length+name.Length+4,name+"\r",1,true,true),3);
        foreach(double gap in new[]{20d,80d,120d})
        {
            var page=new FixedPage{Width=800,Height=1100};var canvas=new Canvas{RenderTransform=new ScaleTransform(4d/3,4d/3)};page.Children.Add(canvas);
            Glyphs Glyph(string text,double y)=>new(){FontUri=new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"arial.ttf")),
                FontRenderingEmSize=14,OriginX=100,OriginY=y,UnicodeString=text,Fill=Brushes.Black};
            var a=Glyph(authority,100);canvas.Children.Add(a);var box=a.ToGlyphRun().ComputeAlignmentBox();
            var n=Glyph(name,100+box.Bottom+gap-box.Y);canvas.Children.Add(n);
            var content=new PageContent();((IAddChild)content).AddChild(page);var doc=new FixedDocument();doc.Pages.Add(content);
            var reference=new DocumentReference();reference.SetDocument(doc);var sequence=new FixedDocumentSequence();sequence.References.Add(reference);
            var anchors=new Dictionary<Issue,PreviewAnchor>{{SignatureWritingSpaceTarget.Probe(target.Authorities[0]),new(1,authority+name,0,authority.Length)},
                {SignatureWritingSpaceTarget.Probe(target.Name),new(1,authority+name,authority.Length,name.Length)}};
            var measurement=SignatureSpaceMeasurementReader.Read(sequence,new PreviewHighlighter(),target,anchors);
            assert(measurement.Verified && measurement.PageCount==1 && Math.Abs(measurement.Gap!.Value-gap)<.01,
                "Handwriting space is measured between actual transformed XPS text rows in points, independent of Enter counts");
            n.Opacity=0;
            assert(!SignatureSpaceMeasurementReader.Read(sequence,new PreviewHighlighter(),target,anchors).Verified,
                "A hidden signer cannot authorize handwriting-space measurement");
            n.Opacity=1;n.Clip=new RectangleGeometry(new Rect(100,0,5,1000));
            assert(!SignatureSpaceMeasurementReader.Read(sequence,new PreviewHighlighter(),target,anchors).Verified,
                "A partly clipped signer cannot pass the80pt postcondition");
        }
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-hand-space-fixture-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            foreach(int blanks in new[]{0,2})
            {
                string path=Path.Combine(folder,blanks.ToString());Directory.CreateDirectory(path);
                string sample=OfficeSignatureWritingSpaceCheck.Create(Path.Combine("samples","BaDuongKeThieu.docx"),path,blanks);
                using(var package=DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(sample,false))
                    assert(!new DocumentFormat.OpenXml.Validation.OpenXmlValidator().Validate(package).Any(),
                        "The no-image handwriting-space Office sample is schema-valid with either0or2existing Enter paragraphs");
                using var zip=ZipFile.OpenRead(sample);using var input=zip.GetEntry("word/document.xml")!.Open();var xml=XDocument.Load(input);
                int cp=91;var live=xml.Descendants(w+"body").Single().Descendants(w+"p").Select(p=>{
                    bool cell=p.Ancestors(w+"tc").Any();string text=string.Concat(p.Descendants(w+"t").Select(t=>t.Value))+"\r"+(cell && !p.ElementsAfterSelf(w+"p").Any()?"\a":"");
                    var row=new WordParagraphSnapshot(cp,cp+text.Length,text,1,false,false);cp=row.End;return row;
                }).ToArray();
                var resolved=SignatureWritingSpaceTarget.Read(xml.ToString(),live,DocumentProfile.State);
                assert(!xml.Descendants(w+"drawing").Any() && resolved.Authorities.Count==1 && resolved.Name.Text.Contains("Nguyễn Văn An"),
                    "The handwriting-space action requires no signature image and resolves both proven text roles after60paragraphs");
            }
        }
        finally{Directory.Delete(folder,true);}
    }
}
