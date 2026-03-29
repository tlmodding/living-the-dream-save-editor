using BinaryReader = AeonSake.BinaryTools.BinaryReader;

namespace LTDSaveEditor.Core.Types;

public record Vector3(float X, float Y, float Z)
{
    public Vector3(BinaryReader reader) : this(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())
    { }
}
