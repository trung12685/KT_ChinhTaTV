using ChinhTa.Core;

namespace ChinhTa.Desktop;

public sealed partial class WordSession
{
    internal static SectionLayout? ReadOwnedSectionLayout(object ownedSection,int index,Action<object?> release)
        =>WordOwnedObjects.Read(own=>
        {
            dynamic section=own(ownedSection);
            dynamic setup=own((object)section.PageSetup);dynamic range=own((object)section.Range);
            string text=(string)range.Text;if(text.Length==0)return null;
            const double mm=25.4/72;
            return new SectionLayout(index,(int)range.Start,text[..1],(double)setup.PageWidth*mm,(double)setup.PageHeight*mm,
                (double)setup.TopMargin*mm,(double)setup.BottomMargin*mm,(double)setup.LeftMargin*mm,(double)setup.RightMargin*mm,(int)setup.MirrorMargins!=0);
        },release);
}
