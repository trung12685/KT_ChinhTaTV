using System.Diagnostics;
using System.Globalization;

namespace ChinhTa.Desktop;

// Fixed stage names and numeric counts only; never document text or filenames.
internal sealed class WordOperationTiming(string operation):IDisposable
{
    private readonly Stopwatch clock=Stopwatch.StartNew();
    private readonly List<string> values=[];
    private double previous;
    public void Mark(string stage)
    {
        double now=clock.Elapsed.TotalMilliseconds;
        values.Add(stage+"Ms="+(now-previous).ToString("F3",CultureInfo.InvariantCulture));previous=now;
    }
    public void Count(string name,int count)=>values.Add(name+"="+count.ToString(CultureInfo.InvariantCulture));
    public void Dispose()=>Diagnostics.Write(operation,null,string.Join("; ",values)+
        "; TotalMs="+clock.Elapsed.TotalMilliseconds.ToString("F3",CultureInfo.InvariantCulture));
}
