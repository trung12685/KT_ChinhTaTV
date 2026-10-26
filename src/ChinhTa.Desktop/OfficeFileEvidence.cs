using System.IO;
using System.Security.Cryptography;

namespace ChinhTa.Desktop;

internal static class OfficeFileEvidence
{
    // Word owns an existing writable handle. File.ReadAllBytes shares only
    // reads, which conflicts with that handle on Windows (0x80070020).
    // This handle remains read-only and releases before the next operation.
    public static byte[] Digest(string path)
    {
        using var input=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
        return SHA256.HashData(input);
    }
}
