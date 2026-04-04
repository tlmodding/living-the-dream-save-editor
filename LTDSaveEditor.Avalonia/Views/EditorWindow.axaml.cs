using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using LTDSaveEditor.Avalonia.Utility;
using LTDSaveEditor.Core;
using LTDSaveEditor.Core.SAV;
using System;
using System.Diagnostics;
using System.IO;

namespace LTDSaveEditor.Avalonia.Views;

public partial class EditorWindow : AppWindow
{
    public SaveInstance SaveInstance { get; private set; }
    public BackupManager BackupManager { get; }
    private bool _sessionBackupCreated = false;
    
    private readonly System.Collections.ObjectModel.ObservableCollection<TabViewItem> _tabs = [];
    
    public EditorWindow(SaveInstance instance)
    {
        InitializeComponent();
        
        SaveInstance = instance;
        BackupManager = new BackupManager(SaveInstance.Folder);

        EditorTabView.TabItems = _tabs;

        TitleBar.Height = 40;
        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;

        CreateTab("Player", SaveInstance.Player);
        CreateTab("Mii", SaveInstance.Mii);
        CreateTab("Map", SaveInstance.Map);
    }

    private void CreateTab(string title, SavFile savFile)
    {
        var tab = new TabViewItem
        {
            Header = title,
            Content = new EditorPageControl(savFile)
        };
        _tabs.Add(tab);
    }

    private async void SaveMenu_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!Directory.Exists(SaveInstance.Folder))
                throw new Exception("Save folder does not exist.");

            if (!_sessionBackupCreated)
            {
                BackupManager.CreateBackup(SaveInstance.Player.Path, SaveInstance.Mii.Path, SaveInstance.Map.Path);
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
        try
        {
            _tabs.Clear();
            _sessionBackupCreated = false;
            
            // Re-load SaveInstance directly
            SaveInstance = new SaveInstance(SaveInstance.Folder);

            CreateTab("Player", SaveInstance.Player);
            CreateTab("Mii", SaveInstance.Mii);
            CreateTab("Map", SaveInstance.Map);

            await ShowMessage("Refreshed", "Save data has been reloaded from disk.");
        }
        catch (Exception ex)
        {
            await ShowMessage("Error", $"Failed to refresh: {ex.Message}");
        }
    }

    private async System.Threading.Tasks.Task ShowMessage(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            DefaultButton = ContentDialogButton.Close
        };
        await dialog.ShowAsync();
    }

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

    private void OpenUrl(string url)
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
