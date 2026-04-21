namespace LTDSaveEditor.Avalonia.Views;

public class EnumOption
{
    public string Label { get; set; } = string.Empty;
    public uint Value { get; set; }

    public override string ToString() => Label;
}