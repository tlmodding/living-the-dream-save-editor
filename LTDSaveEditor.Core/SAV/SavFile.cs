using BinaryReader = AeonSake.BinaryTools.BinaryReader;

namespace LTDSaveEditor.Core.SAV;

public class SavFile
{
    public static byte[] Magic = { 1, 2, 3, 4 };

    public int Version { get; set; }
    public int SaveDataOffset { get; set; }

    public List<SavFileEntry> Entries = [];

    public SavFile(BinaryReader reader)
    {
        var magic = reader.ReadByteArray(4);

        if (!Magic.SequenceEqual(magic))
            throw new Exception("Invalid save file format.");

        Version = reader.ReadInt32();
        SaveDataOffset = reader.ReadInt32();

        reader.Align(0x20);

        var currentData = DataType.Bool;
        while (reader.BaseStream.Position < SaveDataOffset)
        {
            var hash = reader.ReadUInt32();
            if (hash == 0)
            {
                currentData = (DataType) reader.ReadUInt32();
                continue;
            }

            Entries.Add(new SavFileEntry(hash, currentData, reader));
        }
    }
}
