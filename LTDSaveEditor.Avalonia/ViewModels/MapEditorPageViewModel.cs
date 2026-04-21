using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LTDSaveEditor.Avalonia.MapEditor;
using LTDSaveEditor.Core.SAV;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace LTDSaveEditor.Avalonia.ViewModels;

public sealed class MapEditorPageViewModel : ObservableObject
{
    private readonly IMapEditorTool _brushTool = new MapPaintTool();
    private readonly IMapEditorTool _fillTool = new MapFillTool();
    private readonly MapToolContext _toolContext;
    private IMapEditorTool _selectedTool;

    private MapPaletteItem? _selectedPaletteItem;
    private double _zoom = 8;
    private int _hoveredX = -1;
    private int _hoveredY = -1;
    private string _statusText = string.Empty;

    public MapEditorDocument Document { get; }
    public MapEditorHistory History { get; } = new();
    public ObservableCollection<MapPaletteItem> Palette { get; }

    public IRelayCommand UndoCommand { get; }
    public IRelayCommand RedoCommand { get; }
    public IRelayCommand ZoomInCommand { get; }
    public IRelayCommand ZoomOutCommand { get; }
    public IRelayCommand SelectBrushToolCommand { get; }
    public IRelayCommand SelectFillToolCommand { get; }

    public string ToolName => _selectedTool.Name;
    public string ToolIconSymbol => _selectedTool.IconSymbol;
    public bool CanUndo => History.CanUndo;
    public bool CanRedo => History.CanRedo;
    public bool IsBrushToolSelected => ReferenceEquals(_selectedTool, _brushTool);
    public bool IsFillToolSelected => ReferenceEquals(_selectedTool, _fillTool);

    public MapPaletteItem? SelectedPaletteItem
    {
        get => _selectedPaletteItem;
        set
        {
            if (!SetProperty(ref _selectedPaletteItem, value))
                return;

            if (value != null)
                _toolContext.SelectedTileHash = (uint) value.Type;

            OnPropertyChanged(nameof(SelectedTileText));
            UpdateStatusText();
        }
    }

    public double Zoom
    {
        get => _zoom;
        set
        {
            var clamped = Math.Clamp(value, 4, 24);
            if (!SetProperty(ref _zoom, clamped))
                return;

            OnPropertyChanged(nameof(ZoomDisplayText));
        }
    }

    public string ZoomDisplayText => $"{Zoom:0}x";

    public string SelectedTileText => SelectedPaletteItem?.DisplayName ?? "No tile selected";

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public MapEditorPageViewModel(SavFile savFile)
    {
        Document = MapEditorDocument.FromSavFile(savFile);
        _toolContext = new MapToolContext(Document, History);
        _selectedTool = _brushTool;

        Palette = [.. MapPaletteCatalog.CreatePalette(Document.Tiles.Cast<TileType>())];

        SelectedPaletteItem = Palette.FirstOrDefault(item => item.Type == Document.GetTile(0))
            ?? Palette.FirstOrDefault();

        UndoCommand = new RelayCommand(Undo, () => History.CanUndo);
        RedoCommand = new RelayCommand(Redo, () => History.CanRedo);
        ZoomInCommand = new RelayCommand(() => Zoom += 2);
        ZoomOutCommand = new RelayCommand(() => Zoom -= 2);
        SelectBrushToolCommand = new RelayCommand(() => SelectTool(_brushTool));
        SelectFillToolCommand = new RelayCommand(() => SelectTool(_fillTool));

        Document.TilesChanged += Document_TilesChanged;
        History.StateChanged += History_StateChanged;

        UpdateStatusText();
    }

    public void BeginPaintAt(int x, int y)
    {
        if (SelectedPaletteItem == null || !Document.IsInBounds(x, y))
            return;

        _selectedTool.BeginStroke(_toolContext, x, y);
        UpdateStatusText();
    }

    public void ContinuePaintAt(int x, int y)
    {
        if (!Document.IsInBounds(x, y))
            return;

        _selectedTool.ContinueStroke(_toolContext, x, y);
        UpdateStatusText();
    }

    public void EndPaint()
    {
        _selectedTool.EndStroke(_toolContext);
        UpdateStatusText();
    }

    public void PickTileAt(int x, int y)
    {
        if (!Document.IsInBounds(x, y))
            return;

        SelectPaletteByHash(Document.GetTile(x, y));
    }

    public void SetHoverTile(int? x, int? y)
    {
        var nextX = x ?? -1;
        var nextY = y ?? -1;

        if (_hoveredX == nextX && _hoveredY == nextY)
            return;

        _hoveredX = nextX;
        _hoveredY = nextY;
        UpdateStatusText();
    }

    public (int X, int Y)? GetHoveredTile()
    {
        return _hoveredX >= 0 && _hoveredY >= 0
            ? (_hoveredX, _hoveredY)
            : null;
    }

    private void Undo()
    {
        if (History.Undo(Document))
            UpdateStatusText();
    }

    private void Redo()
    {
        if (History.Redo(Document))
            UpdateStatusText();
    }

    private void History_StateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));

        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private void Document_TilesChanged(object? sender, MapTilesChangedEventArgs e)
    {
        foreach (var index in e.Indices)
            EnsurePaletteContains(Document.GetTile(index));

        UpdateStatusText();
    }

    private void SelectPaletteByHash(TileType tileType)
    {
        EnsurePaletteContains(tileType);
        SelectedPaletteItem = Palette.First(item => item.Type == tileType);
    }

    private void EnsurePaletteContains(TileType tileType)
    {
        if (Palette.Any(item => item.Type == tileType))
            return;

        var item = MapPaletteCatalog.CreateItem(tileType);

        Palette.Add(item);
    }

    private void SelectTool(IMapEditorTool tool)
    {
        if (ReferenceEquals(_selectedTool, tool))
            return;

        _selectedTool = tool;
        OnPropertyChanged(nameof(ToolName));
        OnPropertyChanged(nameof(ToolIconSymbol));
        OnPropertyChanged(nameof(IsBrushToolSelected));
        OnPropertyChanged(nameof(IsFillToolSelected));
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {

        if (_hoveredX < 0 || _hoveredY < 0)
        {
            StatusText = $"{ToolName} | Selected: {SelectedTileText}";
            return;
        }

        var tileHash = Document.GetTile(_hoveredX, _hoveredY);
        StatusText = $"{ToolName} | Selected: {SelectedTileText} | Hover: X {_hoveredX}, Y {_hoveredY}, {MapPaletteCatalog.GetDisplayName(tileHash)}";
    }
}
