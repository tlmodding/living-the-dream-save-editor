using LTDSaveEditor.Core.Types;
using System.Text;
using BinaryReader = AeonSake.BinaryTools.BinaryReader;

namespace LTDSaveEditor.Core.SAV;

public class SavFileEntry
{ 
    public uint Hash { get; }
    public object? Value { get; set; }

    public SavFileEntry(uint hash, DataType type, BinaryReader reader)
    {
        Hash = hash;

        if (type.HasOffset())
        {
            uint offset = reader.ReadUInt32();
            uint count = 0;

            if (type.IsArray())
            {
                using (reader.CreateScope())
                    count = reader.ReadUInt32At(offset);

                var singleType = type.ToSingle();
                using (reader.CreateScopeAt(offset + 4))
                {
                    var array = new object[count];
                    for (uint i = 0; i < count; i++)
                        array[i] = ReadValue(reader, singleType);

                    Value = array;
                }
            }
            else 
            {
                using (reader.CreateScopeAt(offset))
                    Value = ReadValue(reader, type);
            }
        }
        else
        {
            Value = ReadValue(reader, type);
        }
    }

    public static object ReadValue(BinaryReader reader, DataType type) => type switch
    {
        DataType.Bool => reader.ReadInt32() == 1,
        DataType.Int => reader.ReadInt32(),
        DataType.Float => reader.ReadSingle(),
        DataType.Enum => reader.ReadUInt32(),
        DataType.Vector2 => new Vector2(reader),
        DataType.Vector3 => new Vector3(reader),
        DataType.String16 => reader.ReadString(16),
        DataType.String32 => reader.ReadString(32),
        DataType.String64 => reader.ReadString(64),
        DataType.Binary => new Binary(reader),
        DataType.UInt => reader.ReadUInt32(),
        DataType.Int64 => reader.ReadInt64(),
        DataType.UInt64 => reader.ReadUInt64(),
        DataType.WString16 => reader.ReadString(16, Encoding.Unicode),
        DataType.WString32 => reader.ReadString(32, Encoding.Unicode),
        DataType.WString64 => reader.ReadString(64, Encoding.Unicode),
        DataType.Bool64bitKey => throw new NotImplementedException(),
        _ => throw new NotImplementedException($"Reading for {type} is not implemented."),
    };
}