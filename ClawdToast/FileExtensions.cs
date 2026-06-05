using System.Text;

namespace ClawdToast;

public static class FileExtensions
{
    public static IEnumerable<string> ReadLinesBackward(string filePath, Encoding encoding)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var position = fs.Length;

        var lineBytes = new List<byte>();

        while (position > 0)
        {
            position--;
            fs.Position = position;
            var currentByte = fs.ReadByte();

            if (currentByte == '\n')
            {
                if (lineBytes.Count > 0)
                {
                    lineBytes.Reverse();
                    yield return encoding.GetString(lineBytes.ToArray()).TrimEnd('\r');
                    lineBytes.Clear();
                }
            }
            else
            {
                lineBytes.Add((byte)currentByte);
            }
        }

        if (lineBytes.Count > 0)
        {
            lineBytes.Reverse();
            yield return encoding.GetString([.. lineBytes]).TrimEnd('\r');
        }
    }
}
