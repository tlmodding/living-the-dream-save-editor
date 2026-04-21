using LTDSaveEditor.Core.SAV;
using System;
using System.Collections.Generic;

namespace LTDSaveEditor.Avalonia.MapEditor;

public sealed class MapTilesChangedEventArgs(IReadOnlyList<int> indices) : EventArgs
{
    public IReadOnlyList<int> Indices { get; } = indices;
}

public sealed class MapEditorDocument
{
    public const int DefaultWidth = 120;
    public const int DefaultHeight = 80;

    private const string FloorKeyHashPath = "MapGrid.GridX.GridZ.FloorKeyHash";

    public uint[] Tiles { get; }
    public int Width { get; }
    public int Height { get; }

    public event EventHandler<MapTilesChangedEventArgs>? TilesChanged;

    public MapEditorDocument(uint[] tiles, int width = DefaultWidth, int height = DefaultHeight)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        if (tiles.Length != width * height)
            throw new ArgumentException($"Expected {width * height} tiles but got {tiles.Length}.", nameof(tiles));

        Tiles = tiles;
        Width = width;
        Height = height;
    }

    public static MapEditorDocument FromSavFile(SavFile savFile, int width = DefaultWidth, int height = DefaultHeight)
    {
        ArgumentNullException.ThrowIfNull(savFile);

        if (!savFile.TryGetValue<uint[]>(FloorKeyHashPath, out var columnMajorMap) || columnMajorMap == null)
            throw new InvalidOperationException($"Map save does not contain '{FloorKeyHashPath}'.");

        return new MapEditorDocument(columnMajorMap, width, height);
    }

    public bool IsInBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    public int GetIndex(int x, int y)
    {
        if (!IsInBounds(x, y))
            throw new ArgumentOutOfRangeException($"Tile coordinate ({x}, {y}) is outside the map bounds.");

        return (x * Height) + y;
    }

    public (int X, int Y) GetCoordinates(int index)
    {
        if ((uint)index >= (uint)Tiles.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        var x = index / Height;
        var y = index % Height;
        return (x, y);
    }

    public TileType GetTile(int x, int y) => (TileType) Tiles[GetIndex(x, y)];

    public TileType GetTile(int index)
    {
        if ((uint)index >= (uint)Tiles.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        return (TileType) Tiles[index];
    }

    internal bool SetTile(int index, uint tileHash, List<int>? changedIndices = null)
    {
        if ((uint)index >= (uint)Tiles.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (Tiles[index] == tileHash)
            return false;

        Tiles[index] = tileHash;
        changedIndices?.Add(index);
        return true;
    }

    internal void NotifyTilesChanged(IReadOnlyList<int> changedIndices)
    {
        if (changedIndices.Count == 0)
            return;

        TilesChanged?.Invoke(this, new MapTilesChangedEventArgs(changedIndices));
    }

    internal void ApplyChanges(IReadOnlyList<MapTileChange> changes, bool useNewValues)
    {
        if (changes.Count == 0)
            return;

        var dirtyIndices = new List<int>(changes.Count);

        foreach (var change in changes)
        {
            var value = useNewValues ? change.NewValue : change.OldValue;
            SetTile(change.Index, value, dirtyIndices);
        }

        NotifyTilesChanged(dirtyIndices);
    }
}