using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using LTDSaveEditor.Avalonia.MapEditor;
using LTDSaveEditor.Avalonia.ViewModels;
using System;

namespace LTDSaveEditor.Avalonia.Views;

public sealed class MapEditorCanvas : Control
{
    public static readonly StyledProperty<MapEditorPageViewModel?> ViewModelProperty =
        AvaloniaProperty.Register<MapEditorCanvas, MapEditorPageViewModel?>(nameof(ViewModel));

    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<MapEditorCanvas, double>(nameof(Zoom), 8);

    private WriteableBitmap? _bitmap;
    private MapEditorDocument? _attachedDocument;
    private bool _isPainting;
    private bool _isPanning;
    private Point _panStartPointerPosition;
    private double _panStartHorizontalOffset;
    private double _panStartVerticalOffset;

    static MapEditorCanvas()
    {
        AffectsMeasure<MapEditorCanvas>(ViewModelProperty, ZoomProperty);
        AffectsRender<MapEditorCanvas>(ViewModelProperty, ZoomProperty);
        FocusableProperty.OverrideDefaultValue<MapEditorCanvas>(true);
    }

    public MapEditorPageViewModel? ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public ScrollViewer? ScrollViewerHost { get; set; }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ViewModelProperty)
        {
            AttachDocument(ViewModel?.Document);
            _bitmap = null;
            InvalidateMeasure();
            InvalidateVisual();
        }
        else if (change.Property == ZoomProperty)
        {
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (ViewModel?.Document is not MapEditorDocument document)
            return base.MeasureOverride(availableSize);

        var zoom = Math.Max(1, Zoom);
        return new Size(document.Width * zoom, document.Height * zoom);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (ViewModel?.Document is not MapEditorDocument document)
            return;

        _bitmap ??= MapRender.CreateMapBitmap(document.Tiles, document.Width, document.Height);

        context.FillRectangle(new SolidColorBrush(Color.Parse("#171717")), new Rect(Bounds.Size));

        using (context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None }))
        {
            context.DrawImage(
                _bitmap,
                new Rect(0, 0, _bitmap.PixelSize.Width, _bitmap.PixelSize.Height),
                new Rect(Bounds.Size));
        }

        if (Zoom >= 12)
            DrawGrid(context, document);

        if (ViewModel.GetHoveredTile() is (int X, int Y))
            DrawHoverOutline(context, X, Y);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        Focus();

        var point = e.GetCurrentPoint(this);
        var position = e.GetPosition(this);
        var modifiers = e.KeyModifiers;

        if (modifiers.HasFlag(KeyModifiers.Control) && point.Properties.IsLeftButtonPressed && ScrollViewerHost != null)
        {
            StartPan(e);
            e.Handled = true;
            return;
        }

        if (!TryGetTile(position, out var x, out var y))
            return;

        if (point.Properties.IsRightButtonPressed)
        {
            ViewModel?.PickTileAt(x, y);
            e.Handled = true;
            return;
        }

        if (!point.Properties.IsLeftButtonPressed)
            return;

        _isPainting = true;
        e.Pointer.Capture(this);
        ViewModel?.SetHoverTile(x, y);
        ViewModel?.BeginPaintAt(x, y);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isPanning)
        {
            ContinuePan(e);
            e.Handled = true;
            return;
        }

        if (TryGetTile(e.GetPosition(this), out var x, out var y))
        {
            ViewModel?.SetHoverTile(x, y);

            if (_isPainting && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                ViewModel?.ContinuePaintAt(x, y);
        }
        else
        {
            ViewModel?.SetHoverTile(null, null);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isPanning)
        {
            EndPan(e.Pointer);
            e.Handled = true;
            return;
        }

        EndPaintStroke(e.Pointer);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);

        if (_isPanning)
            ResetPan();

        if (_isPainting)
        {
            _isPainting = false;
            ViewModel?.EndPaint();
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        if (!_isPainting && !_isPanning)
            ViewModel?.SetHoverTile(null, null);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        if (ViewModel?.Document is not MapEditorDocument document)
            return;

        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        var direction = Math.Sign(e.Delta.Y);
        if (direction == 0)
            return;

        var oldZoom = ViewModel.Zoom;
        var newZoom = Math.Clamp(oldZoom + (direction * 2), 4, 24);
        if (Math.Abs(newZoom - oldZoom) < double.Epsilon)
            return;

        var pointerPosition = e.GetPosition(this);
        var scrollViewer = ScrollViewerHost;
        var currentOffset = scrollViewer?.Offset ?? default;
        var viewportPoint = scrollViewer != null
            ? new Point(pointerPosition.X - currentOffset.X, pointerPosition.Y - currentOffset.Y)
            : pointerPosition;
        var contentPoint = new Point(pointerPosition.X / oldZoom, pointerPosition.Y / oldZoom);

        ViewModel.Zoom = newZoom;

        if (scrollViewer != null)
        {
            var targetHorizontalOffset = (contentPoint.X * newZoom) - viewportPoint.X;
            var targetVerticalOffset = (contentPoint.Y * newZoom) - viewportPoint.Y;

            SetScrollOffsets(scrollViewer, document, newZoom, targetHorizontalOffset, targetVerticalOffset);
        }

        e.Handled = true;
    }

    private void AttachDocument(MapEditorDocument? document)
    {
        if (ReferenceEquals(_attachedDocument, document))
            return;

        _attachedDocument?.TilesChanged -= Document_TilesChanged;
        _attachedDocument = document;
        _attachedDocument?.TilesChanged += Document_TilesChanged;
    }

    private void Document_TilesChanged(object? sender, MapTilesChangedEventArgs e)
    {
        if (ViewModel?.Document is not MapEditorDocument document)
            return;

        _bitmap ??= MapRender.CreateMapBitmap(document.Tiles, document.Width, document.Height);
        MapRender.UpdateMapBitmap(_bitmap, document.Tiles, document.Width, document.Height, e.Indices);
        InvalidateVisual();
    }

    private void EndPaintStroke(IPointer pointer)
    {
        if (!_isPainting)
            return;

        _isPainting = false;
        pointer.Capture(null);
        ViewModel?.EndPaint();
    }

    private void StartPan(PointerPressedEventArgs e)
    {
        if (ScrollViewerHost == null)
            return;

        _isPanning = true;
        _panStartPointerPosition = e.GetPosition(ScrollViewerHost);
        _panStartHorizontalOffset = ScrollViewerHost.Offset.X;
        _panStartVerticalOffset = ScrollViewerHost.Offset.Y;

        if (_isPainting)
            EndPaintStroke(e.Pointer);

        e.Pointer.Capture(this);
    }

    private void ContinuePan(PointerEventArgs e)
    {
        if (!_isPanning || ScrollViewerHost == null || ViewModel?.Document is not MapEditorDocument document)
            return;

        var currentPosition = e.GetPosition(ScrollViewerHost);
        var delta = currentPosition - _panStartPointerPosition;

        SetScrollOffsets(
            ScrollViewerHost,
            document,
            ViewModel.Zoom,
            _panStartHorizontalOffset - delta.X,
            _panStartVerticalOffset - delta.Y);

        if (TryGetTile(e.GetPosition(this), out var x, out var y))
            ViewModel.SetHoverTile(x, y);
        else
            ViewModel.SetHoverTile(null, null);
    }

    private void EndPan(IPointer pointer)
    {
        if (!_isPanning)
            return;

        pointer.Capture(null);
        ResetPan();
    }

    private void ResetPan()
    {
        _isPanning = false;
    }

    private bool TryGetTile(Point position, out int x, out int y)
    {
        x = -1;
        y = -1;

        if (ViewModel?.Document is not MapEditorDocument document)
            return false;

        var zoom = Math.Max(1, Zoom);
        x = (int)(position.X / zoom);
        y = (int)(position.Y / zoom);

        return document.IsInBounds(x, y);
    }

    private static void SetScrollOffsets(
        ScrollViewer scrollViewer,
        MapEditorDocument document,
        double zoom,
        double horizontalOffset,
        double verticalOffset)
    {
        var viewportWidth = scrollViewer.Viewport.Width;
        var viewportHeight = scrollViewer.Viewport.Height;
        var maxHorizontalOffset = Math.Max(0, (document.Width * zoom) - viewportWidth);
        var maxVerticalOffset = Math.Max(0, (document.Height * zoom) - viewportHeight);

        scrollViewer.Offset = new Vector(
            Math.Clamp(horizontalOffset, 0, maxHorizontalOffset),
            Math.Clamp(verticalOffset, 0, maxVerticalOffset));
    }

    private void DrawGrid(DrawingContext context, MapEditorDocument document)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(90, 0, 0, 0)), 1);

        for (var x = 1; x < document.Width; x++)
        {
            var drawX = x * Zoom;
            context.DrawLine(pen, new Point(drawX, 0), new Point(drawX, Bounds.Height));
        }

        for (var y = 1; y < document.Height; y++)
        {
            var drawY = y * Zoom;
            context.DrawLine(pen, new Point(0, drawY), new Point(Bounds.Width, drawY));
        }
    }

    private void DrawHoverOutline(DrawingContext context, int x, int y)
    {
        var rect = new Rect(x * Zoom, y * Zoom, Zoom, Zoom);
        context.DrawRectangle(null, new Pen(Brushes.White, 2), rect);
    }
}
