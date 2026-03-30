using AeonSake.BinaryTools;
using LTDSaveEditor.Core.Extensions;
using LTDSaveEditor.Core.Types;
using System.Text;
using BinaryReader = AeonSake.BinaryTools.BinaryReader;
using BinaryWriter = AeonSake.BinaryTools.BinaryWriter;

namespace LTDSaveEditor.Core.SAV;

public class SavFileEntry
{ 
    public uint Hash { get; }
    public object? Value { get; set; }
    public DataType DataType { get; set; }


    public SavFileEntry(uint hash, DataType type, BinaryReader reader)
    {
        Hash = hash;
        DataType = type;

        if (type.HasOffset())
        {
            uint offset = reader.ReadUInt32();

            if (type == DataType.Bool64bitKey && offset == 0)
                return;

            if (type.IsArray())
            {
                using (reader.CreateScopeAt(offset))
                {
                    uint count = reader.ReadUInt32();

                    if (DataType == DataType.BoolArray)
                    {   
                        var values = new bool[count];

                        var byteSize = (int)Math.Ceiling(Math.Max(4, count / 8d));
                        byte[] bitBytes = reader.ReadByteArray(byteSize);

                        for (int i = 0; i < count; i++)
                        {
                            int byteIndex = i / 8;
                            int bitIndex = i % 8;

                            values[i] = BitHelper.IsSet(bitBytes[byteIndex], bitIndex);
                        }

                        Value = values;
                    } 
                    else
                    {
                        var singleType = type.ToSingle();

                        var array = Array.CreateInstance(singleType.ToType(), count);
                        //var array = new object[count];
                        for (uint i = 0; i < count; i++)
                            array.SetValue(ReadValue(reader, singleType), i);

                        Value = array;
                    }
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
        DataType.WString16 => reader.ReadString(16 * 2, Encoding.Unicode),
        DataType.WString32 => reader.ReadString(32 * 2, Encoding.Unicode),
        DataType.WString64 => reader.ReadString(64 * 2, Encoding.Unicode),
        DataType.Bool64bitKey => null!, // Not present in Tomodachi Life
        _ => type.IsArray() ? throw new Exception($"Tried to read an array in {nameof(ReadValue)}") : throw new NotImplementedException($"Reading for {type} is not implemented."),
    };

    public static void WriteValue(BinaryWriter writer, DataType type, object? value)
    {
        if (value == null)
            throw new Exception("Tried to save a null value!");

        switch (type)
        {
            case DataType.Bool:
                writer.Write((bool)value ? 1 : 0);
                break;
            case DataType.Int:
                writer.Write((int)value);
                break;
            case DataType.Float:
                writer.Write((float)value);
                break;
            case DataType.Enum:
                writer.Write((uint)value);
                break;
            case DataType.Vector2:
                ((Vector2)value).Write(writer);
                break;
            case DataType.Vector3:
                ((Vector3)value).Write(writer);
                break;
            case DataType.String16:
                writer.Write((string)value, 16, Encoding.UTF8);
                break;
            case DataType.String32:
                writer.Write((string)value, 32, Encoding.UTF8);
                break;
            case DataType.String64:
                writer.Write((string)value, 64, Encoding.UTF8);
                break;
            case DataType.Binary:
                ((Binary)value).Write(writer);
                break;
            case DataType.UInt:
                writer.Write((uint)value);
                break;
            case DataType.Int64:
                writer.Write((long)value);
                break;
            case DataType.UInt64:
                writer.Write((ulong)value);
                break;
            case DataType.WString16:
                writer.Write((string)value, 16 * 2, Encoding.Unicode);
                break;
            case DataType.WString32:
                writer.Write((string)value, 32 * 2, Encoding.Unicode);
                break;
            case DataType.WString64:
                writer.Write((string)value, 64 * 2, Encoding.Unicode);
                break;

            default:
                if (type.IsArray())
                    throw new NotImplementedException($"Tried to write an array in {nameof(WriteValue)}");
                else throw new NotImplementedException($"Writing for {type} is not implemented.");
        }
    }

    private WriterScopePointer? scopePointer;
    public void Write(BinaryWriter writer)
    {
        if (DataType.HasOffset())
        {
            scopePointer = writer.CreatePointer();
        }
        else
        {
            WriteValue(writer, DataType, Value);
        }
    }

    internal void ResolvePointer()
    {
        if (DataType == DataType.Bool64bitKey && Value == null) return;

        scopePointer?.Resolve(w =>
        {
            if (DataType.IsArray())
            {
                if (Value is not Array array)
                    throw new Exception($"Expected an array value for hash {Hash:X} ({DataType}) but got {Value?.GetType().Name ?? "null"}");

                w.Write(array.Length);

                if (DataType == DataType.BoolArray)
                {
                    if (Value is not bool[] boolArray)
                        throw new Exception($"Expected a bool[] value for hash {Hash:X} but got {Value?.GetType().Name ?? "null"}");

                    var byteSize = (int)Math.Ceiling(Math.Max(4, array.Length / 8d));
                    byte[] bitBytes = new byte[byteSize];
                    
                    for (int i = 0; i < array.Length; i++)
                    {
                        if (boolArray[i])
                        {
                            int byteIndex = i / 8;
                            int bitIndex = i % 8;

                            bitBytes[byteIndex] = BitHelper.Set(bitBytes[byteIndex], bitIndex);
                        }
                    }

                    w.Write(bitBytes);

                    w.Align(4);

                }
                else
                {
                    foreach (var item in array)
                        WriteValue(w, DataType.ToSingle(), item);
                }
            } else
            {
                WriteValue(w, DataType, Value);
            }
        });
    }
}