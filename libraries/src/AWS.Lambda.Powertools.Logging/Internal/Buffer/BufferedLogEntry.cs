
namespace AWS.Lambda.Powertools.Logging.Internal;

internal class BufferedLogEntry
{
    public string Entry { get; }
    public int Size { get; }

    public BufferedLogEntry(string entry, int calculatedSize)
    {
        Entry = entry;
        Size = calculatedSize;
    }
}