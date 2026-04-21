using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LTDSaveEditor.Avalonia.MapEditor;

public class MapRender
{
    public static Color UnknownTileColor => Colors.Magenta;

    public static Dictionary<TileType, Color> TileColors { get; } = new()
    {
        [TileType.Archstone] = Color.Parse("#e0dfdc"),
        [TileType.Archstone_Road] = Color.Parse("#d6d5d2"),
        [TileType.Asphalt] = Color.Parse("#393c40"),
        [TileType.Asphalt_Road] = Color.Parse("#303236"),
        [TileType.Beach] = Color.Parse("#F2D8A3"),
        [TileType.CherryBlossom] = Color.Parse("#e0c5d3"),
        [TileType.CherryBlossom_Road] = Color.Parse("#c9b1be"),
        [TileType.Clover] = Color.Parse("#7D943C"),
        [TileType.Clover_Road] = Color.Parse("#708535"),
        [TileType.Cobblestone] = Color.Parse("#B7C2C4"),
        [TileType.Cobblestone_Road] = Color.Parse("#A9B6B8"),
        [TileType.Concrete] = Color.Parse("#A1A1A1"),
        [TileType.Concrete_Road] = Color.Parse("#4F4F4F"),
        [TileType.FallenLeaves] = Color.Parse("#E07B28"),
        [TileType.FallenLeaves_Road] = Color.Parse("#CC6B1D"),
        [TileType.Gold] = Color.Parse("#E8C341"),
        [TileType.Gold_Road] = Color.Parse("#E8AE41"),
        [TileType.Grass] = Color.Parse("#62733B"),
        [TileType.Grass_Road] = Color.Parse("#515E2D"),
        [TileType.Iron] = Color.Parse("#CACDCF"),
        [TileType.Iron_Road] = Color.Parse("#B8BCBF"),
        [TileType.Pebble] = Color.Parse("#B8A681"),
        [TileType.Pebble_Road] = Color.Parse("#997151"),
        [TileType.RoomInvalid] = Colors.Magenta,
        [TileType.Sand] = Color.Parse("#C9A05D"),
        [TileType.Sand_Road] = Color.Parse("#B58D4C"),
        [TileType.Seaside] = Color.Parse("#6A9B7A"),
        [TileType.Snow] = Color.Parse("#E6EDEC"),
        [TileType.Snow_Road] = Color.Parse("#D3DEDD"),
        [TileType.Soil] = Color.Parse("#C68B46"),
        [TileType.Soil_Road] = Color.Parse("#B07A3C"),
        [TileType.Stone] = Color.Parse("#C97038"),
        [TileType.Stone_Road] = Color.Parse("#B35E2B"),
        [TileType.Tile] = Color.Parse("#25A0C2"),
        [TileType.Tile_Road] = Color.Parse("#1E90B0"),
        [TileType.UGC] = Color.Parse("#ED2424"),
        [TileType.Water] = Color.Parse("#3A6A85"),
        [TileType.Wood] = Color.Parse("#966120"),
        [TileType.Wood_Road] = Color.Parse("#7A4C14"),
    };

    public static Color GetTileColor(uint tileHash) => GetTileColor((TileType)tileHash);

    public static Color GetTileColor(TileType tileType) => TileColors.GetValueOrDefault(tileType, UnknownTileColor);
    
    public static WriteableBitmap CreateMapBitmap(uint[] columnMajorMap, int width = 120, int height = 80)
    {
        var bmp = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        UpdateMapBitmap(bmp, columnMajorMap, width, height);
        return bmp;
    }

    public static void UpdateMapBitmap(WriteableBitmap bitmap, uint[] columnMajorMap, int width = 120, int height = 80, IReadOnlyList<int>? dirtyIndices = null)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentNullException.ThrowIfNull(columnMajorMap);

        if (bitmap.PixelSize.Width != width || bitmap.PixelSize.Height != height)
            throw new ArgumentException("Bitmap dimensions do not match the requested map size.", nameof(bitmap));

        if (columnMajorMap.Length != width * height)
            throw new ArgumentException($"Expected {width * height} tiles but got {columnMajorMap.Length}.", nameof(columnMajorMap));


        using var fb = bitmap.Lock();

        if (dirtyIndices == null)
        {
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var idx = (x * height) + y;
                    WriteTile(fb.Address, fb.RowBytes, x, y, 1, PackColor(GetTileColor(columnMajorMap[idx])));
                }
            }
        }
        else
        {
            foreach (var index in dirtyIndices)
            {
                if ((uint)index >= (uint)columnMajorMap.Length)
                    continue;

                var x = index / height;
                var y = index % height;
                WriteTile(fb.Address, fb.RowBytes, x, y, 1, PackColor(GetTileColor(columnMajorMap[index])));
            }
        }
    }

    private static void WriteTile(IntPtr address, int rowBytes, int x, int y, int tileSize, int packedColor)
    {
        var startX = x * tileSize;
        var startY = y * tileSize;

        for (var py = 0; py < tileSize; py++)
        {
            var rowOffset = (startY + py) * rowBytes;

            for (var px = 0; px < tileSize; px++)
            {
                Marshal.WriteInt32(address, rowOffset + ((startX + px) * 4), packedColor);
            }
        }
    }

    private static int PackColor(Color color)
    {
        return color.B
            | (color.G << 8)
            | (color.R << 16)
            | (color.A << 24);
    }
}