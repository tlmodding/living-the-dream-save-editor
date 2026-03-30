using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LTDSaveEditor.WinForms.Settings;

public enum EnumDisplayMode
{
    Name,
    Hash,
    Number,
}

public enum EnumFallbackMode
{
    Hash,
    Number,
}


public class UserOptions
{
    private static UserOptions? _instance;
    public static UserOptions Instance => _instance ??= Load();

    [Category("Display")]
    [DisplayName("Enum Display Mode")]
    public EnumDisplayMode EnumDisplayMode { get; set; } = EnumDisplayMode.Name;

    [Category("Display")]
    [DisplayName("Show Flag Type")]
    [Description("If enabled, flag entries will display their type (e.g. 'Flag (UInt)') instead of just the name or hash. (requires restarting)")]
    public bool ShowFlagType { get; set; }

    //[Category("Display")]
    //[DisplayName("Enum Fallback Mode")]
    //public EnumFallbackMode EnumFallbackMode { get; set; } = EnumFallbackMode.Hash;

    [Category("General")]
    [DisplayName("Open Last Save On Startup")]
    [Description("If enabled, the application will attempt to open the last save file used when it starts up.")]
    public bool OpenLastSaveOnStartup { get; set; } = true;

    [Category("General")]
    [DisplayName("Maximum Backups")]
    [Description("The maximum number of backup files to keep when creating backups. Older backups will be deleted when this limit is exceeded.")]
    public int MaximumBackups { get; set; } = 10;

    [JsonInclude]
    internal string LastSaveFolder { get; set; } = string.Empty;

    private static JsonSerializerOptions SerializerOptions { get; } = new()
    {
        WriteIndented = true
    };

    private static string FilePath => "options.json";

    private static UserOptions Load()
    {
        if (!File.Exists(FilePath)) return new UserOptions();
        
        var json = File.ReadAllText(FilePath);
        return  JsonSerializer.Deserialize<UserOptions>(json) ?? new UserOptions();
    }

    internal void Save()
    {
        var json = JsonSerializer.Serialize(this, SerializerOptions);

        File.WriteAllText(FilePath, json);
    }
}