using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using LTDSaveEditor.Core;
using LTDSaveEditor.Core.SAV;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LTDSaveEditor.Avalonia.Views;


public partial class EditorPageControl : UserControl
{
    public SavFile? SaveFile { get; private set; }
    private readonly ObservableCollection<TreeViewNode> _rootNodes = [];
    private ObservableCollection<DataGridRowWrapper>? _currentRows;

    public EditorPageControl()
    {
        InitializeComponent();
    }

    public EditorPageControl(SavFile savFile)
    {
        InitializeComponent();
        SaveFile = savFile;
        GameDataTree.ItemsSource = _rootNodes;

        LoadGameData();
    }

    private void GameDataTree_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (GameDataTree.SelectedItem is not TreeViewNode node || node.Hash == 0) return;

        if (SaveFile != null && SaveFile.TryGetEntry(node.Hash, out var entry))
        {
            UpdateEditor(entry, node.Hash);
        }
    }

    // Need to declare as dependecy so enums don't explode when trimming
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicEvents, typeof(DataGridRowWrapper))]
    private void UpdateEditor(SavFileEntry entry, uint hash)
    {
        // Detach old listeners
        if (_currentRows != null)
        {
            foreach (var row in _currentRows)
                row.PropertyChanged -= Row_PropertyChanged;
        }

        EntryDataGrid.Columns.Clear();
        EntryDataGrid.ItemsSource = null;
        _currentRows = [];

        var data = HashManager.TryGetData(hash, out var d) ? d : null;
        EntryTitle.Text = data?.Name ?? "< Unknown >";

        // Bulk edit visibility managed inside the array/single check below
        BulkEditSection.IsVisible = false;

        var targetType = entry.Value is Array arr ? arr.GetType().GetElementType() : entry.Value?.GetType();
        if (targetType == null) return;

        if (entry.Value is Array array)
        {
            var options = GetEnumOptions(entry, data);

            // Show bulk edit for IntArray, EnumArray, or fields with options (like food)
            var canBulkEdit = entry.DataType == DataType.IntArray || entry.DataType == DataType.EnumArray || options != null;
            BulkEditSection.IsVisible = canBulkEdit;

            if (canBulkEdit)
            {
                if (options != null)
                {
                    BulkEnumInput.IsVisible = true;
                    BulkEnumInput.ItemsSource = options;
                    BulkEnumInput.SelectedIndex = 0;
                    BulkValueInput.IsVisible = false;
                }
                else
                {
                    BulkEnumInput.IsVisible = false;
                    BulkValueInput.IsVisible = true;
                    BulkValueInput.Text = "0";
                }
            }
            for (var i = 0; i < array.Length; i++)
            {
                var val = array.GetValue(i);
                var row = new DataGridRowWrapper
                {
                    Index = i,
                    Value = val,
                    Tag = entry,
                    Options = options
                };

                if (options != null && val is uint hashValue)
                    row.SelectedOption = options.FirstOrDefault(o => o.Value == hashValue);

                row.PropertyChanged += Row_PropertyChanged;
                _currentRows.Add(row);
            }

            // Create columns
            EntryDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Index",
                Binding = new Binding("Index"),
                IsReadOnly = true,
                Width = new DataGridLength(85)
            });

            var valueCol = CreateColumn(entry, data, targetType);
            valueCol.Header = "Value";
            valueCol.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            EntryDataGrid.Columns.Add(valueCol);
        }
        else
        {
            var options = GetEnumOptions(entry, data);
            var val = entry.Value;

            var row = new DataGridRowWrapper
            {
                Index = 0,
                Value = val,
                Label = data?.Name ?? "Value",
                Tag = entry,
                Options = options
            };

            if (options != null && val is uint hashValue)
                row.SelectedOption = options.FirstOrDefault(o => o.Value == hashValue);

            row.PropertyChanged += Row_PropertyChanged;
            _currentRows.Add(row);

            var valueCol = CreateColumn(entry, data, targetType);
            valueCol.Header = "Value";
            valueCol.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            EntryDataGrid.Columns.Add(valueCol);
        }

        EntryDataGrid.ItemsSource = _currentRows;
    }

    private void BulkApplyButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_currentRows == null) return;

        object? newValue = null;

        if (BulkEnumInput.IsVisible)
        {
            if (BulkEnumInput.SelectedItem is EnumOption selected)
                newValue = selected.Value;
        }
        else if (BulkValueInput.IsVisible)
        {
            if (int.TryParse(BulkValueInput.Text, out var result))
                newValue = result;
        }

        if (newValue != null)
        {
            foreach (var row in _currentRows)
            {
                row.Value = newValue;
            }
        }
    }

    private void Row_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(DataGridRowWrapper.Value)) return;
        if (sender is not DataGridRowWrapper row) return;
        if (row.Tag is not SavFileEntry entry) return;

        try
        {
            var targetType = entry.Value is Array arr ? arr.GetType().GetElementType() : entry.Value?.GetType();
            if (targetType == null) return;

            var converted = Convert.ChangeType(row.Value, targetType);

            if (entry.Value is Array array)
            {
                array.SetValue(converted, row.Index);
            }
            else
            {
                entry.Value = converted;
            }
        }
        catch { /* Handle parse errors */ }
    }

    private static List<EnumOption>? GetEnumOptions(SavFileEntry entry, GameData? data)
    {
        var isEnum = entry.DataType is DataType.Enum or DataType.EnumArray;
        var isUInt = entry.DataType is DataType.UInt or DataType.UIntArray;

        if (isEnum || isUInt)
        {
            if (data != null && data.Options.Count > 0)
            {
                return [.. data.Options.Select(opt => new EnumOption { Label = opt.Value, Value = opt.Key })];
            }

            // Check if current value(s) match any food hash
            var isFood = false;
            if (entry.Value is uint hashValue)
            {
                isFood = FoodManager.FoodHashes.ContainsKey(hashValue);
            }
            else if (entry.Value is uint[] hashArray && hashArray.Length > 0)
            {
                isFood = hashArray.Any(FoodManager.FoodHashes.ContainsKey);
            }

            if (isFood)
            {
                return [.. FoodManager.FoodHashes.Select(kvp => new EnumOption { Label = kvp.Value, Value = kvp.Key }).OrderBy(o => o.Label)];
            }
        }
        return null;
    }

    private static DataGridTemplateColumn CreateColumn(SavFileEntry entry, GameData? data, Type targetType)
    {
        if (targetType == typeof(bool))
        {
            return new DataGridTemplateColumn
            {
                CellTemplate = new FuncDataTemplate<DataGridRowWrapper>((wrapper, _) =>
                    new CheckBox
                    {
                        [!CheckBox.IsCheckedProperty] = new Binding("Value", BindingMode.TwoWay),
                        HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left
                    })
            };
        }

        // Handle Enums or fields with options (like food) with ComboBox
        var options = GetEnumOptions(entry, data);
        if (options != null)
        {
            return new DataGridTemplateColumn
            {
                CellTemplate = new FuncDataTemplate<DataGridRowWrapper>((wrapper, _) =>
                {
                    var combo = new ComboBox
                    {
                        [!ComboBox.ItemsSourceProperty] = new Binding("Options"),
                        [!ComboBox.SelectedItemProperty] = new Binding("SelectedOption", BindingMode.TwoWay),
                        DisplayMemberBinding = new Binding("Label"),
                        HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch,
                        Margin = new Thickness(5, 2),
                        IsVisible = true
                    };
                    return combo;
                })
            };
        }

        // Use a TextBox template for everything else
        return new DataGridTemplateColumn
        {
            CellTemplate = new FuncDataTemplate<DataGridRowWrapper>((wrapper, _) =>
            {
                var tb = new TextBox
                {
                    [!TextBox.TextProperty] = new Binding("Value", BindingMode.TwoWay),
                    VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
                    Margin = new Thickness(5, 2),
                    BorderThickness = new Thickness(0, 0, 0, 1), // Subtle bottom border
                    Background = global::Avalonia.Media.Brushes.Transparent
                };
                return tb;
            })
        };
    }

    private void LoadGameData()
    {
        _rootNodes.Clear();
        var nodesMap = new Dictionary<string, TreeViewNode>();

        if (SaveFile == null) return;

        foreach (var (hash, entry) in SaveFile.Entries)
        {
            var name = HashManager.GetName(hash);
            var parts = name.Split('.');

            TreeViewNode? currentNode = null;
            var currentLevel = _rootNodes;
            var currentPath = "";

            foreach (var part in parts)
            {
                currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}.{part}";

                if (!nodesMap.TryGetValue(currentPath, out currentNode))
                {
                    currentNode = new TreeViewNode { Label = part };
                    nodesMap[currentPath] = currentNode;
                    currentLevel.Add(currentNode);
                }
                currentLevel = currentNode.Nodes;
            }

            if (currentNode != null)
            {
                if (entry.DataType == DataType.Bool64bitKey && entry.Value == null) continue;

                currentNode.Hash = hash;
                if (entry.Value is Array array)
                    currentNode.Label += $" ({entry.DataType}[{array.Length}])";
                else
                    currentNode.Label += $" ({entry.DataType})";
            }
        }
    }
}
