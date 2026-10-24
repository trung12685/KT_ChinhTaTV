using System.Runtime.InteropServices;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class HeadingSelectiveSizeWriterChecks
{
    public static void Run(Action<bool,string> assert)
    {
        foreach(float min in new[]{12f,13f})
        {
            float max=min+1;
            var first=new SelectiveSizeWriterChecks.Range(10,11,"A",new(min-2));
            var blank=new SelectiveSizeWriterChecks.Range(11,12," ",new(8));
            var second=new SelectiveSizeWriterChecks.Range(12,13,"B",new(min));
            var third=new SelectiveSizeWriterChecks.Range(13,14,"C",new(max));
            var root=new SelectiveSizeWriterChecks.Range(10,14,"A BC",new(9999999),[first,blank,second,third]);
            var style=new HeadingFormat(min,1,max,true);
            VisibleFormatReader.RepairSizes(root,"A BC",style);
            assert(first.Font.Size==min && first.Font.Writes==1 && second.Font.Size==min && third.Font.Size==max &&
                second.Font.Writes==0 && third.Font.Writes==0 && blank.Font.Writes==0 && blank.Font.Size==8,
                "National and motto selective writers share the proven actual-character route and retain all already legal sizes and spaces");
            VisibleFormatReader.RepairSizes(root,"A BC",style);
            assert(first.Font.Writes==1,"A normalized heading does not receive duplicate per-character size writes");
            bool stale=false;try{VisibleFormatReader.RepairSizes(root,"Khác",style);}catch(InvalidOperationException){stale=true;}
            assert(stale,"Selective heading writes bind to the current exact range text");
            third.Font.Size=9999999;int before=first.Font.Writes;bool failed=false;
            try{VisibleFormatReader.RepairSizes(root,"A BC",style);}catch(InvalidOperationException){failed=true;}
            assert(failed && first.Font.Writes==before,"A late unreadable heading glyph prevents every size write in the approved region");
            third.Font.Size=max;first.Font.Size=min-2;third.Font.Size=max+1;third.Font.Fail=true;
            bool restored=false,propagated=false;
            var kind=min==12?FixKind.NationalHeading:FixKind.StateMotto;
            var issue=new Issue(10,4,"A BC","A BC","",false,kind,Format:style);
            try
            {
                AtomicTextBatch.Apply([issue],i=>"A BC",i=>{VisibleFormatReader.RepairSizes(root,"A BC",style);return "A BC";},
                    ()=>throw new Exception("must not commit"),()=>{first.Font.Size=min-2;third.Font.Fail=false;third.Font.Size=max+1;restored=true;});
            }
            catch(COMException){propagated=true;}
            assert(restored && propagated && first.Font.Size==min-2 && third.Font.Size==max+1,
                "A late heading size setter failure reaches the same whole-batch rollback as other formatting failures");
        }
        foreach(var style in new[]{new HeadingFormat(12,1,13),new HeadingFormat(12,1,null,true),
            new HeadingFormat(12,1,14,true),new HeadingFormat(14,1,15,true),new HeadingFormat(float.NaN,1,13,true)})
        {
            var glyph=new SelectiveSizeWriterChecks.Range(0,1,"A",new(10));
            var root=new SelectiveSizeWriterChecks.Range(0,1,"A",new(10),[glyph]);bool rejected=false;
            try{VisibleFormatReader.RepairSizes(root,"A",style);}catch(InvalidOperationException){rejected=true;}
            assert(rejected && glyph.Font.Writes==0,"The Word heading writer rejects non-selective, missing and unsupported size intervals before writing");
        }
    }
}
