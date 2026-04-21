using CommunityToolkit.Mvvm.ComponentModel;
using LTDSaveEditor.Core.SAV;
using System.Collections.ObjectModel;

namespace LTDSaveEditor.Avalonia.ViewModels;

public partial class MiiEditorPageViewModel : ObservableObject
{
    public ObservableCollection<MiiOptionViewModel> Miis { get; } = [];

    [ObservableProperty]
    private MiiOptionViewModel? selectedMii;

    public MiiEditorPageViewModel(SavFile savFile)
    {
        // Uses Mii.Name to check if Mii exists... probably there is a better way 🤔
        if (savFile.TryGetValue<string[]>("Mii.Name.Name", out var names))
        {
            for (var i = 0; i < names.Length; i++)
                if (!string.IsNullOrEmpty(names[i]))
                    Miis.Add(new MiiOptionViewModel(savFile, i));

            SelectedMii = Miis.Count > 0 ? Miis[0] : null;
        }
    }
}