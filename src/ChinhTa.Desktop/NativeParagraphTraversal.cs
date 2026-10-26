using System.Collections;
using System.Runtime.InteropServices;
using Microsoft.CSharp.RuntimeBinder;

namespace ChinhTa.Desktop;

public sealed class ParagraphTraversalMeasurements
{
    public bool Sequential { get; internal set; }
    public bool FellBack { get; internal set; }
    public int IndexedReads { get; internal set; }
}

// No document mutations. Discard a partial enumeration before indexed fallback.
// This avoids repeatedly asking Word to locate paragraph i from the collection.
public static class NativeParagraphTraversal
{
    public static IReadOnlyList<T> Read<T>(object collection,int count,Func<int,object> indexed,
        Func<object,int,T> observe,Action<object> release,bool sequential,Func<IReadOnlyList<T>,bool> validate,
        ParagraphTraversalMeasurements measurements)
    {
        if(count<0)throw new InvalidOperationException("Invalid paragraph count");
        if(sequential && count>0)
        {
            try
            {
                var values=new List<T>();IEnumerator? cursor=null;
                try
                {
                    cursor=((IEnumerable)collection).GetEnumerator();
                    while(cursor.MoveNext())
                    {
                        object value=cursor.Current;
                        try
                        {
                            if(values.Count>=count)throw new InvalidOperationException("Paragraph enumeration count changed");
                            values.Add(observe(value,values.Count+1));
                        }
                        finally{release(value);}
                    }
                }
                finally
                {
                    if(cursor is not null)
                    {try{(cursor as IDisposable)?.Dispose();}finally{release(cursor);}}
                }
                if(values.Count!=count || !validate(values))throw new InvalidOperationException("Paragraph enumeration is incomplete or reordered");
                measurements.Sequential=true;return values;
            }
            catch(Exception error) when(error is InvalidCastException or InvalidOperationException or RuntimeBinderException ||
                error is COMException && !ChinhTa.Core.WordConnectionPolicy.IsDisconnected(error))
            {measurements.FellBack=true;}
        }
        var fallback=new List<T>();
        for(int i=1;i<=count;i++)
        {
            object value=indexed(i);measurements.IndexedReads++;
            try{fallback.Add(observe(value,i));}finally{release(value);}
        }
        return fallback;
    }
}
