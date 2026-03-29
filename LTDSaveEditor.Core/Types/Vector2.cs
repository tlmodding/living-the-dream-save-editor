using BinaryReader = AeonSake.BinaryTools.BinaryReader;

namespace LTDSaveEditor.Core.Types;

public record Vector2(float X, float Y)
{
    public Vector2(BinaryReader reader) : this(reader.ReadSingle(), reader.ReadSingle())
    { }

    public void Write(AeonSake.BinaryTools.BinaryWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
    }
}
