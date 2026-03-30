using System.Text;
using BinaryWriter = AeonSake.BinaryTools.BinaryWriter;

namespace LTDSaveEditor.Core.Extensions;

public static class BinaryWriterExtensions
{
    extension(BinaryWriter writer)
    {
        public void Write(string value, int maxLength, Encoding encoding)
        {
            var bytes = encoding.GetBytes(value);
            writer.Write(bytes, 0, Math.Min(bytes.Length, maxLength));

            if (bytes.Length < maxLength)
                writer.Pad(maxLength - bytes.Length);
        }

        public WriterScopePointer CreatePointer()
        {
            var pointer = new WriterScopePointer(writer, writer.Position);
            writer.Pad(4);
            return pointer;
        }

        public WriterScopePointer CreatePointerAt(long position)
        {
            var pointer = new WriterScopePointer(writer, position);
            writer.Pad(4);
            return pointer;
        }
    }
}

public sealed class WriterScopePointer(BinaryWriter writer, long position)
{
    public long Position { get; } = position;

    public void Resolve(uint pos)
    {
        using (writer.CreateScope())
            writer.WriteAt(Position, pos);
    }

    public void Resolve(Action<BinaryWriter> action)
    {
        var pos = (uint) writer.Position;

        action(writer);

        using (writer.CreateScope())
            writer.WriteAt(Position, pos);
    }
}