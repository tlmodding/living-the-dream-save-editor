using LTDSaveEditor.Core.SAV;
using LTDSaveEditor.Core.Types;

namespace LTDSaveEditor.Core.Extensions;

public static class DataTypeExtensions
{
    extension(DataType type)
    {
        public DataType ToArray() => type switch
        {
            DataType.Bool => DataType.BoolArray,
            DataType.Int => DataType.IntArray,
            DataType.Float => DataType.FloatArray,
            DataType.Enum => DataType.EnumArray,
            DataType.Vector2 => DataType.Vector2Array,
            DataType.Vector3 => DataType.Vector3Array,
            DataType.String16 => DataType.String16Array,
            DataType.String32 => DataType.String32Array,
            DataType.String64 => DataType.String64Array,
            DataType.Binary => DataType.BinaryArray,
            DataType.UInt => DataType.UIntArray,
            DataType.Int64 => DataType.Int64Array,
            DataType.UInt64 => DataType.UInt64Array,
            DataType.WString16 => DataType.WString16Array,
            DataType.WString32 => DataType.WString32Array,
            DataType.WString64 => DataType.WString64Array,
            _ => throw new NotImplementedException($"Reading for {type} is not implemented."),
        };

        public DataType ToSingle() => type switch
        {
            DataType.BoolArray => DataType.Bool,
            DataType.IntArray => DataType.Int,
            DataType.FloatArray => DataType.Float,
            DataType.EnumArray => DataType.Enum,
            DataType.Vector2Array => DataType.Vector2,
            DataType.Vector3Array => DataType.Vector3,
            DataType.String16Array => DataType.String16,
            DataType.String32Array => DataType.String32,
            DataType.String64Array => DataType.String64,
            DataType.BinaryArray => DataType.Binary,
            DataType.UIntArray => DataType.UInt,
            DataType.Int64Array => DataType.Int64,
            DataType.UInt64Array => DataType.UInt64,
            DataType.WString16Array => DataType.WString16,
            DataType.WString32Array => DataType.WString32,
            DataType.WString64Array => DataType.WString64,
            _ => throw new NotImplementedException($"Conversion from {type} is not implemented."),
        };

        public bool IsArray() => type switch
        {
            DataType.BoolArray => true,
            DataType.IntArray => true,
            DataType.FloatArray => true,
            DataType.EnumArray => true,
            DataType.Vector2Array => true,
            DataType.Vector3Array => true,
            DataType.String16Array => true,
            DataType.String32Array => true,
            DataType.String64Array => true,
            DataType.BinaryArray => true,
            DataType.UIntArray => true,
            DataType.Int64Array => true,
            DataType.UInt64Array => true,
            DataType.WString16Array => true,
            DataType.WString32Array => true,
            DataType.WString64Array => true,
            _ => false,
        };

        public bool HasOffset() => type switch
        {
            DataType.Vector2 => true,
            DataType.Vector3 => true,
            DataType.String16 => true,
            DataType.String32 => true,
            DataType.String64 => true,
            DataType.Binary => true,
            DataType.Int64 => true,
            DataType.UInt64 => true,
            DataType.WString16 => true,
            DataType.WString32 => true,
            DataType.WString64 => true,
            DataType.Bool64bitKey => true,
            _ => type.IsArray(),
        };

        public Type ToType() => type switch
        {
            DataType.Bool => typeof(bool),
            DataType.Int => typeof(int),
            DataType.Float => typeof(float),
            DataType.Enum => typeof(uint),
            DataType.Vector2 => typeof(Vector2),
            DataType.Vector3 => typeof(Vector3),
            DataType.String16 => typeof(string),
            DataType.String32 => typeof(string),
            DataType.String64 => typeof(string),
            DataType.Binary => typeof(Binary),
            DataType.UInt => typeof(uint),
            DataType.Int64 => typeof(long),
            DataType.UInt64 => typeof(ulong),
            DataType.WString16 => typeof(string),
            DataType.WString32 => typeof(string),
            DataType.WString64 => typeof(string),
            _ => throw new NotImplementedException($"Conversion from {type} is not implemented."),
        };
    }
}
