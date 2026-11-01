using ChinhTa.WordProtocol;
namespace ChinhTa.WordAddin;
internal static class WordFormatSnapshot
{
    internal static FormatDto Read(object range,bool table)
    {
        var f=new FormatDto();object? font=null,style=null,cells=null,cell=null,cellRange=null,tables=null,ownerTable=null,tableRange=null;
        try
        {
            dynamic r=range;f.Page=(int)r.Information[3];style=r.ParagraphFormat;dynamic s=style;
            f.Alignment=(int)s.Alignment;f.UnindentedCenter=f.Alignment==1 && Math.Abs((float)s.LeftIndent)<.1 && Math.Abs((float)s.RightIndent)<.1 && Math.Abs((float)s.FirstLineIndent)<.1;
            f.LayoutKnown=f.Page>0 && f.Alignment>=0 && f.Alignment<=9;
            font=r.Font;dynamic value=font;f.Font=(string?)value.Name??"";f.Size=(float)value.Size;f.Bold=(int)value.Bold;f.Italic=(int)value.Italic;f.Color=(int)value.Color;
            f.Observed=f.Font.Length>0 && f.Size>0 && f.Size<=1638 && f.Bold is 0 or -1 && f.Italic is 0 or -1 && f.Color is not (9999999 or -9999999);
            if(table)
            {
                cells=r.Cells;dynamic cs=cells;cell=cs[1];dynamic c=cell;cellRange=c.Range;dynamic cr=cellRange;f.CellStart=(int)cr.Start;f.CellEnd=(int)cr.End;
                tables=r.Tables;dynamic ts=tables;ownerTable=ts[1];dynamic t=ownerTable;tableRange=t.Range;dynamic tr=tableRange;f.TableStart=(int)tr.Start;
            }
        }
        catch(Exception){/* Unknown observations remain unknown; no guessed formatting. */}
        finally{foreach(object? value in new[]{tableRange,ownerTable,tables,cellRange,cell,cells,font,style})ComLifetime.Release(value);}
        return f;
    }
    internal static List<SectionDto> Sections(object doc)
    {
        var result=new List<SectionDto>();object? sections=null;
        try
        {
            dynamic d=doc;sections=d.Sections;dynamic ss=sections;int count=(int)ss.Count;if(count>128)return result;
            for(int i=1;i<=count;i++)
            {
                object? section=null,range=null,setup=null;
                try
                {
                    section=ss[i];dynamic s=section;range=s.Range;setup=s.PageSetup;dynamic r=range,p=setup;
                    result.Add(new(){Number=i,Start=(int)r.Start,End=(int)r.End,WidthMm=(double)p.PageWidth*25.4/72,HeightMm=(double)p.PageHeight*25.4/72,
                        TopMm=(double)p.TopMargin*25.4/72,BottomMm=(double)p.BottomMargin*25.4/72,LeftMm=(double)p.LeftMargin*25.4/72,RightMm=(double)p.RightMargin*25.4/72,MirrorMargins=(int)p.MirrorMargins!=0});
                }
                catch(Exception){}
                finally{ComLifetime.Release(setup);ComLifetime.Release(range);ComLifetime.Release(section);}
            }
        }
        catch(Exception){}
        finally{ComLifetime.Release(sections);}
        return result;
    }
}
