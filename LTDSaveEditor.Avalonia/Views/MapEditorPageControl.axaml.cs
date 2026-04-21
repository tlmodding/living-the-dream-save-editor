using Avalonia.Controls;
using LTDSaveEditor.Avalonia.ViewModels;
using LTDSaveEditor.Core.SAV;
using System;

namespace LTDSaveEditor.Avalonia.Views;

public partial class MapEditorPageControl : UserControl
{
    public MapEditorPageControl()
    {
        InitializeComponent();
        MapCanvas.ScrollViewerHost = MapScrollViewer;
    }

    public MapEditorPageControl(SavFile savFile) : this()
    {
        try
        {
            DataContext = new MapEditorPageViewModel(savFile);
        }
        catch (Exception ex)
        {
            EditorState.IsVisible = false;
            ErrorState.IsVisible = true;
            ErrorText.Text = ex.Message;
        }
    }
}
