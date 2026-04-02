using LTDSaveEditor.Core.Extensions;
using System.Diagnostics.CodeAnalysis;
using BinaryReader = AeonSake.BinaryTools.BinaryReader;
using BinaryWriter = AeonSake.BinaryTools.BinaryWriter;

namespace LTDSaveEditor.Core.SAV;

public class SavFile
{
    public static readonly byte[] Magic = [4, 3, 2, 1];

    public int Version { get; set; }

    public Dictionary<uint, SavFileEntry> Entries = [];
    public string Path { get; set; }

    public SavFile(string path, Stream stream)
    {
        Path = path;

        using var reader = new BinaryReader(stream);

        var magic = reader.ReadByteArray(4);

        if (!Magic.SequenceEqual(magic))
            throw new Exception("Invalid save file format.");

        Version = reader.ReadInt32();
        var saveDataOffset = reader.ReadInt32();

        reader.Align(0x20);

        var currentData = DataType.Bool;
        while (reader.BaseStream.Position < saveDataOffset)
        {
            var hash = reader.ReadUInt32();
            if (hash == 0)
            {
                currentData = (DataType) reader.ReadUInt32();
                continue;
            }

            var entry = new SavFileEntry(hash, currentData, reader);
            Entries.TryAdd(hash, entry);
        }
    }

    public bool TryGetEntry(uint hash, [MaybeNullWhen(false)] out SavFileEntry entry)
    {
        if (Entries.TryGetValue(hash, out var val))
        {
            entry = val;
            return true;
        }

        entry = null;
        return false;
    }


    public bool TryGetValue<T>(string name, [MaybeNullWhen(false)] out T value) where T : struct => TryGetValue(name.ToMurmur(), out value);
    public bool TryGetValue<T>(uint hash, [MaybeNullWhen(false)] out T value) where T : struct
    {
        if (Entries.TryGetValue(hash, out var entry))
        {
            if (entry.Value is not T val)
                throw new Exception("Entry doesn't match expected value type!");

            value = val;
            return true;
        }

        value = default;
        return false;
    }

    public void SetValue<T>(string name, T value) where T : struct => SetValue(name.ToMurmur(), value);
    public void SetValue<T>(uint hash, T value) where T : struct
    {
        if (Entries.TryGetValue(hash, out var entry))
        {
            if (entry.Value is not T)
                throw new Exception("Entry doesn't match expected value type!");

            entry.Value = value;
        }
    }

    public void Save(Stream stream)
    {
        using var writer = new BinaryWriter(stream);

        writer.Write(Magic);
        writer.Write(Version);
        var saveDataOffset = writer.CreatePointer();

        writer.Align(0x20);

        var entries = new List<SavFileEntry>();
        var dataTypes = Enum.GetValues<DataType>();

        foreach(var currentData in dataTypes)
        {
            // Declare the current data type section
            writer.Write(0);
            writer.Write((uint) currentData);

            var values = Entries.Values.Where(e => e.DataType == currentData);

            // Write all entries of the current data type
            foreach (var item in values)
            {
                writer.Write(item.Hash);
                item.Write(writer);
                entries.Add(item);
            }
        }

        saveDataOffset.Resolve((uint) writer.Position);

        foreach (var item in entries)
        {
            item.ResolvePointer();
        }
    }
    
    public void SaveTo(string path)
    {
        using var writeStream = File.Create(path);
            Save(writeStream);
    }
}