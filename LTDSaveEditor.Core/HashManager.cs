using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace LTDSaveEditor.Core;

public class GameData
{
    public uint Number { get; set; }
    public string? Name { get; set; }
    List<string>? Options { get; set; }
}

public class GameDataMap : ClassMap<GameData>
{
    public GameDataMap()
    {
        Map(m => m.Number).Index(1);
        Map(m => m.Name).Index(3);
    }
}

public static class HashManager
{
    public static List<GameData> Hashes { get; private set; } = [];
    public static bool IsInitialized => Hashes.Count > 0;

    public static void Initialize(string hashesCSV)
    {
        Hashes.Clear();

        if (!File.Exists(hashesCSV))
            throw new FileNotFoundException($"The specified file '{hashesCSV}' does not exist.");

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
        };

        using var reader = new StreamReader(hashesCSV);
        using var csv = new CsvReader(reader, config);

        csv.Context.RegisterClassMap<GameDataMap>();

        Hashes = [.. csv.GetRecords<GameData>()];
    }
}
