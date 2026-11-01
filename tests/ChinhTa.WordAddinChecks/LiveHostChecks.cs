using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ChinhTa.WordAddin;
using ChinhTa.WordProtocol;
internal static class LiveHostChecks
{
    internal static void Run(Action<bool,string> assert)
    {
        var epoch=new LiveReviewEpoch();epoch.Observe("document A:paragraph old");long version=epoch.Version;
        assert(epoch.Accept(version,"document A:paragraph old"),"An unchanged active paragraph can accept its own live result");
        epoch.Observe("document A:paragraph changed");epoch.Observe("document A:paragraph old");
        assert(!epoch.Accept(version,"document A:paragraph old"),"Editing and returning to identical text cannot resurrect an older live result");
        version=epoch.Version;epoch.Observe("document B:paragraph old");assert(!epoch.Accept(version,"document A:paragraph old"),"Another document with the same paragraph cannot inherit its live underline");
        version=epoch.Version;epoch.Clear();assert(!epoch.Accept(version,"document B:paragraph old"),"Disabling the live checker invalidates pending results");
        assert(UnderlinePalette.ColorFor("Spelling")==Color.Firebrick && UnderlinePalette.ColorFor("Context")==Color.RoyalBlue && UnderlinePalette.ColorFor("Format")==Color.DarkGoldenrod,"Spelling, including reviewed phrase errors, maps to red; contextual advice blue; format hints gold");
        using(var overlay=new UnderlineOverlay())
        {
            var parameters=(CreateParams)typeof(UnderlineOverlay).GetProperty("CreateParams",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(overlay)!;
            assert((parameters.ExStyle&UnderlineOverlay.ExtendedFlags)==UnderlineOverlay.ExtendedFlags && (parameters.ExStyle&8)==0,"The underline surface is click-through, nonactivating and not a global topmost window");
            overlay.Present(IntPtr.Zero,new[]{new UnderlineMark(new Rectangle(1,1,10,12),Color.Red)});
            assert(!overlay.Visible,"An unrelated or unavailable foreground Word window cannot receive a visible overlay");
        }
        var document=new FakeDocument("nghành.\r");var window=new FakeWindow(document);
        using(var active=WordActiveParagraph.Capture(window))
        {
            var marks=active.Measure(window,new[]{new FindingDto{ParagraphIndex=1,Offset=0,Length=6,Original="nghành",Category="Spelling"}});
            assert(marks.Count==6 && marks.All(m=>m.Color==Color.Firebrick) && document.SelectedStart==-1 && document.Text=="nghành.\r","Live measurement proves native characters and obtains screen boxes without selecting or writing Word text");
            using(var other=WordActiveParagraph.Capture(new FakeWindow(new FakeDocument(document.Text))))
                assert(active.Key!=other.Key,"Live paragraph keys include document COM identity, not text alone");
        }
        document.HasFields=true;bool rejected=false;try{using var ignored=WordActiveParagraph.Capture(window);}catch(InvalidOperationException){rejected=true;}
        assert(rejected,"Live underlines skip Word fields before measuring a guessed text range");
        assert(!WordFormatSnapshot.Read(new object(),false).Observed,"Failed aggregate COM formatting reads remain unobserved");
        var app=new DictionaryApplication();var original=app.CustomDictionaries.ActiveCustomDictionary;
        WordNativeProofing.Register(app,"C:\\ChinhTaViet\\Vietnamese.dic",true);
        assert(ReferenceEquals(original,app.CustomDictionaries.ActiveCustomDictionary) && app.CustomDictionaries.Count==2 && app.CustomDictionaries[2].LanguageID==1066,"Registering Vietnamese.dic preserves the user's active dictionary and restricts the added dictionary to Vietnamese");
        WordNativeProofing.Register(app,"C:\\ChinhTaViet\\Vietnamese.dic",true);
        assert(app.CustomDictionaries.Count==2,"Repeated native dictionary setup does not add duplicate entries");
    }
}
public sealed class DictionaryApplication{public DictionaryCollection CustomDictionaries {get;}=new();}
public sealed class DictionaryCollection
{
    private readonly List<DictionaryEntry> values=new(){new("C:\\Existing\\CUSTOM.DIC")};
    public DictionaryEntry ActiveCustomDictionary {get;set;}
    public DictionaryCollection(){ActiveCustomDictionary=values[0];}
    public int Count=>values.Count;
    public DictionaryEntry this[int index]=>values[index-1];
    public DictionaryEntry Add(string path){var entry=new DictionaryEntry(path);values.Add(entry);ActiveCustomDictionary=entry;return entry;}
}
public sealed class DictionaryEntry(string fullPath)
{
    public string Path=>System.IO.Path.GetDirectoryName(fullPath)!;
    public string Name=>System.IO.Path.GetFileName(fullPath);
    public bool LanguageSpecific {get;set;}
    public int LanguageID {get;set;}
}
