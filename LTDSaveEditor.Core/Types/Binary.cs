using BinaryReader = AeonSake.BinaryTools.BinaryReader;

namespace LTDSaveEditor.Core.Types;

public class Binary
{
    public byte[] Bytes { get; }
    public Binary(BinaryReader reader)
    {
        var size = reader.ReadInt32();
        Bytes = reader.ReadByteArray(size);

    }

    public void Write(AeonSake.BinaryTools.BinaryWriter writer)
    {
        writer.Write(Bytes.Length);
        writer.Write(Bytes);
    }
}