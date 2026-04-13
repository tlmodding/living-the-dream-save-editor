using CommunityToolkit.Mvvm.ComponentModel;
using LTDSaveEditor.Core.SAV;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace LTDSaveEditor.Avalonia.ViewModels;

public partial class MiiOptionViewModel(SavFile savFile, int index) : ObservableObject
{
    private readonly SavFile _savFile = savFile;

    public int Index { get; } = index;

    public string Name
    {
        get => GetMiiValue("Mii.Name.Name", "Unknown Mii");
        set => SetMiiValue("Mii.Name.Name", value);
    }

    public uint Money
    {
        get => GetMiiValue<uint>("Mii.Belongings.Money", 0);
        set => SetMiiValue("Mii.Belongings.Money", value);
    }

    private bool SetMiiValue<T>(string key, T value, [CallerMemberName] string? propertyName = null)
    {
        if (value is null)
            return false;
        
        if (!TryGetValue<T[]>(key, out var array) || array is null)
            return false;

        if (Index < 0 || Index >= array.Length)
            return false;

        if (EqualityComparer<T>.Default.Equals(array[Index], value))
            return false;

        array[Index] = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private T GetMiiValue<T>(string key, T fallback = default!)
    {
        if (TryGetValueFromMiiArray<T>(key, out var value))
            return value;

        return fallback;
    }

    private bool TryGetValue<T>(string key, [MaybeNullWhen(false)] out T value) => _savFile.TryGetValue(key, out value);
    private bool TryGetValueFromMiiArray<T>(string key, [MaybeNullWhen(false)] out T value)
    {
        value = default;

        if (!_savFile.TryGetValue(key, out T[]? array) || array is null || array.Length == 0)
            return false;

        if (Index < 0 || Index >= array.Length)
            return false;

        value = array[Index];
        return true;
    }
}