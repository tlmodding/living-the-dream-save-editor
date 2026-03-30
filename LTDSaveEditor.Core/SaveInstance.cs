using LTDSaveEditor.Core.SAV;

namespace LTDSaveEditor.Core;

public class SaveInstance
{
    public static SaveInstance FromFolder(string folder) => new(folder);

    public SavFile Player { get; set; }
    public SavFile Mii { get; set; }
    public SavFile Map { get; set; }

    public string Folder { get; }

    public SaveInstance(string folder)
    {
        if (string.IsNullOrEmpty(folder))
            throw new ArgumentException("Folder cannot be null or empty.", nameof(folder));

        if (!Directory.Exists(folder))
            throw new Exception("Folder does not exist.");

        Folder = folder;

        var playerSave = Path.Combine(folder, "Player.sav");
        var miiSave = Path.Combine(folder, "Mii.sav");
        var mapSave = Path.Combine(folder, "Map.sav");

        using var playerStream = File.OpenRead(playerSave);
        Player = new SavFile(playerSave, playerStream);

        using var miiStream = File.OpenRead(miiSave);
        Mii = new SavFile(playerSave, miiStream);

        using var mapStream = File.OpenRead(mapSave);
        Map = new SavFile(playerSave, mapStream);
    }
}