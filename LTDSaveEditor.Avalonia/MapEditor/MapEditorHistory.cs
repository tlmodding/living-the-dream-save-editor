using System;
using System.Collections.Generic;
using System.Linq;

namespace LTDSaveEditor.Avalonia.MapEditor;

public readonly record struct MapTileChange(int Index, uint OldValue, uint NewValue);

public sealed class MapEditAction(string name, IReadOnlyList<MapTileChange> changes)
{
    public string Name { get; } = name;
    public IReadOnlyList<MapTileChange> Changes { get; } = changes;

    public void Undo(MapEditorDocument document) => document.ApplyChanges(Changes, useNewValues: false);

    public void Redo(MapEditorDocument document) => document.ApplyChanges(Changes, useNewValues: true);
}

public sealed class MapEditBatchBuilder(string name)
{
    private readonly Dictionary<int, MapTileChange> _changes = [];

    public string Name { get; } = string.IsNullOrWhiteSpace(name) ? "Edit Map" : name;

    public int ChangeCount => _changes.Count;

    public int Apply(MapEditorDocument document, IEnumerable<int> indices, uint newValue)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(indices);

        var dirtyIndices = new List<int>();
        var appliedCount = 0;

        foreach (var index in indices)
        {
            if (ApplyCore(document, index, newValue, dirtyIndices))
                appliedCount++;
        }

        document.NotifyTilesChanged(dirtyIndices);
        return appliedCount;
    }

    public MapEditAction? Build()
    {
        if (_changes.Count == 0)
            return null;

        var orderedChanges = _changes.Values
            .OrderBy(change => change.Index)
            .ToArray();

        return new MapEditAction(Name, orderedChanges);
    }

    private bool ApplyCore(MapEditorDocument document, int index, uint newValue, List<int> dirtyIndices)
    {
        if (_changes.TryGetValue(index, out var existing))
        {
            if (existing.NewValue == newValue)
                return false;

            if (!document.SetTile(index, newValue, dirtyIndices))
                return false;

            if (existing.OldValue == newValue)
                _changes.Remove(index);
            else
                _changes[index] = existing with { NewValue = newValue };

            return true;
        }

        var oldValue = (uint) document.GetTile(index);
        if (oldValue == newValue)
            return false;

        if (!document.SetTile(index, newValue, dirtyIndices))
            return false;

        _changes[index] = new MapTileChange(index, oldValue, newValue);
        return true;
    }
}

public sealed class MapEditorHistory
{
    private readonly Stack<MapEditAction> _undoStack = [];
    private readonly Stack<MapEditAction> _redoStack = [];

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public event EventHandler? StateChanged;

    public void Push(MapEditAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        _undoStack.Push(action);
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool Undo(MapEditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (_undoStack.Count == 0)
            return false;

        var action = _undoStack.Pop();
        action.Undo(document);
        _redoStack.Push(action);

        StateChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool Redo(MapEditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (_redoStack.Count == 0)
            return false;

        var action = _redoStack.Pop();
        action.Redo(document);
        _undoStack.Push(action);

        StateChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void Clear()
    {
        if (_undoStack.Count == 0 && _redoStack.Count == 0)
            return;

        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}