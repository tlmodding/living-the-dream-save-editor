using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LTDSaveEditor.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LTDSaveEditor.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize HashManager if not already done
        if (!HashManager.IsInitialized)
        {
            string[] possiblePaths = [
                Path.Combine(AppContext.BaseDirectory, "Data", "GameDataListFull.csv"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "GameDataListFull.csv"),
                Path.Combine(Directory.GetCurrentDirectory(), "LTDSaveEditor.Avalonia", "Data", "GameDataListFull.csv"),
                "Data/GameDataListFull.csv"
            ];

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    HashManager.Initialize(path);
                    break;
                }
            }
        }

        // Initialize FoodManager if not already done
        if (!FoodManager.IsInitialized)
        {
            string[] possiblePaths = [
                Path.Combine(AppContext.BaseDirectory, "Data", "food_hashes.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "food_hashes.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "LTDSaveEditor.Avalonia", "Data", "food_hashes.json"),
                "Data/food_hashes.json"
            ];

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    FoodManager.Initialize(path);
                    break;
                }
            }
        }

        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
            e.DragEffects = DragDropEffects.Copy;
        else
            e.DragEffects = DragDropEffects.None;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var items = e.Data.GetFiles();
        if (items != null && items.Any())
        {
            var first = items.First();
            string? localPath = first.Path.LocalPath;

            if (localPath != null)
            {
                if (Directory.Exists(localPath))
                {
                    ValidateAndOpenSave(localPath);
                }
                else if (File.Exists(localPath))
                {
                    var parent = Path.GetDirectoryName(localPath);
                    if (parent != null)
                        ValidateAndOpenSave(parent);
                }
            }
        }
    }

    private async void SelectFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Save Folder",
            AllowMultiple = false
        });

        if (folders.Any())
        {
            ValidateAndOpenSave(folders[0].Path.LocalPath);
        }
    }

    private void ValidateAndOpenSave(string saveFolder)
    {
        if (!ValidateSaveFolder(saveFolder))
            return;

        var saveInstance = SaveInstance.FromFolder(saveFolder);
        var editorWindow = new EditorWindow(saveInstance);
        editorWindow.Show();
        this.Close();
    }

    public string[] RequiredFiles = ["Map.sav", "Mii.sav", "Player.sav"];

    public bool ValidateSaveFolder(string saveFolder)
    {
        if (!Directory.Exists(saveFolder))
        {
            StatusText.Text = "The specified folder does not exist.";
            return false;
        }

        var files = Directory.GetFiles(saveFolder).Select(x => Path.GetFileName(x));

        if (!RequiredFiles.All(y => files.Contains(y, StringComparer.OrdinalIgnoreCase)))
        {
            StatusText.Text = "The folder does not contain Required files: " + string.Join(", ", RequiredFiles);
            return false;
        }

        return true;
    }
}
