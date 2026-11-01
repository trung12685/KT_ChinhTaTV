using System.Runtime.InteropServices;

// Exercise the unmanaged ABI Office uses, not a managed call on the same interface.
internal static class NativeLifecycleChecks
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int Connection(IntPtr self,IntPtr application,int mode,IntPtr addin,ref IntPtr custom);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int Disconnection(IntPtr self,int mode,ref IntPtr custom);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int Notification(IntPtr self,ref IntPtr custom);
    [DllImport("oleaut32.dll")]private static extern IntPtr SafeArrayCreateVector(ushort type,int lowerBound,uint count);
    [DllImport("oleaut32.dll")]private static extern int SafeArrayDestroy(IntPtr array);

    internal static void Run(object connector,Action<bool,string> assert)
    {
        IntPtr unknown=Marshal.GetIUnknownForObject(connector),connection=IntPtr.Zero;
        var application=new LifecycleApplication();IntPtr dispatch=Marshal.GetIDispatchForObject(application);
        try
        {
            Guid id=new Guid("B65AD801-ABAF-11D0-BB8B-00A0C90F2744");
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(unknown,ref id,out connection));
            IntPtr table=Marshal.ReadIntPtr(connection);
            T Method<T>(int slot) where T:Delegate=>Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(table,slot*IntPtr.Size));
            foreach(int count in new[]{0,1})
            {
                IntPtr custom=SafeArrayCreateVector((ushort)VarEnum.VT_VARIANT,0,(uint)count);
                if(custom==IntPtr.Zero)throw new OutOfMemoryException("SAFEARRAY allocation failed");
                try
                {
                    Console.WriteLine("Calling native OnConnection with SAFEARRAY count="+count);
                    int result=Method<Connection>(7)(connection,dispatch,1,dispatch,ref custom);
                    Console.WriteLine("Native OnConnection SAFEARRAY count="+count+" HRESULT=0x"+result.ToString("X8"));
                    assert(result==0,"Office-style unmanaged OnConnection accepts a VARIANT SAFEARRAY with "+count+" elements");
                    var field=connector.GetType().GetField("application",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                    assert(field is not null && ReferenceEquals(field.GetValue(connector),application),"Native OnConnection reaches the connector body and stores the application");
                    foreach(int slot in new[]{9,10,11})
                    {
                        result=Method<Notification>(slot)(connection,ref custom);
                        assert(result==0,"Office-style unmanaged lifecycle callback slot "+slot+" accepts SAFEARRAY count="+count);
                    }
                    result=Method<Disconnection>(8)(connection,1,ref custom);
                    assert(result==0,"Office-style unmanaged OnDisconnection accepts SAFEARRAY count="+count);
                    assert(field!.GetValue(connector) is null,"Native shutdown clears the connector application reference");
                }
                finally{if(custom!=IntPtr.Zero)Marshal.ThrowExceptionForHR(SafeArrayDestroy(custom));}
            }
        }
        finally{if(connection!=IntPtr.Zero)Marshal.Release(connection);Marshal.Release(dispatch);Marshal.Release(unknown);GC.KeepAlive(application);}
    }
}

[ComVisible(true),ClassInterface(ClassInterfaceType.AutoDispatch)]
public sealed class LifecycleApplication { public string Name=>"First-party COM lifecycle fixture"; }
