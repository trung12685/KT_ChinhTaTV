namespace ChinhTa.Core;

public static class WordConnectionPolicy
{
    public static bool IsDisconnected(Exception error) => unchecked((uint)error.HResult) is
        0x800706BA or 0x80010108 or 0x800401FD or 0x80010007 or 0x80010012;

    // Only read operations may be replayed. Never use this around text replacement or Undo.
    public static T ReadWithRecovery<T>(Func<T> read, Action recover)
    {
        try { return read(); }
        catch (Exception error) when (IsDisconnected(error))
        {
            recover();
            return read(); // One retry, then preserve the actual error for diagnosis.
        }
    }
}
