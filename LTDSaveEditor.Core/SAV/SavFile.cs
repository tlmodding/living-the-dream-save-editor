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

    public bool TryGetValue(uint hash, [MaybeNullWhen(false)] out SavFileEntry entry)
    {
        if (Entries.TryGetValue(hash, out var val))
        {
            entry = val;
            return true;
        }

        entry = null;
        return false;
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