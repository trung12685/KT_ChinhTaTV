using System.Xml.Linq;
using ChinhTa.Core;

public static class SignatureWritingSpaceChecks
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main",PKG="http://schemas.microsoft.com/office/2006/xmlPackage";
    private static WordParagraphSnapshot[] Live(Paragraph[] rows)=>rows.Select(p=>new WordParagraphSnapshot(p.Start,p.Start+p.Text.Length,p.Text,1,false,false)).ToArray();
    public static void Run(Action<bool,string> assert)
    {
        foreach(int padding in new[]{0,60})
        foreach(bool soft in new[]{false,true})
        {
            var sample=SignatureChecks.Sample(soft,padding);string xml=sample.Xml.ToString();var native=Live(sample.Rows);
            var target=SignatureWritingSpaceTarget.Read(xml,native,DocumentProfile.State);
            assert(target.Authorities.Count==1 && target.Name.Text.Contains("Nguyễn Văn An") && target.Name.Start==native[target.NameXmlIndex].Start,
                "Handwriting space uses current proven role ranges even after40 and with a multiline authority");
            assert(xml==sample.Xml.ToString() && target.Name.Text==native[target.NameXmlIndex].Text,
                "Selecting the handwriting-space region does not remove Enter paragraphs or guess CP offsets");
            var missing=native.Select(p=>p.Start==target.Name.Start?p with{Text="Không khớp\r"}:p).ToArray();
            bool closed=false;try{SignatureWritingSpaceTarget.Read(xml,missing,DocumentProfile.State);}catch(InvalidOperationException){closed=true;}
            assert(closed,"A mismatched signer cannot authorize spacing before a guessed name");
        }
        foreach(var change in new Action<XDocument>[]
        {
            x=>x.Descendants(W+"tc").Last().Elements(W+"p").Skip(1).First().Add(new XElement(W+"r",new XElement(W+"drawing"))),
            x=>x.Descendants(W+"tc").Last().Elements(W+"p").Skip(1).First().AddFirst(new XElement(W+"pPr",new XElement(W+"numPr"))),
            x=>x.Descendants(W+"tc").Last().Elements(W+"p").Skip(1).First().AddFirst(new XElement(W+"pPr",new XElement(W+"pStyle",new XAttribute(W+"val","Unknown")))),
            x=>x.Descendants(W+"tr").Last().AddFirst(new XElement(W+"trPr",new XElement(W+"trHeight",new XAttribute(W+"hRule","exact"),new XAttribute(W+"val",1500)))),
            x=>x.Descendants(W+"tc").Last().Elements(W+"p").Last().AddFirst(new XElement(W+"pPr",new XElement(W+"framePr"))),
            x=>x.Descendants(W+"tc").Last().Elements(W+"p").Skip(1).First().AddFirst(new XElement(W+"pPr",new XElement(W+"pageBreakBefore")))
        })
        {
            var sample=SignatureChecks.Sample(mutate:change);bool closed=false;
            try{SignatureWritingSpaceTarget.Read(sample.Xml.ToString(),Live(sample.Rows),DocumentProfile.State);}catch(InvalidOperationException){closed=true;}
            assert(closed,"Objects, numbering, unknown blank styles, exact row heights and special paragraph layouts keep handwriting-space edits closed");
        }
        foreach(var profile in new[]{DocumentProfile.Party,DocumentProfile.Personal})
        {
            var sample=SignatureChecks.Sample();bool closed=false;
            try{SignatureWritingSpaceTarget.Read(sample.Xml.ToString(),Live(sample.Rows),profile);}catch(InvalidOperationException){closed=true;}
            assert(closed,"The bounded State signing-space action does not infer roles in other profiles");
        }
        foreach(double initialGap in new[]{0d,15d,32d,65d,76d})
        foreach(float before in new[]{0f,6f,24f})
        {
            int writes=0;var result=SignatureWritingSpace.Adjust(before,new(1,1,1,initialGap),value=>{writes++;return new(1,1,1,initialGap+value-before);});
            assert(result.Changed && Math.Abs(result.Gap-80)<=3 && writes==result.Attempts && writes<=12,
                "Measured feedback supplements real missing space without treating Enter counts as heights");
            assert(Math.Abs(result.Before*20-Math.Round(result.Before*20))<.001,
                "Before-spacing writes use Word's twip resolution instead of an unrepresentable float");
        }
        int collapsedWrites=0;var collapsed=SignatureWritingSpace.Adjust(10,new(1,1,1,70),value=>{collapsedWrites++;return new(1,1,1,20+Math.Max(50,value));});
        assert(collapsed.Changed && Math.Abs(collapsed.Gap-80)<=3 && collapsedWrites>1,
            "Feedback handles Word collapsing adjacent paragraph spacing without adding a blind80pt");
        foreach(double gap in new[]{77d,80d,100d,180d})
        {
            int writes=0,commits=0,restores=0,verifies=0;
            var result=SignatureWritingSpace.Apply(0,new(1,1,1,gap),v=>{writes++;return new(1,1,1,v);},_=>verifies++,()=>commits++,()=>restores++);
            assert(!result.Changed && result.Gap==gap && writes+commits+restores+verifies==0,
                "An already sufficient signing area is retained without extra space or an empty Undo step");
        }
        foreach(string failure in new[]{"ignored","new page","extra page","missing measure","final verify"})
        {
            float working=0;int commits=0,restores=0;bool failed=false;
            try{SignatureWritingSpace.Apply(0,new(1,1,1,20),value=>{
                working=value;return failure switch{
                    "ignored"=>new(1,1,1,20),"new page"=>new(1,2,2,null),"extra page"=>new(1,1,2,20+value),
                    "missing measure"=>new(0,0,1,null),_=>new SignatureSpaceMeasurement(1,1,1,20+value)};
            },_=>{if(failure=="final verify")throw new IOException("late persistence failure");},()=>commits++,()=>{restores++;working=0;});}
            catch(Exception e) when(e is InvalidOperationException or IOException){failed=true;}
            assert(failed && restores==1 && commits==0 && working==0,
                "Ignored spacing, overflow, lost glyph proof or late save verification restores the whole handwriting-space action: "+failure);
        }
        int bounded=0;bool noRoom=false;
        try{SignatureWritingSpace.Adjust(0,new(1,1,1,20),value=>{bounded++;return value>50?new(1,2,2,null):new(1,1,1,20+value);});}
        catch(InvalidOperationException){noRoom=true;}
        assert(noRoom && bounded<=12,"A page that cannot fit80pt fails within a fixed number of measurements");
        bool aggregate=false;
        try{SignatureWritingSpace.Apply(0,new(1,1,1,20),_=>throw new IOException("write"),_=>{},()=>{},()=>throw new IOException("restore"));}
        catch(AggregateException e){aggregate=e.InnerExceptions.Count==2;}
        assert(aggregate,"A restore failure retains both actual causes instead of claiming that the original is restored");

        var document=SignatureChecks.Sample().Xml;int nameIndex=document.Descendants(W+"body").Single().Descendants(W+"p").Count()-2;
        XDocument Copy()=>XDocument.Parse(document.ToString());
        void SetBefore(XDocument d){var p=d.Descendants(W+"body").Single().Descendants(W+"p").ElementAt(nameIndex);p.AddFirst(new XElement(W+"pPr",new XElement(W+"spacing",new XAttribute(W+"before",1600),new XAttribute(W+"beforeAutospacing",0))));}
        var allowed=Copy();SetBefore(allowed);SignatureSpaceXmlProof.Verify(document.ToString(),allowed.ToString(),nameIndex);
        assert(true,"The XML scope proof accepts only before-spacing/auto changes on the proven signer");
        foreach(var mutation in new Action<XDocument>[] {
            x=>x.Descendants(W+"t").First().Value+=" changed",
            x=>x.Descendants(W+"body").Single().Descendants(W+"p").ElementAt(nameIndex).Element(W+"pPr")!.Element(W+"spacing")!.SetAttributeValue(W+"after",120),
            x=>x.Descendants(W+"body").Single().Descendants(W+"p").First().AddFirst(new XElement(W+"pPr",new XElement(W+"spacing",new XAttribute(W+"before",1600)))),
            x=>x.Descendants(W+"t").Last().Parent!.AddFirst(new XElement(W+"rPr",new XElement(W+"b")))})
        {
            var changed=XDocument.Parse(allowed.ToString());mutation(changed);bool rejected=false;
            try{SignatureSpaceXmlProof.Verify(document.ToString(),changed.ToString(),nameIndex);}catch(InvalidOperationException){rejected=true;}
            assert(rejected,"The scope proof rejects changed text, other paragraph spacing, after-spacing or unselected font properties");
        }
        var package=new XDocument(new XElement(PKG+"package",new XElement(PKG+"part",new XAttribute(PKG+"name","/word/document.xml"),new XElement(PKG+"xmlData",new XElement(document.Root!))),
            new XElement(PKG+"part",new XAttribute(PKG+"name","/word/media/image1.png"),new XElement(PKG+"binaryData","AQID")),
            new XElement(PKG+"part",new XAttribute(PKG+"name","/word/footer1.xml"),new XElement(PKG+"xmlData",new XElement(W+"ftr",new XElement(W+"p",new XElement(W+"r",new XElement(W+"t","Giữ footer"))))))));
        foreach(bool image in new[]{false,true})
        {
            var changed=XDocument.Parse(package.ToString());if(image)changed.Descendants(PKG+"binaryData").Single().Value="AQIE";else changed.Descendants(W+"ftr").Single().Descendants(W+"t").Single().Value="Changed footer";
            bool rejected=false;try{SignatureSpaceXmlProof.Verify(package.ToString(),changed.ToString(),nameIndex);}catch(InvalidOperationException){rejected=true;}
            assert(rejected,"The full-package proof also protects image bytes and footer content during the spacing action");
        }
    }
}
