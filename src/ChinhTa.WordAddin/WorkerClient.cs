using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using ChinhTa.WordProtocol;
namespace ChinhTa.WordAddin;
internal sealed class WorkerClient:IDisposable
{
    private readonly SemaphoreSlim gate=new SemaphoreSlim(1,1);
    private readonly JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=BridgeContract.MaxFrame,RecursionLimit=16};
    private Process? process;
    private bool disposed;
    internal async Task<BridgeResponse> Send(BridgeRequest request)
    {
        await gate.WaitAsync();
        try
        {
            if(disposed)throw new ObjectDisposedException(nameof(WorkerClient));
            if(process is null || process.HasExited)
            {
                Stop();string root=Path.GetDirectoryName(typeof(Connect).Assembly.Location)!;
                string exe=Path.Combine(root,"worker","ChinhTa.WordWorker.exe");
                if(!File.Exists(exe))throw new InvalidOperationException("Thiếu bộ xử lý đi kèm. Hãy cài lại toàn bộ gói Add-in.");
                process=new Process{StartInfo=new ProcessStartInfo{FileName=exe,Arguments="--stdio",WorkingDirectory=root,UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=new UTF8Encoding(false),StandardErrorEncoding=new UTF8Encoding(false)}};
                process.ErrorDataReceived+=(_,_)=>{};process.Start();process.BeginErrorReadLine();
            }
            request.Id=Guid.NewGuid().ToString();string text=AsciiJson(json.Serialize(request));
            if(text.Length>BridgeContract.MaxFrame)throw new InvalidOperationException("Tài liệu vượt giới hạn bản thử.");
            await process.StandardInput.WriteLineAsync(text);await process.StandardInput.FlushAsync();
            var read=process.StandardOutput.ReadLineAsync();
            if(await Task.WhenAny(read,Task.Delay(30000))!=read){Stop();throw new InvalidOperationException("Bộ xử lý chưa trả kết quả sau 30 giây. Chưa tự gửi lại thao tác; hãy kiểm tra rồi thử lại.");}
            string? line=await read;
            if(line is null || line.Length>BridgeContract.MaxFrame){Stop();throw new InvalidOperationException("Mất kết nối bộ xử lý; chưa xác nhận thao tác đã hoàn tất.");}
            var response=json.Deserialize<BridgeResponse>(line);
            if(response is null || response.Version!=BridgeContract.Version || response.Id!=request.Id || response.DocumentId!=request.DocumentId || response.SnapshotHash!=request.SnapshotHash){Stop();throw new InvalidOperationException("Kết quả không khớp lượt yêu cầu/phiên bản; hãy cài đầy đủ cùng một gói Add-in.");}
            if(!response.Success)throw new InvalidOperationException(response.Error);
            return response;
        }
        finally{gate.Release();}
    }
    internal static string AsciiJson(string value)
    {
        var output=new StringBuilder(value.Length);
        foreach(char c in value)if(c>127)output.Append("\\u").Append(((int)c).ToString("X4"));else output.Append(c);
        return output.ToString();
    }
    private void Stop()
    {
        var child=process;process=null;if(child is null)return;
        try{if(!child.HasExited){child.StandardInput.Close();if(!child.WaitForExit(1000))child.Kill();}}
        catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}
        finally{child.Dispose();}
    }
    public void Dispose(){disposed=true;Stop();}
}
