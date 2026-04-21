using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using LTDSaveEditor.Common.Utility;
using LTDSaveEditor.Core;
using LTDSaveEditor.Core.SAV;
using System;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO;
using System.Threading.Tasks;

namespace LTDSaveEditor.Avalonia.Views;

public partial class EditorWindow : AppWindow
{
    public SaveInstance? SaveInstance { get; private set; }
    public BackupManager? BackupManager { get; }
    private bool _sessionBackupCreated = false;

    private readonly System.Collections.ObjectModel.ObservableCollection<TabViewItem> _tabs = [];

    public EditorWindow()
    {
        InitializeComponent();
    }

    public EditorWindow(SaveInstance instance)
    {
        InitializeComponent();

        SaveInstance = instance;
        BackupManager = new BackupManager(SaveInstance.Folder);

        EditorTabView.TabItems = _tabs;

        TitleBar.Height = 40;
        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;

        CreateTabs();
    }


    private void CreateTabs()
    {
        if (SaveInstance == null) return;

        CreateTab("Player", SaveInstance.Player);
        CreateTab("Mii", SaveInstance.Mii);
        CreateTab("Map Data", SaveInstance.Map);

        _tabs.Add(new TabViewItem
        {
            Header = "Map Editor",
            IsClosable = false,
            Content = new MapEditorPageControl(SaveInstance.Map)
        });
#if DEBUG
        _tabs.Add(new TabViewItem
        {
            Header = "Mii (new)",
            IsClosable = false,
            Content = new MiiEditorPageControl(SaveInstance.Mii)
        });
#endif
    }

    private void CreateTab(string title, SavFile savFile)
    {
        var tab = new TabViewItem
        {
            Header = title,
            IsClosable = false,
            Content = new EditorPageControl(savFile)
        };
        _tabs.Add(tab);
    }

    private async void SaveMenu_Click(object? sender, RoutedEventArgs e)
    {
        if (SaveInstance == null) return;
        try
        {
            if (!Directory.Exists(SaveInstance.Folder))
                throw new Exception("Save folder does not exist.");

            if (!_sessionBackupCreated)
            {
                BackupManager?.CreateBackup(SaveInstance.Player.Path, SaveInstance.Mii.Path, SaveInstance.Map.Path);
                _sessionBackupCreated = true;
            }

            var playerPath = Path.Combine(SaveInstance.Folder, "Player.sav");
            SaveInstance.Player.SaveTo(playerPath);

            var miiPath = Path.Combine(SaveInstance.Folder, "Mii.sav");
            SaveInstance.Mii.SaveTo(miiPath);

            var mapPath = Path.Combine(SaveInstance.Folder, "Map.sav");
            SaveInstance.Map.SaveTo(mapPath);

            await ShowMessage("Success", "Save files updated successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessage("Error", $"Failed to save: {ex.Message}");
        }
    }

    private async void RefreshMenu_Click(object? sender, RoutedEventArgs e)
    {
        if (SaveInstance == null) return;

        try
        {
            _tabs.Clear();
            _sessionBackupCreated = false;

            // Re-load SaveInstance directly
            SaveInstance = new SaveInstance(SaveInstance.Folder);
            CreateTabs();

            await ShowMessage("Refreshed", "Save data has been reloaded from disk.");
        }
        catch (Exception ex)
        {
            await ShowMessage("Error", $"Failed to refresh: {ex.Message}");
        }
    }

    private static async Task ShowMessage(string title, string message) => await new ContentDialog
    {
        Title = title,
        Content = message,
        CloseButtonText = "OK",
        DefaultButton = ContentDialogButton.Close
    }.ShowAsync();

    private void ExitMenu_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void DiscordMenu_Click(object? sender, RoutedEventArgs e)
    {
        OpenUrl("https://discord.gg/YHFNTvXrdE");
    }

    private void WikiMenu_Click(object? sender, RoutedEventArgs e)
    {
        OpenUrl("https://tlmodding.github.io/");
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch { }
    }
}