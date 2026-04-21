using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LTDSaveEditor.Avalonia.MapEditor;

public sealed class MapPaletteItem(TileType tileHash, string name)
{
    public TileType Type { get; } = tileHash;
    public string Name { get; } = name;
    public SolidColorBrush BrushColor { get; } = new(MapRender.GetTileColor(tileHash));
    public string DisplayName => MapPaletteCatalog.GetDisplayName(Type);
}

public static class MapPaletteCatalog
{
    public static IReadOnlyList<MapPaletteItem> CreatePalette(IEnumerable<TileType> existingTiles)
    {
        ArgumentNullException.ThrowIfNull(existingTiles);

        var tileHashes = Enum.GetValues<TileType>().ToHashSet();
        tileHashes.UnionWith(existingTiles);

        return [.. tileHashes
            .Select(CreateItem)
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Type)];
    }

    public static MapPaletteItem CreateItem(TileType tileHash)
    {
        var name = Enum.GetName(tileHash);
        return new MapPaletteItem(tileHash, name ?? "Unknown");
    }

    public static string GetDisplayName(TileType tileHash) => Enum.GetName(tileHash) ?? $"0x{tileHash:X}";
    
}