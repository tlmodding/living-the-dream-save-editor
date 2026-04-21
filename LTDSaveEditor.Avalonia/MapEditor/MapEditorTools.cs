using System;
using System.Collections.Generic;

namespace LTDSaveEditor.Avalonia.MapEditor;

public sealed class MapToolContext(MapEditorDocument document, MapEditorHistory history)
{
    public MapEditorDocument Document { get; } = document;
    public MapEditorHistory History { get; } = history;
    public uint SelectedTileHash { get; set; }
}

public interface IMapEditorTool
{
    string Name { get; }
    string IconSymbol { get; }

    void BeginStroke(MapToolContext context, int x, int y);
    void ContinueStroke(MapToolContext context, int x, int y);
    void EndStroke(MapToolContext context);
}

public sealed class MapPaintTool : IMapEditorTool
{
    private MapEditBatchBuilder? _currentStroke;
    private (int X, int Y)? _lastTile;

    public string Name => "Brush";
    public string IconSymbol => "ColorLine";

    public void BeginStroke(MapToolContext context, int x, int y)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Document.IsInBounds(x, y))
            return;

        _currentStroke = new MapEditBatchBuilder("Paint Tiles");
        _lastTile = (x, y);

        PaintLine(context, x, y, x, y);
    }

    public void ContinueStroke(MapToolContext context, int x, int y)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_currentStroke == null || _lastTile == null || !context.Document.IsInBounds(x, y))
            return;

        var (lastX, lastY) = _lastTile.Value;
        if (lastX == x && lastY == y)
            return;

        PaintLine(context, lastX, lastY, x, y);
        _lastTile = (x, y);
    }

    public void EndStroke(MapToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_currentStroke?.Build() is MapEditAction action)
            context.History.Push(action);

        _currentStroke = null;
        _lastTile = null;
    }

    private void PaintLine(MapToolContext context, int startX, int startY, int endX, int endY)
    {
        if (_currentStroke == null)
            return;

        _currentStroke.Apply(
            context.Document,
            RasterizeLine(context.Document, startX, startY, endX, endY),
            context.SelectedTileHash);
    }

    private static IEnumerable<int> RasterizeLine(MapEditorDocument document, int startX, int startY, int endX, int endY)
    {
        var x = startX;
        var y = startY;
        var deltaX = Math.Abs(endX - startX);
        var deltaY = Math.Abs(endY - startY);
        var stepX = startX < endX ? 1 : -1;
        var stepY = startY < endY ? 1 : -1;
        var error = deltaX - deltaY;

        while (true)
        {
            yield return document.GetIndex(x, y);

            if (x == endX && y == endY)
                yield break;

            var errorTimesTwo = error * 2;

            if (errorTimesTwo > -deltaY)
            {
                error -= deltaY;
                x += stepX;
            }

            if (errorTimesTwo < deltaX)
            {
                error += deltaX;
                y += stepY;
            }
        }
    }
}

public sealed class MapFillTool : IMapEditorTool
{
    public string Name => "Fill";
    public string IconSymbol => "ColorFill";

    public void BeginStroke(MapToolContext context, int x, int y)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Document.IsInBounds(x, y))
            return;

        var sourceTileHash = (uint) context.Document.GetTile(x, y);
        if (sourceTileHash == context.SelectedTileHash)
            return;

        var batch = new MapEditBatchBuilder("Fill Tiles");
        batch.Apply(
            context.Document,
            CollectFillIndices(context.Document, x, y, sourceTileHash),
            context.SelectedTileHash);

        if (batch.Build() is MapEditAction action)
            context.History.Push(action);
    }

    public void ContinueStroke(MapToolContext context, int x, int y)
    {
    }

    public void EndStroke(MapToolContext context)
    {
    }

    private static IReadOnlyList<int> CollectFillIndices(MapEditorDocument document, int startX, int startY, uint sourceTileHash)
    {
        var indices = new List<int>();
        var visited = new bool[document.Tiles.Length];
        var pending = new Queue<int>();
        var startIndex = document.GetIndex(startX, startY);

        visited[startIndex] = true;
        pending.Enqueue(startIndex);

        while (pending.Count > 0)
        {
            var currentIndex = pending.Dequeue();
            var currentValue = (uint) document.GetTile(currentIndex);
            if (currentValue != sourceTileHash)
                continue;

            indices.Add(currentIndex);

            var (x, y) = document.GetCoordinates(currentIndex);
            EnqueueIfValid(x - 1, y);
            EnqueueIfValid(x + 1, y);
            EnqueueIfValid(x, y - 1);
            EnqueueIfValid(x, y + 1);
        }

        return indices;

        void EnqueueIfValid(int x, int y)
        {
            if (!document.IsInBounds(x, y))
                return;

            var index = document.GetIndex(x, y);
            if (visited[index])
                return;

            visited[index] = true;
            pending.Enqueue(index);
        }
    }
}