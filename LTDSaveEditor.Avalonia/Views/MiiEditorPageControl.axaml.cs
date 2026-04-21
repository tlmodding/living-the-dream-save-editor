using Avalonia.Controls;
using LTDSaveEditor.Avalonia.ViewModels;
using LTDSaveEditor.Core.SAV;

namespace LTDSaveEditor.Avalonia;

public partial class MiiEditorPageControl : UserControl
{
    public SavFile? SavFile { get; }

    // Empty constructor for Avalonia
    public MiiEditorPageControl()
    {
        InitializeComponent();
    }

    public MiiEditorPageControl(SavFile savFile) : this()
    {
        SavFile = savFile;
        DataContext = new MiiEditorPageViewModel(savFile);
    }
}
